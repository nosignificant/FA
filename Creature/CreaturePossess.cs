using UnityEngine;
using CreatureTypes;

public class CreaturePossess : MonoBehaviour
{
    [Header("입력")]
    public KeyCode possessKey = KeyCode.F;     // 조준한 생물 빙의/해제
    public float proxyMoveSpeed = 8f;
    public Transform cameraTransform;          // 이동 방향 기준 (비우면 Camera.main)


    [Header("탑승")]
    [Tooltip("빙의 중 플레이어를 생물 자식으로 둬서 같이 이동")]
    public bool rideCreature = true;

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
        if (Input.GetKeyDown(possessKey))
        {
            if (IsPossessing) Release();
            else TryPossessLockedTarget();
        }

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

        // 생물이 죽으면(분해 등) 파괴 직전에 풀어주기
        controlledCreature = controlled.self;
        if (controlledCreature != null)
        {
            controlledCreature.Died += OnControlledDied;
            controlledCreature.intent = CreatureIntent.Controlled;   // 조종 상태로

            Player.Instance.pl.ForceLock(controlledCreature);
        }

        if (rideCreature && proxy != null)
        {
            transform.SetParent(proxy, true);          // 월드 위치 유지하며 자식으로
            transform.localPosition = Vector3.zero;    // 탑승 지점에 스냅
            PlayerControl.SetPlayerMove(false);        // 플레이어 자체 이동은 정지

            if (playerRb == null) playerRb = GetComponent<Rigidbody>();
            if (playerRb != null) { playerRb.isKinematic = true; }

            if (cameraTransform != null && proxy != null)
            {
                camOriginalParent = cameraTransform.parent;
                cameraTransform.SetParent(proxy, true);
            }
        }


        SetupProxyPhysics();

        Debug.Log($"[Possess] {controlled.name} 조종");
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

    private void OnControlledDied(Creature c, CreatureID who)
    {
        ToastUI.Instance?.Show("조종하던 생물이 사라졌다");
        Release();   // 생물 파괴 전에 플레이어·카메라 분리
    }

    public void Release()
    {
        if (controlled == null) return;
        controlled.SetManualControl(false);

        TeardownProxyPhysics();
        driveDir = Vector3.zero;

        // proxy와 rootTransform 중간 지점에 플레이어 내리기
        if (controlledCreature != null && proxy != null)
        {
            Vector3 proxyPos = proxy.position;
            Vector3 rootPos = controlledCreature.rootTransform != null
                ? controlledCreature.rootTransform.position
                : controlledCreature.transform.position;
            transform.position = (proxyPos + rootPos) * 0.5f;
        }

        if (controlledCreature != null)
        {
            controlledCreature.Died -= OnControlledDied;
            if (controlledCreature.intent == CreatureIntent.Controlled)
                controlledCreature.intent = CreatureIntent.Wander;
            controlledCreature = null;
        }
        Player.Instance.pl.Unlock();

        transform.SetParent(null, true);
        PlayerControl.SetPlayerMove(true);

        if (playerRb != null) playerRb.isKinematic = false;

        if (cameraTransform != null)
        {
            cameraTransform.SetParent(camOriginalParent, true);
            cameraTransform.localPosition = camLocalPos;
        }

        Debug.Log($"[Possess] {controlled.name} 조종 해제");
        controlled = null;
        proxy = null;
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
