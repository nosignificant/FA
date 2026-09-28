using UnityEngine;
using System.Collections;

// 몸이 리드하는 procedural IK 걷기 (참고: youtube e6Gjhr1IP6w 10단계)
// - 몸은 followingTarget 쪽으로 이동/회전한다 (리드).
// - 발마다 몸에 붙은 홈 타겟이 있고, 발이 그 타겟에서 stride 이상 벌어지면
//   독립적으로 한 발씩 딛는다 (step 5~6).
// - 대각 그룹이 번갈아 뜨도록 gait 잠금 (step 8).
// - 몸 높이 = 발 평균 + standHeight (step 9), 몸 기울기 = 발 높이차 (step 10).
public class QuadLegs : MonoBehaviour
{
    [Header("Transform")]
    public Transform followingTarget;
    public Transform body;
    public Transform[] tipTargets;

    [Header("Move")]
    public float followTriggerDist = 1f;   // 이 거리 넘으면 몸 이동
    public float stride = 5f;              // 발이 홈에서 이만큼 벌어지면 스텝
    public float moveSpeed = 3f;           // 몸 이동/회전 속도
    public float lateralDist = 1.5f;       // 발-몸통 옆 거리 (stance 반경)
    public float stepTime = 0.2f;          // 발 한 발짝 이동 시간
    public float stepHeight = 2f;          // 발 들어올리는 높이

    public bool doesNeedToRot = true;
    [Header("rotation")]
    public float rotationThreshold = 25f;  // 이 각도 넘으면 회전 우선(이동 억제)

    [Header("Body follow (step 9~10)")]
    public float standHeight = 3f;         // 발 평균 위 몸 높이
    public float bodyLerp = 8f;            // 몸 위치/회전 스무딩 강도
    public float tiltStrength = 6f;        // 발 높이차 → 기울기 스케일 (도/유닛)
    public float maxTilt = 25f;            // pitch/roll 최대 각도

    public LayerMask ground;

    // private
    private float[] theta;                 // 몸 forward 기준 발 방향 각도
    private int[] gaitGroup;               // 대각 그룹 (0/1)
    private bool[] isFront;                 // 앞다리 여부
    private bool[] isRight;                 // 오른다리 여부
    private bool[] isStepping;              // 현재 딛는 중인 발
    private Vector3 movingDir;             // 수평 이동 방향
    private bool moving;

    void Start()
    {
        int n = tipTargets.Length;
        theta = new float[n];
        gaitGroup = new int[n];
        isFront = new bool[n];
        isRight = new bool[n];
        isStepping = new bool[n];

        for (int i = 0; i < n; i++)
        {
            Vector3 offset = tipTargets[i].position - body.position;

            // 몸 정면 기준 발 방향 각도 (기존 로직 유지)
            theta[i] = Vector2.SignedAngle(
                new Vector2(offset.x, offset.z),
                new Vector2(body.forward.x, body.forward.z));

            // 앞/뒤·좌/우 분류 → 대각 gait 그룹
            Vector3 local = body.InverseTransformPoint(tipTargets[i].position);
            isFront[i] = local.z >= 0f;
            isRight[i] = local.x >= 0f;
            // 대각 쌍: (앞-오른, 뒤-왼) = 그룹0 / (앞-왼, 뒤-오른) = 그룹1
            gaitGroup[i] = (isFront[i] == isRight[i]) ? 0 : 1;
        }
    }

    void Update()
    {
        if (body == null || tipTargets == null || tipTargets.Length == 0) return;

        // 수평 이동 방향
        Vector3 flat = followingTarget.position - body.position;
        flat.y = 0f;
        float dist = flat.magnitude;
        movingDir = dist > 0.0001f ? flat / dist : body.forward;
        moving = dist > followTriggerDist;

        float yawAngle = Vector3.SignedAngle(body.forward, movingDir, Vector3.up);
        bool rotatingFirst = doesNeedToRot && Mathf.Abs(yawAngle) > rotationThreshold;

        // 1) 몸 수평 이동 (리드). 큰 회전이 필요하면 제자리 회전 우선.
        if (moving && !rotatingFirst)
        {
            float moveDist = Mathf.Min(moveSpeed * Time.deltaTime, dist);
            Vector3 next = body.position + movingDir * moveDist;
            next.y = body.position.y;   // Y는 아래(step 9)에서 따로 제어
            body.position = next;
        }

        // 2) 발별 독립 스텝 + gait 잠금 (step 5~6, 8)
        for (int i = 0; i < tipTargets.Length; i++)
        {
            if (isStepping[i]) continue;

            Vector3 home = HomeTarget(i);
            float planarDist = new Vector2(
                tipTargets[i].position.x - home.x,
                tipTargets[i].position.z - home.z).magnitude;

            if (planarDist > stride && CanStep(i))
                StartCoroutine(StepFoot(i, home));
        }

        // 3) 몸 높이(step 9) + 회전(yaw + 발 높이차 tilt, step 10)
        UpdateBodyPose();
    }

    // 몸에 붙은 발 홈 타겟 (step 3~4)
    Vector3 HomeTarget(int i)
    {
        float phi = Vector2.SignedAngle(new Vector2(body.forward.x, body.forward.z), Vector2.up);
        float psi = (theta[i] + phi) * Mathf.Deg2Rad;
        Vector3 footDir = new Vector3(Mathf.Sin(psi), 0, Mathf.Cos(psi));

        // 진행방향으로 앞서 딛기 (정지 시엔 lateral stance만)
        float proj = moving ? Mathf.Max(0f, Vector3.Dot(movingDir, footDir)) : 0f;
        Vector3 target = body.position + footDir * lateralDist + movingDir * (stride * proj);

        // 아래로 레이캐스트해 땅에 붙이기 (step 4). 못 찾으면 현재 발 유지.
        if (FootUtil.TryGround(target, ground, out Vector3 g)) target = g;
        else target.y = tipTargets[i].position.y;
        return target;
    }

    // 반대 대각 그룹이 전부 땅에 닿아있을 때만 뗄 수 있음 (step 8)
    bool CanStep(int i)
    {
        for (int j = 0; j < tipTargets.Length; j++)
            if (gaitGroup[j] != gaitGroup[i] && isStepping[j]) return false;
        return true;
    }

    IEnumerator StepFoot(int i, Vector3 target)
    {
        isStepping[i] = true;
        yield return StartCoroutine(FootUtil.lerpMove(tipTargets[i], target, stepTime, stepHeight));
        tipTargets[i].position = target;
        isStepping[i] = false;
    }

    // 몸 위치(Y=발평균+standHeight) + 회전(yaw + 발 높이차 기울기)
    void UpdateBodyPose()
    {
        int n = tipTargets.Length;
        float avgY = 0f, frontY = 0f, backY = 0f, leftY = 0f, rightY = 0f;
        int fN = 0, bN = 0, lN = 0, rN = 0;

        for (int i = 0; i < n; i++)
        {
            float y = tipTargets[i].position.y;
            avgY += y;
            if (isFront[i]) { frontY += y; fN++; } else { backY += y; bN++; }
            if (isRight[i]) { rightY += y; rN++; } else { leftY += y; lN++; }
        }
        avgY /= n;
        if (fN > 0) frontY /= fN;
        if (bN > 0) backY /= bN;
        if (lN > 0) leftY /= lN;
        if (rN > 0) rightY /= rN;

        float smooth = 1f - Mathf.Exp(-bodyLerp * Time.deltaTime);

        // step 9: 몸 높이 = 발 평균 + standHeight
        Vector3 pos = body.position;
        pos.y = Mathf.Lerp(pos.y, avgY + standHeight, smooth);
        body.position = pos;

        // yaw: 이동방향 바라보기 (수평 고정)
        Vector3 flatForward = movingDir;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) { flatForward = body.forward; flatForward.y = 0f; }
        Quaternion yawRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);

        // step 10: 앞뒤/좌우 발 높이차 → pitch/roll
        float pitch = Mathf.Clamp((backY - frontY) * tiltStrength, -maxTilt, maxTilt);
        float roll = Mathf.Clamp((leftY - rightY) * tiltStrength, -maxTilt, maxTilt);
        Quaternion tiltRot = Quaternion.Euler(pitch, 0f, roll);

        body.rotation = Quaternion.Slerp(body.rotation, yawRot * tiltRot, smooth);
    }
}
