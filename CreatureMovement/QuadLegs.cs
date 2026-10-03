using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 절차적 4족 보행 (drift 기반 한 발씩):
//  - 발마다 몸통 기준 "쉬는 자리(stance)"를 둠. 몸이 움직여 발이 stance에서 stepThreshold 넘게 벌어지면 내딛음.
//  - 인접(각도 가까운) 다리는 동시에 안 들어 지지 유지 → 자연스러운 걸음.
//  - 발은 호(sin arc)를 그리며 이동. 몸통 위치는 transform 직접, 높이는 발 평균에서 산출 (Rigidbody 관성 없음).
public class QuadLegs : MonoBehaviour
{
    [Header("Transform")]
    public Transform followingTarget;
    public Transform body;
    public Transform[] tipTargets;

    [Header("이동")]
    [Tooltip("타겟이 이 거리보다 멀면 몸통 이동")]
    public float followTriggerDist = 1f;
    public float moveSpeed = 3f;
    [Tooltip("몸통이 이동방향으로 도는 속도")]
    public float turnSpeed = 6f;

    [Header("발 딛기")]
    [Tooltip("발이 쉬는 자리에서 이만큼 벌어지면 내딛음")]
    public float stepThreshold = 2f;
    [Tooltip("한 걸음이 나아가는 거리(전방 예측)")]
    public float stride = 2.5f;
    [Tooltip("발 드는 높이(호)")]
    public float stepHeight = 1.5f;
    [Tooltip("한 걸음 걸리는 시간(초, 작을수록 빠름)")]
    public float stepDuration = 0.25f;
    [Tooltip("동시에 들 수 있는 최대 발 수 (1=조심스런 걸음, 2=대각선 속보)")]
    public int maxLegsStepping = 2;
    [Tooltip("이 각도 안의 인접 다리는 동시에 안 듦 (지지 유지)")]
    public float neighborAngle = 80f;
    [Tooltip("발이 자기 홈 방향에서 벗어날 수 있는 최대 각도 (작을수록 다리 안 꼬임)")]
    public float maxLegAngle = 40f;
    [Tooltip("발이 몸통에서 벌어질 수 있는 반경 배율 (홈 반경 대비 최소/최대)")]
    public float minRadiusRatio = 0.6f;
    public float maxRadiusRatio = 1.5f;

    [Header("몸통 높이/균형")]
    [Tooltip("발 평균 위로 이 높이만큼 몸통을 띄움")]
    public float bodyHeight = 1.5f;
    [Tooltip("몸통 높이 따라가는 속도")]
    public float heightAdjustSpeed = 8f;
    [Tooltip("바닥 기울기에 몸통을 맞추는 정도 (0=항상 수평)")]
    [Range(0f, 1f)] public float tiltToGround = 0.5f;

    public LayerMask ground;

    // 내부
    private float[] theta;        // 각 발의 몸통 정면 기준 방향 각(deg)
    private float[] legRadius;    // 각 발의 몸통 기준 반경
    private bool[] isStepping;
    private Vector3[] plantedPos; // 딛고 있는 발의 월드 위치 (몸 움직여도 고정)
    private Rigidbody rb;

    void Start()
    {
        if (body != null) rb = body.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;   // 물리 관성 제거 (transform으로 직접 제어)

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
            plantedPos[i] = tipTargets[i].position;   // 초기 착지 위치
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

        // 1) 몸통 이동·회전 (도착했으면 정지 → 발도 안 벌어져 스텝 안 함)
        if (moving)
        {
            // proxy(수평 위치)를 넘지 않게 다가감 — 지나쳐서 좌우로 흔들리는 것 방지
            Vector3 flatTarget = new Vector3(followingTarget.position.x, body.position.y, followingTarget.position.z);
            body.position = Vector3.MoveTowards(body.position, flatTarget, moveSpeed * Time.deltaTime);

            Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
            body.rotation = Quaternion.Slerp(body.rotation, look, Time.deltaTime * turnSpeed);
        }

        // 2) 발 딛기 판정 (벌어진 발을, 인접 다리 안 들 때, 최대 수만큼)
        TryStepLegs(moving ? moveDir : Vector3.zero);

        // 3) 몸통 높이/기울기 (발 평균 기준)
        AdjustBody();
    }

    // 몸 이동/회전 뒤에도 딛고 있는 발은 땅에 고정 (몸 자식이라 안 그러면 끌려감)
    void LateUpdate()
    {
        if (tipTargets == null || plantedPos == null) return;
        for (int i = 0; i < tipTargets.Length; i++)
            if (!isStepping[i]) tipTargets[i].position = plantedPos[i];
    }

    // 다리 i의 홈 방향(월드, 수평)
    private Vector3 HomeDir(int i)
    {
        float phi = Vector2.SignedAngle(new Vector2(body.forward.x, body.forward.z), Vector2.up);
        float psi = (theta[i] + phi) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(psi), 0f, Mathf.Cos(psi));
    }

    // 각 발의 현재 쉬는 자리(stance) 월드 위치 (바닥에 스냅)
    private Vector3 StancePos(int i)
    {
        Vector3 p = body.position + HomeDir(i) * legRadius[i];
        return FootUtil.SetTargetNearest(p, ground);
    }

    // 착지 목표를 홈 방향 부채꼴(각도·반경) 안으로 제한 → 다리 안 꼬임
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

        // 가장 많이 벌어진 발부터 내딛음
        int bestLeg = -1;
        float bestDrift = stepThreshold;   // 이 이상 벌어져야 후보

        for (int i = 0; i < tipTargets.Length; i++)
        {
            if (isStepping[i]) continue;

            Vector3 stance = StancePos(i);
            float drift = Horizontal(tipTargets[i].position - stance);
            if (drift <= bestDrift) continue;
            if (NeighborStepping(i)) continue;   // 인접 다리가 들고 있으면 이 발은 대기 (지지 유지)

            bestDrift = drift;
            bestLeg = i;
        }

        if (bestLeg >= 0)
        {
            // 목표 = stance + 이동방향으로 stride의 절반만큼 앞 예측, 홈 부채꼴로 제한(안 꼬이게)
            Vector3 target = StancePos(bestLeg) + moveDir * (stride * 0.5f);
            target = ClampToLegArc(bestLeg, target);
            StartCoroutine(StepLeg(bestLeg, target));
        }
    }

    // 이미 스텝 중인 다리 중 각도상 인접한 게 있나
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
            p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * stepHeight;   // 호(arc)
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

        // 발 평균 위치 + 노멀
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

        // 높이: 발 평균 + bodyHeight
        Vector3 bp = body.position;
        bp.y = Mathf.Lerp(bp.y, avg.y + bodyHeight, Time.deltaTime * heightAdjustSpeed);
        body.position = bp;

        // 기울기: 바닥 노멀에 맞춤 (tiltToGround로 정도 조절), forward는 유지
        Vector3 targetUp = Vector3.Slerp(Vector3.up, up, tiltToGround);
        Vector3 fwd = Vector3.ProjectOnPlane(body.forward, targetUp).normalized;
        if (fwd.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(fwd, targetUp);
            body.rotation = Quaternion.Slerp(body.rotation, targetRot, Time.deltaTime * heightAdjustSpeed);
        }
    }

    private static float Horizontal(Vector3 v) { v.y = 0f; return v.magnitude; }
}