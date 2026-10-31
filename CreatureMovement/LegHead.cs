using UnityEngine;

public class LegHead : MonoBehaviour
{
    [Header("References")]
    public Leg[] legs;
    public Transform target;
    public TargetControl tc;
    public LayerMask ground;
    public float height = 10f;
    public float speed = 5f;

    [Header("벽 등반 (wall climb)")]
    [Tooltip("켜면 진행 방향 앞에 수직 벽이 있을 때 그 벽을 타고 목표 높이까지 올라감")]
    public bool climbWalls = false;
    [Tooltip("벽 탐지 레이어(바닥+벽). 비우면 ground 사용")]
    public LayerMask surfaceMask;
    [Tooltip("목표 수평 방향으로 벽을 찾는 거리")]
    public float wallProbeDist = 4f;
    [Tooltip("벽면에서 몸을 띄울 거리")]
    public float wallStandoff = 2f;
    [Tooltip("벽 타고 올라가는 속도(m/s). 느리면 키우세요")]
    public float climbSpeed = 12f;
    [Tooltip("등반 중 발을 몸 좌우로 벌릴 거리")]
    public float footSpreadX = 2f;
    [Tooltip("등반 중 발을 몸 위아래로 벌릴 거리")]
    public float footSpreadY = 2f;
    [Tooltip("발이 목표 자리에서 이만큼 벗어나면 스텝(들었다 놓기)")]
    public float climbStepThreshold = 2f;
    [Tooltip("한 스텝에 걸리는 시간(초)")]
    public float climbStepTime = 0.18f;
    [Tooltip("스텝 시 발을 벽에서 들어올리는 높이")]
    public float climbStepHeight = 1.5f;
    [Tooltip("등반 진단 로그(0.3초)")]
    public bool debugClimb = false;
    private float _dbgNext;
    private float _walkDbgNext;
    private bool _climbingFeet;

    // 벽 스텝 상태(발별)
    private Vector3[] _anchors;      // 현재 디딘 지점
    private bool[] _stepping;
    private float[] _stepT;
    private Vector3[] _stepFrom, _stepTo;
    private bool _climbInit;

    private void Awake()
    {
        if (tc == null) tc = GetComponent<TargetControl>();
        if (tc == null) tc = GetComponentInParent<TargetControl>();
        ground = LayerMask.GetMask("Ground");
    }

    private void Start()
    {
        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>();
        ApplyTargetToLegs(transform);

    }

    void Update()
    {
        if (target != null) LerpHead();

    }

    private void LerpHead()
    {
        // 벽 등반 모드: 진행 방향 앞에 수직 벽이 있으면 그 벽을 타고 목표 높이로 올라감
        if (climbWalls && TryClimb()) return;

        // 등반에서 벗어났으면 잠근 발 풀기 (평지 보행 복귀)
        if (_climbingFeet) { UnlockClimbFeet(); _climbingFeet = false; }

        // 평지: 목표 X,Z에서 아래로 Ground를 찾아 바닥높이+height에 몸을 둠.
        // Ground가 없으면 공중 좌표를 따라 올라가지 않고 현재 위치 유지.
        Vector3 groundProbe = new Vector3(
            target.position.x,
            transform.position.y - height,
            target.position.z
        );
        if (!FootUtil.TryGround(groundProbe, ground, out Vector3 groundPos))
        {
            if (debugClimb && Time.time >= _walkDbgNext)
            {
                _walkDbgNext = Time.time + 0.3f;
                Debug.Log($"[Walk:{name}] 바닥 못 찾음 → 정지. probeXZ=({groundProbe.x:0.0},{groundProbe.z:0.0}) " +
                          $"probeY={groundProbe.y:0.0} groundMask={ground.value} target=({target.position.x:0.0},{target.position.y:0.0},{target.position.z:0.0})", this);
            }
            return;
        }

        Vector3 targetPos = new Vector3(target.position.x, groundPos.y + height, target.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
    }

    // 목표 수평 방향 앞에 '수직 벽'이 있으면 벽면에 붙어 목표(벽 위)까지 접선으로 올라간다.
    private bool TryClimb()
    {
        LayerMask mask = surfaceMask.value != 0 ? surfaceMask : ground;
        Vector3 toTarget = target.position - transform.position;
        Vector3 horiz = new Vector3(toTarget.x, 0f, toTarget.z);
        if (horiz.sqrMagnitude < 0.0001f) return false;

        bool hitWall = Physics.Raycast(transform.position, horiz.normalized, out RaycastHit wall, wallProbeDist, mask, QueryTriggerInteraction.Ignore);
        bool isVertical = hitWall && wall.normal.y < 0.5f;   // 바닥/경사면은 등반 아님

        if (debugClimb && Time.time >= _dbgNext)
        {
            _dbgNext = Time.time + 0.3f;
            Debug.Log($"[Climb:{name}] 앞벽={hitWall} 수직벽={isVertical} " +
                      $"{(hitWall ? $"normal={wall.normal} dist={wall.distance:0.0}" : $"(레이 {wallProbeDist} 안에 벽 없음)")} " +
                      $"bodyY={transform.position.y:0.0} targetY={target.position.y:0.0}", this);
        }

        if (!isVertical) return false;

        Vector3 normal = wall.normal;
        Vector3 stick = wall.point + normal * wallStandoff;                   // 벽면에 standoff 띄움
        Vector3 tangent = Vector3.ProjectOnPlane(target.position - stick, normal);  // 목표를 벽면에 투영 → 위로 올라가는 성분
        Vector3 desired = stick + tangent;

        transform.position = Vector3.MoveTowards(transform.position, desired, climbSpeed * Time.deltaTime);

        // up = 벽 노말, forward = 월드 위를 벽면에 투영(기어오르는 방향)
        Vector3 climbUp = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (climbUp.sqrMagnitude < 0.0001f) climbUp = transform.forward;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(climbUp.normalized, normal), Time.deltaTime * speed);

        // 발을 벽면에 직접 꽂아 몸과 함께 올라가게 (바닥에 남아 다리 늘어나는 것 방지)
        ClimbFeet(normal, climbUp.normalized);
        _climbingFeet = true;
        return true;
    }

    // 등반 중 네 발을 벽면에서 '스텝'(들었다 놓기)으로 배치. 디딘 발은 고정, 목표에서 멀어진 발만 대각 교대로 옮긴다.
    private void ClimbFeet(Vector3 normal, Vector3 climbUp)
    {
        if (legs == null || legs.Length == 0) return;
        EnsureClimbArrays();
        LayerMask mask = surfaceMask.value != 0 ? surfaceMask : ground;
        Vector3 right = Vector3.Cross(normal, climbUp).normalized;   // 벽면 좌우

        // 첫 프레임: 현재 발 위치를 앵커로
        if (!_climbInit)
        {
            for (int i = 0; i < legs.Length; i++)
                _anchors[i] = legs[i] != null && legs[i].tipTarget != null ? legs[i].tipTarget.position : transform.position;
            _climbInit = true;
        }

        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];
            if (leg == null) continue;

            // 이 발의 목표 자리(벽면 스냅)
            float sx = (i % 2 == 0) ? -1f : 1f;      // 좌/우
            float sy = (i < 2) ? 1f : -1f;           // 위/아래(앞/뒤)
            Vector3 probe = transform.position + right * footSpreadX * sx + climbUp * footSpreadY * sy - normal * wallStandoff;
            Vector3 desired = FootUtil.SetTargetNearest(probe, mask, footSpreadX + footSpreadY + 4f);

            if (_stepping[i])
            {
                // 들었다 놓기: 벽 법선 방향으로 띄우며 이동
                _stepT[i] = Mathf.Min(1f, _stepT[i] + Time.deltaTime / Mathf.Max(0.001f, climbStepTime));
                Vector3 arc = Vector3.Lerp(_stepFrom[i], _stepTo[i], _stepT[i]) + normal * (Mathf.Sin(_stepT[i] * Mathf.PI) * climbStepHeight);
                leg.SetFootAnchor(arc);
                if (_stepT[i] >= 1f) { _anchors[i] = _stepTo[i]; _stepping[i] = false; leg.SetFootAnchor(_stepTo[i]); }
            }
            else
            {
                bool far = (_anchors[i] - desired).sqrMagnitude > climbStepThreshold * climbStepThreshold;
                if (far && GaitAllows(i)) { _stepFrom[i] = _anchors[i]; _stepTo[i] = desired; _stepT[i] = 0f; _stepping[i] = true; }
                else leg.SetFootAnchor(_anchors[i]);   // 디딘 채 고정
            }
        }
    }

    // 대각 그룹 {0,3} / {1,2} — 반대 그룹이 스텝 중이면 대기(교대 보행)
    private bool GaitAllows(int i)
    {
        bool groupA = (i == 0 || i == 3);
        for (int j = 0; j < legs.Length; j++)
        {
            if (!_stepping[j]) continue;
            bool jA = (j == 0 || j == 3);
            if (jA != groupA) return false;
        }
        return true;
    }

    private void EnsureClimbArrays()
    {
        if (_anchors != null && _anchors.Length == legs.Length) return;
        _anchors = new Vector3[legs.Length];
        _stepping = new bool[legs.Length];
        _stepT = new float[legs.Length];
        _stepFrom = new Vector3[legs.Length];
        _stepTo = new Vector3[legs.Length];
    }

    private void UnlockClimbFeet()
    {
        _climbInit = false;
        if (_stepping != null) for (int i = 0; i < _stepping.Length; i++) _stepping[i] = false;
        if (legs == null) return;
        for (int i = 0; i < legs.Length; i++)
            if (legs[i] != null) legs[i].SetFootTargetLock(false);
    }

    private void ApplyTargetToLegs(Transform target)
    {
        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>();

        if (legs == null) return;

        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            leg.SetTarget(target);
        }
    }

    public bool CheckValidFootPos(Vector3 destPos, Leg legAdding)
    {
        if ((destPos - legAdding.tipTarget.position).sqrMagnitude < 0.2f * 0.2f)
            return false;

        if (legs == null) return true;

        float minDist = legAdding.stride * 0.7f;
        float minDistSqr = minDist * minDist;

        for (int i = 0; i < legs.Length; i++)
        {
            Leg otherLeg = legs[i];
            if (otherLeg == null) continue;
            if (otherLeg == legAdding) continue;

            Transform tip = otherLeg.tipTarget;
            if ((tip.position - destPos).sqrMagnitude < minDistSqr)
                return false;
        }
        return true;
    }
}
