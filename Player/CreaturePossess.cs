using UnityEngine;
using CreatureTypes;

// tryPossess > possess에 해당 생물 넘김 , think 멈춤 
// possess하면 proxy에 자식으로 이동, proxy에 rigidbody + collider 붙여서 방/문에 막히게 함

//release하면 rigidbody + collider 없애고, think 다시 실행 


public class CreaturePossess : MonoBehaviour
{
    [Header("입력")]
    public float proxyMoveSpeed = 8f;
    public Transform cameraTransform;          // 이동 방향 기준 (비우면 Camera.main)


    private Think2 controlled;
    private Transform proxy;


    private Rigidbody playerRb;
    private bool rbWasKinematic;
    private Transform camOriginalParent;
    private Vector3 camLocalPos;

    private Rigidbody proxyRb;
    private SphereCollider proxyCol;
    private Vector3 driveDir;   // Update에서 계산, FixedUpdate에서 물리 적용

    public bool IsPossessing => controlled != null;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        camLocalPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        // 조종 생물이 잡히거나(합성/분해) 파괴되면 즉시 해제.
        // (합성은 Died 이벤트 없이 Destroy하므로 여기서 잡아야 proxy 파괴로 카메라가 딸려가는 걸 막음)
        if (IsPossessing &&
            (controlledCreature == null || controlledCreature.IsDead || controlledCreature.IsGrabbed))
        {
            ToastUI.Instance?.Show("조종하던 생물을 빼앗겼습니다");
            Release();
            return;
        }

        if (IsPossessing) DriveProxy();
    }

    // 빙의 토글 (PlayerInputManager의 F 키에서 호출)
    public void TogglePossess()
    {
        if (IsPossessing) Release();
        else TryPossessLockedTarget();
    }

    private void FixedUpdate()
    {
        if (!IsPossessing || proxyRb == null) return;
        proxyRb.linearVelocity = driveDir * proxyMoveSpeed;
    }



    // 플레이어가 락온 중인 생물을 빙의
    private void TryPossessLockedTarget()
    {
        Creature target = Player.Instance.pl.targetCreature;
        if (target == null) return;

        // 조종 불가 생물이면 알림만 띄우고 중단
        if (target.data != null && !target.data.controllable)
        {
            ToastUI.Instance?.Show("이 생물은 당신이 조종할 수 없습니다");
            return;
        }

        // 다른 생물에게 잡혀있는(합성/분해 중) 생물은 조종 불가
        if (target.IsGrabbed)
        {
            ToastUI.Instance?.Show("다른 생물에게 잡힌 생물은 조종할 수 없습니다");
            return;
        }

        // 빙의 즉시 사망 종: 실제 조종 없이 스토리만 올리고 죽임 (일회성 소모)
        if (target.data != null && target.data.dieOnPossess)
        {
            Player.Instance.TryAdvanceFromPossess(target);
            if (!target.IsDead) target.TakeDamage(target.currentHP, Player.Instance.pc);
            return;
        }

        Think2 brain = target.GetComponent<Think2>();
        if (brain == null) return;

        Possess(brain);
    }

    private Creature controlledCreature;

    public void Possess(Think2 brain)
    {
        if (brain.self?.data == null || !brain.self.data.controllable) return;

        Release();   // 이전 거 해제
        controlled = brain;
        controlled.SetManualControl(true);
        proxy = controlled.ProxyTarget;
        driveDir = Vector3.zero;

        controlledCreature = controlled.self;

        CreatureControlSetting(controlledCreature, true);
        if (proxy != null) PlayerRideProxy(proxy, true);
        SetupProxyPhysics();

        Debug.Log($"[Possess] {controlled.name} 조종");
    }



    private void OnControlledDied(Creature c, CreatureID who)
    {
        ToastUI.Instance?.Show("조종하던 생물이 사라졌다");
        Release();   // 생물 파괴 전에 플레이어·카메라 분리
    }

    [Tooltip("방 밖에서 내릴 때 경계 안쪽으로 밀어넣을 여유 거리")]
    public float roomClampPadding = 2f;

    // 하차 위치가 방(마지막으로 속한 방) 밖이면 경계 안쪽 최근접점으로 당김.
    private Vector3 ClampToRoom(Vector3 pos)
    {
        Room room = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (room == null && controlledCreature != null) room = controlledCreature.currentRoom;
        if (room == null || room.homeBound == null) return pos;   // 기준 방 없으면 그대로

        Bounds b = room.homeBound.bounds;

        bool insideXYZ = pos.x >= b.min.x && pos.x <= b.max.x &&
                        pos.z >= b.min.z && pos.z <= b.max.z &&
                        pos.y >= b.min.y && pos.y <= b.max.y;

        if (insideXYZ) return pos;

        Vector3 inside = b.ClosestPoint(pos);
        Vector3 toCenter = b.center - inside;
        toCenter.y = 0f;
        if (toCenter.sqrMagnitude > 0.001f)
            inside += toCenter.normalized * roomClampPadding;

        return new Vector3(inside.x, inside.y, inside.z);
    }

    public void Release()
    {
        if (controlled == null) return;
        controlled.SetManualControl(false);

        TeardownProxyPhysics();
        driveDir = Vector3.zero;

        // 하차 위치 계산 (proxy가 아직 부모인 상태에서 월드 좌표로 미리 잡음)
        Vector3? landingPos = proxy != null ? ClampToRoom(proxy.position) : null;

        CreatureControlSetting(controlledCreature, false);
        controlledCreature = null;

        PlayerRideProxy(proxy, false);   // SetParent(null) 포함
        if (landingPos.HasValue) transform.position = landingPos.Value;

        Debug.Log($"[Possess] {controlled.name} 조종 해제");
        controlled = null;
        proxy = null;
    }

    private void PlayerRideProxy(Transform target, bool isRiding)
    {
        if (isRiding)
        {
            transform.SetParent(target, true);          // 현재 위치 유지하며 proxy 자식으로
            transform.localPosition = Vector3.zero;      // proxy 지점으로 스냅

            if (cameraTransform != null)
            {
                camOriginalParent = cameraTransform.parent;
                cameraTransform.SetParent(target, true);
            }
        }
        else
        {
            transform.SetParent(null, true);
            if (cameraTransform != null)
            {
                cameraTransform.SetParent(camOriginalParent, true);
                cameraTransform.localPosition = camLocalPos;
            }
        }

        PlayerControl.SetPlayerMove(!isRiding);

        if (playerRb == null) playerRb = GetComponent<Rigidbody>();
        if (playerRb != null) playerRb.isKinematic = isRiding;
    }

    // 조종 대상 생물의 상태 설정(isRiding=true) / 해제(false)
    private void CreatureControlSetting(Creature creature, bool isRiding)
    {
        if (creature == null) return;

        if (isRiding)
        {
            creature.Died += OnControlledDied;
            creature.intent = CreatureIntent.Controlled;

            Player.Instance.pl.ForceLock(creature);

            // advancesStory 종이면 스토리 단계 +1
            Player.Instance.TryAdvanceFromPossess(creature);

        }
        else
        {
            creature.Died -= OnControlledDied;
            if (creature.intent == CreatureIntent.Controlled)
                creature.intent = CreatureIntent.Wander;

            Player.Instance.pl.Unpin();   // 고정만 해제 — 해제 후에도 그 생물을 계속 바라봄
        }
    }

    // proxy에 Rigidbody + Collider를 붙여 벽/문에 물리로 막히게 함
    private void SetupProxyPhysics()
    {
        if (proxy == null) return;
        var go = proxy.gameObject;

        proxyCol = go.AddComponent<SphereCollider>();
        proxyCol.radius = 7f;

        proxyRb = go.AddComponent<Rigidbody>();
        proxyRb.useGravity = false;
        proxyRb.isKinematic = false;
        proxyRb.constraints = RigidbodyConstraints.FreezeRotation;
        proxyRb.interpolation = RigidbodyInterpolation.Interpolate;
        proxyRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 조종 중인 생물 몸체·플레이어와는 충돌 무시 (자기 몸에 끼임 방지)
        IgnoreCollisionsWith(controlledCreature != null ? controlledCreature.gameObject : null);
        if (Player.Instance != null) IgnoreCollisionsWith(Player.Instance.gameObject);
    }

    private void IgnoreCollisionsWith(GameObject root)
    {
        if (root == null || proxyCol == null) return;
        foreach (var c in root.GetComponentsInChildren<Collider>())
            if (c != null && c != proxyCol) Physics.IgnoreCollision(proxyCol, c, true);
    }

    private void TeardownProxyPhysics()
    {
        if (proxyRb != null) { Destroy(proxyRb); proxyRb = null; }
        if (proxyCol != null) { Destroy(proxyCol); proxyCol = null; }
    }

    // WASD: 수평 이동 / E: 상승 Q: 하강
    // 입력 방향만 계산 — 실제 이동은 FixedUpdate에서 proxyRb로 처리(벽 충돌 적용)
    private void DriveProxy()
    {
        if (proxy == null) { Release(); return; }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 수직 입력 (E 상승, Q 하강)
        float up = 0f;
        if (Input.GetKey(KeyCode.E)) up += 1f;
        if (Input.GetKey(KeyCode.Q)) up -= 1f;

        // 카메라 기준 평면 방향
        Vector3 fwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        fwd.y = 0f; right.y = 0f;
        fwd.Normalize(); right.Normalize();

        driveDir = (fwd * v + right * h + Vector3.up * up).normalized;
    }
}
