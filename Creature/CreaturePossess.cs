using UnityEngine;
using CreatureTypes;

// 방에 있는 생물 1마리를 골라 직접 조종.
// 대상 Think2를 수동 모드로 돌리고, WASD로 그 생물의 EQS proxy를 움직이면
// TargetControl/RBpart/다리 이동 스택이 그대로 따라감.
public class CreaturePossess : MonoBehaviour
{
    [Header("입력")]
    public KeyCode possessKey = KeyCode.F;     // 조준한 생물 빙의/해제
    public float proxyMoveSpeed = 8f;
    public Transform cameraTransform;          // 이동 방향 기준 (비우면 Camera.main)

    [Header("빙의 콜라이더")]
    [Tooltip("빙의 중 proxy에 붙일 콜라이더 반지름 — 벽/문에 물리로 막히게 함")]
    public float proxyColliderRadius = 1f;

    [Header("탑승")]
    [Tooltip("빙의 중 플레이어를 생물 자식으로 둬서 같이 이동")]
    public bool rideCreature = true;

    private Think2 controlled;
    private Transform proxy;
    private Transform playerOriginalParent;
    private bool hierarchySaved = false;
    private Rigidbody playerRb;
    private bool rbWasKinematic;
    private Transform camOriginalParent;

    private Rigidbody proxyRb;
    private SphereCollider proxyCol;
    private Vector3 driveDir;   // Update에서 계산, FixedUpdate에서 물리 적용

    public bool IsPossessing => controlled != null;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(possessKey))
        {
            if (IsPossessing) Release();
            else TryPossessLockedTarget();
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
        var pl = Player.Instance != null ? Player.Instance.pl : null;
        Creature target = pl != null ? pl.targetCreature : null;
        if (target == null) { Debug.Log("[Possess] 락온된 생물 없음"); return; }

        Think2 brain = target.GetComponentInChildren<Think2>();
        if (brain == null) brain = target.GetComponentInParent<Think2>();
        if (brain == null) { Debug.Log("[Possess] 대상에 Think2 없음"); return; }

        Possess(brain);
    }

    private Creature controlledCreature;

    public void Possess(Think2 brain)
    {
        // Door / D 는 빙의 불가
        if (brain.self != null && brain.self.data != null &&
            (brain.self.data.creatureID == CreatureID.Door ||
             brain.self.data.creatureID == CreatureID.D))
        {
            Debug.Log("[Possess] Door/D는 빙의할 수 없습니다");
            return;
        }

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

            // 조종 중엔 그 생물로 락온 고정
            var pl = Player.Instance != null ? Player.Instance.pl : null;
            if (pl != null) pl.ForceLock(controlledCreature);
        }

        if (rideCreature && proxy != null)
        {
            // 플레이어를 proxy 자식으로 — proxy는 회전 안 하니 카메라도 안 돎
            Transform mount = proxy;

            playerOriginalParent = transform.parent;
            hierarchySaved = true;
            transform.SetParent(mount, true);          // 월드 위치 유지하며 자식으로
            transform.localPosition = Vector3.zero;    // 탑승 지점에 스냅
            PlayerControl.SetPlayerMove(false);        // 플레이어 자체 이동은 정지

            // 물리 오브젝트는 부모를 안 따라감 → kinematic으로 바꿔야 실려 다님
            if (playerRb == null) playerRb = GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                rbWasKinematic = playerRb.isKinematic;
                playerRb.isKinematic = true;
            }

            // 카메라는 EQS proxy 자식으로 → proxy는 회전 안 하니 시점 안 돎
            if (cameraTransform != null && proxy != null)
            {
                camOriginalParent = cameraTransform.parent;
                cameraTransform.SetParent(proxy, true);
            }
        }


        SetupProxyPhysics();

        Debug.Log($"[Possess] {controlled.name} 조종 시작");
    }

    // proxy에 Rigidbody + Collider를 붙여 벽/문에 물리로 막히게 함
    private void SetupProxyPhysics()
    {
        if (proxy == null) return;
        var go = proxy.gameObject;

        proxyCol = go.GetComponent<SphereCollider>();
        if (proxyCol == null) proxyCol = go.AddComponent<SphereCollider>();
        proxyCol.radius = proxyColliderRadius;

        proxyRb = go.GetComponent<Rigidbody>();
        if (proxyRb == null) proxyRb = go.AddComponent<Rigidbody>();
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
        Release();   // 생물 파괴 전에 플레이어·카메라 분리
    }

    public void Release()
    {
        if (controlled == null) return;
        controlled.SetManualControl(false);

        TeardownProxyPhysics();
        driveDir = Vector3.zero;

        if (controlledCreature != null)
        {
            controlledCreature.Died -= OnControlledDied;
            // 조종 해제 → intent 복구 (AI 다시 판단하게)
            if (controlledCreature.intent == CreatureIntent.Controlled)
                controlledCreature.intent = CreatureIntent.Wander;
            controlledCreature = null;
        }

        // 락온 해제
        var pl = Player.Instance != null ? Player.Instance.pl : null;
        if (pl != null) pl.Unlock();

        if (hierarchySaved)
        {
            transform.SetParent(playerOriginalParent, true);
            hierarchySaved = false;
            PlayerControl.SetPlayerMove(true);

            if (playerRb != null) playerRb.isKinematic = rbWasKinematic;   // 물리 복구

            if (cameraTransform != null)
                cameraTransform.SetParent(camOriginalParent, true);        // 카메라 원위치
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
