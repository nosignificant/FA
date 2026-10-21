using UnityEngine;
using CreatureTypes;

public class WeedThink : Think2
{
    [Header("Weed")]
    [Tooltip("wander 목적지 도달 판정 거리")]
    public float weedReachThreshold = 5f;   // Think2.wanderReachThreshold와 이름 충돌 방지
    [Tooltip("목적지 도달 후 그 자리에 머무는 시간(초). 0이면 바로 다음 점으로")]
    public float dwellTime = 0f;
    [Tooltip("한 타겟을 계속 주목하는 최대 시간(초). 지나면 다른 타겟을 찾음. 0=무제한")]
    public float maxTrackTime = 0f;

    [Header("원궤도 wander (orbit) — 타겟 없을 때")]
    [Tooltip("켜면 랜덤 배회 대신, 아래로 쏜 raycast 지점 위에서 원을 그리며 돈다 (밀집 방지)")]
    public bool orbitWander = false;
    [Tooltip("원 반경(m)")]
    public float orbitRadius = 2f;
    [Tooltip("raycast 지점에서 위로 띄울 높이(m) — 원 중심 높이")]
    public float orbitHeight = 2f;
    [Tooltip("도는 속도(도/초). 음수면 반대 방향")]
    public float orbitAngularSpeed = 90f;
    [Tooltip("원 중심을 찾을 raycast가 맞을 바닥 레이어")]
    public LayerMask orbitGroundMask = ~0;
    [Tooltip("아래로 쏘는 raycast 최대 길이(m)")]
    public float orbitRaycastDist = 50f;

    [Tooltip("개체마다 도는 속도·방향·반경을 랜덤하게 흩어 동기화된(어색한) 느낌 방지")]
    public bool orbitVaryPerInstance = true;
    [Tooltip("속도 랜덤 편차 (0.4=±40%)")]
    [Range(0f, 1f)] public float orbitSpeedJitter = 0.4f;
    [Tooltip("반경 랜덤 편차 (0.3=±30%)")]
    [Range(0f, 1f)] public float orbitRadiusJitter = 0.3f;

    [System.NonSerialized] public float orbitAngle;
    [System.NonSerialized] private Vector3 orbitCenter;
    [System.NonSerialized] public bool orbitCenterValid;   // 타겟 추적 들어가면 false로 → 복귀 시 재앵커

    // 개체별 고정 랜덤 (한 번만 정해 personality 유지 — 재앵커해도 안 바뀜)
    [System.NonSerialized] private bool _orbitVarsInit;
    [System.NonSerialized] private float _orbitSpeedMul = 1f;
    [System.NonSerialized] private float _orbitDir = 1f;
    [System.NonSerialized] private float _orbitRadiusMul = 1f;

    // 타겟 없을 때 호출: 원 중심을 (아래 raycast 지점 + 높이)로 한 번 잡고, 매 tick 각도를 전진시켜 원 위의 점 반환
    public Vector3 ComputeOrbitPoint()
    {
        // 개체별 속도/방향/반경 랜덤 1회 결정
        if (orbitVaryPerInstance && !_orbitVarsInit)
        {
            _orbitSpeedMul = 1f + Random.Range(-orbitSpeedJitter, orbitSpeedJitter);
            _orbitDir = Random.value < 0.5f ? -1f : 1f;          // 시계/반시계 랜덤
            _orbitRadiusMul = 1f + Random.Range(-orbitRadiusJitter, orbitRadiusJitter);
            _orbitVarsInit = true;
        }

        if (!orbitCenterValid)
        {
            Vector3 from = self.rootTransform.position;
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, orbitRaycastDist, orbitGroundMask, QueryTriggerInteraction.Ignore))
                orbitCenter = hit.point + Vector3.up * orbitHeight;
            else
                orbitCenter = from + Vector3.up * orbitHeight;   // 바닥 못 찾으면 제자리 위
            orbitCenterValid = true;
            orbitAngle = Random.value * Mathf.PI * 2f;           // 시작 각도 랜덤 → 여럿이면 서로 흩어짐
        }

        orbitAngle += orbitAngularSpeed * _orbitSpeedMul * _orbitDir * Mathf.Deg2Rad * waitInterval;   // tick마다 전진
        float r = orbitRadius * _orbitRadiusMul;
        return orbitCenter + new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * r;
    }

    protected WeedWanderState weedWander;

    protected override void Awake()
    {
        base.Awake();
        weedWander = CreateWanderState();
    }

    // 하위 클래스가 다른 wander 상태로 교체할 수 있게 (예: CCDIKThink)
    protected virtual WeedWanderState CreateWanderState()
    {
        return new WeedWanderState(this)
        {
            reachThreshold = weedReachThreshold,
            dwellTime = dwellTime,
            maxTrackTime = maxTrackTime,
        };
    }

    // weed는 관계 없이도 주변 아무 생물에게나 반응 → 관계 검사 없이 유효 판정
    public override bool IsValidTarget(Creature target)
    {
        if (target == null || target == self || target.IsDead || target.data == null) return false;
        // weed끼리·문은 서로 무시
        var id = target.data.creatureID;
        if (id == CreatureID.Weed || id == CreatureID.WalkingWeed || id == CreatureID.Door) return false;
        return true;
    }

    // weed는 이동 의도를 갖지 않음 — 항상 Wander(반경 반응 + 배회)
    protected override CreatureIntent DetermineIntent() => CreatureIntent.Wander;

    // 어떤 intent든 weed 반응 상태로
    protected override ThinkState GetThinkState(CreatureIntent intent) => weedWander;
}
