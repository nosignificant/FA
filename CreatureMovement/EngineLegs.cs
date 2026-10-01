using UnityEngine;
using System.Collections;

// 절차적 다족 보행 (drift 기반 한 발씩) — QuadLegs와 동일 알고리즘, 발 개수 무관.
//  - 발마다 몸통 기준 "쉬는 자리(stance)"를 두고, stepThreshold 넘게 벌어지면 한 발씩 내딛음.
//  - 인접(각도 가까운) 다리는 동시에 안 듦 → 지지 유지. 발은 호(sin arc)로 이동.
//  - 착지 목표를 홈 부채꼴로 제한 → 다리 안 꼬임. 몸통은 transform 직접(관성 없음), 높이는 발 평균에서.
public class EngineLegs : MonoBehaviour
{
    [Header("Transform")]
    public Transform followingTarget;
    public Transform body;
    public Transform[] tipTargets;

    [Header("이동")]
    public float followTriggerDist = 1f;
    public float moveSpeed = 3f;
    public float turnSpeed = 6f;

    [Header("발 딛기")]
    public float stepThreshold = 2f;
    public float stride = 2.5f;
    public float stepHeight = 1.5f;
    public float stepDuration = 0.25f;
    [Tooltip("동시에 들 수 있는 최대 발 수")]
    public int maxLegsStepping = 2;
    [Tooltip("이 각도 안의 인접 다리는 동시에 안 듦")]
    public float neighborAngle = 70f;
    [Tooltip("발이 홈 방향에서 벗어날 최대 각도 (작을수록 안 꼬임)")]
    public float maxLegAngle = 40f;
    public float minRadiusRatio = 0.6f;
    public float maxRadiusRatio = 1.5f;

    [Header("몸통 높이/균형")]
    public float bodyHeight = 1.5f;
    public float heightAdjustSpeed = 8f;
    [Range(0f, 1f)] public float tiltToGround = 0.5f;

    public LayerMask ground;

    private float[] theta;
    private float[] legRadius;
    private bool[] isStepping;
    private Vector3[] plantedPos;   // 딛고 있는 발의 월드 위치 (몸 움직여도 고정)
    private Rigidbody rb;
    private Creature owner;         // 방 경계 clamp용

    void Start()
    {
        owner = GetComponentInParent<Creature>();
        if (body != null) rb = body.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        int n = tipTargets != null ? tipTargets.Length : 0;
        theta = new float[n];
        legRadius = new float[n];
        isStepping = new bool[n];
        plantedPos = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            Vector3 offset = tipTargets[i].position - body.position;
            offset.y = 0f;
            legRadius[i] = Mathf.Max(0.1f, offset.magnitude);
            theta[i] = Vector2.SignedAngle(
                new Vector2(offset.x, offset.z),
                new Vector2(body.forward.x, body.forward.z));
            plantedPos[i] = tipTargets[i].position;
        }
    }

    void Update()
    {
        if (body == null || followingTarget == null || tipTargets == null) return;

        Vector3 toTarget = followingTarget.position - body.position;
        Vector3 flat = toTarget; flat.y = 0f;
        float dist = flat.magnitude;
        bool moving = dist > followTriggerDist;
        Vector3 moveDir = flat.sqrMagnitude > 0.0001f ? flat.normalized : body.forward;

        if (moving)
        {
            Vector3 next = body.position + moveDir * moveSpeed * Time.deltaTime;
            next.y = body.position.y;
            body.position = ClampToRoom(next);   // 방 경계 밖으로 못 나가게 → PushInsideRoom 순간이동 방지

            Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
            body.rotation = Quaternion.Slerp(body.rotation, look, Time.deltaTime * turnSpeed);
        }

        TryStepLegs(moving ? moveDir : Vector3.zero);
        AdjustBody();
    }

    // 몸 이동/회전 뒤에도 딛고 있는 발은 땅에 고정 (몸 자식이라 안 그러면 끌려감)
    void LateUpdate()
    {
        if (tipTargets == null || plantedPos == null) return;
        for (int i = 0; i < tipTargets.Length; i++)
            if (!isStepping[i]) tipTargets[i].position = plantedPos[i];
    }

    private Vector3 HomeDir(int i)
    {
        float phi = Vector2.SignedAngle(new Vector2(body.forward.x, body.forward.z), Vector2.up);
        float psi = (theta[i] + phi) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(psi), 0f, Mathf.Cos(psi));
    }

    private Vector3 StancePos(int i)
    {
        Vector3 p = body.position + HomeDir(i) * legRadius[i];
        return FootUtil.SetTargetNearest(p, ground);
    }

    private Vector3 ClampToLegArc(int i, Vector3 target)
    {
        Vector3 fromBody = target - body.position; fromBody.y = 0f;
        if (fromBody.sqrMagnitude < 0.0001f) return StancePos(i);

        Vector3 home = HomeDir(i);
        float ang = Mathf.Clamp(Vector3.SignedAngle(home, fromBody, Vector3.up), -maxLegAngle, maxLegAngle);
        float radius = Mathf.Clamp(fromBody.magnitude, legRadius[i] * minRadiusRatio, legRadius[i] * maxRadiusRatio);

        Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * home;
        Vector3 p = body.position + dir * radius;
        return FootUtil.SetTargetNearest(p, ground);
    }

    private void TryStepLegs(Vector3 moveDir)
    {
        int steppingCount = 0;
        for (int i = 0; i < isStepping.Length; i++) if (isStepping[i]) steppingCount++;
        if (steppingCount >= maxLegsStepping) return;

        int bestLeg = -1;
        float bestDrift = stepThreshold;

        for (int i = 0; i < tipTargets.Length; i++)
        {
            if (isStepping[i]) continue;
            Vector3 stance = StancePos(i);
            float drift = Horizontal(tipTargets[i].position - stance);
            if (drift <= bestDrift) continue;
            if (NeighborStepping(i)) continue;
            bestDrift = drift;
            bestLeg = i;
        }

        if (bestLeg >= 0)
        {
            Vector3 target = StancePos(bestLeg) + moveDir * (stride * 0.5f);
            target = ClampToLegArc(bestLeg, target);
            StartCoroutine(StepLeg(bestLeg, target));
        }
    }

    private bool NeighborStepping(int i)
    {
        for (int j = 0; j < isStepping.Length; j++)
        {
            if (j == i || !isStepping[j]) continue;
            if (Mathf.Abs(Mathf.DeltaAngle(theta[i], theta[j])) < neighborAngle) return true;
        }
        return false;
    }

    private IEnumerator StepLeg(int i, Vector3 target)
    {
        isStepping[i] = true;
        Vector3 start = tipTargets[i].position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, stepDuration);
            Vector3 p = Vector3.Lerp(start, target, t);
            p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * stepHeight;
            tipTargets[i].position = p;
            yield return null;
        }
        tipTargets[i].position = target;
        plantedPos[i] = target;      // 착지 위치 저장 → 이후 고정
        isStepping[i] = false;
    }

    private void AdjustBody()
    {
        if (tipTargets.Length == 0) return;

        Vector3 avg = Vector3.zero;
        Vector3 up = Vector3.zero;
        for (int i = 0; i < tipTargets.Length; i++)
        {
            avg += tipTargets[i].position;
            if (Physics.Raycast(tipTargets[i].position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ground))
                up += hit.normal;
        }
        avg /= tipTargets.Length;
        if (up.sqrMagnitude < 0.0001f) up = Vector3.up;
        up.Normalize();

        Vector3 bp = body.position;
        bp.y = Mathf.Lerp(bp.y, avg.y + bodyHeight, Time.deltaTime * heightAdjustSpeed);
        body.position = bp;

        Vector3 targetUp = Vector3.Slerp(Vector3.up, up, tiltToGround);
        Vector3 fwd = Vector3.ProjectOnPlane(body.forward, targetUp).normalized;
        if (fwd.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(fwd, targetUp);
            body.rotation = Quaternion.Slerp(body.rotation, targetRot, Time.deltaTime * heightAdjustSpeed);
        }
    }

    // 방 homeBound 안으로 수평 clamp (경계에서 멈춤). 방 없으면 그대로.
    private Vector3 ClampToRoom(Vector3 pos)
    {
        if (owner != null && owner.IsControlled) return pos;   // 조종 중엔 플레이어 따라 방 넘나들게
        if (owner == null || owner.currentRoom == null || owner.currentRoom.homeBound == null) return pos;
        Bounds b = owner.currentRoom.homeBound.bounds;
        Vector3 inset = new Vector3(b.extents.x - 0.5f, b.extents.y, b.extents.z - 0.5f);
        Vector3 c = b.center;
        pos.x = Mathf.Clamp(pos.x, c.x - inset.x, c.x + inset.x);
        pos.z = Mathf.Clamp(pos.z, c.z - inset.z, c.z + inset.z);
        return pos;
    }

    private static float Horizontal(Vector3 v) { v.y = 0f; return v.magnitude; }
}
