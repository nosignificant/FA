using UnityEngine;
using CreatureTypes;

public class WeedThink : Think2
{
    [Header("Weed")]
    [Tooltip("wander 목적지 도달 판정 거리")]
    public float weedReachThreshold = 5f;   // Think2.wanderReachThreshold와 이름 충돌 방지
    [Tooltip("목적지 도달 후 그 자리에 머무는 시간(초). 0이면 바로 다음 점으로")]
    public float dwellTime = 0f;

    protected WeedWanderState weedWander;

    protected override void Awake()
    {
        base.Awake();
        weedWander = CreateWanderState();
    }

    // 하위 클래스가 다른 wander 상태로 교체할 수 있게 (예: Tthink)
    protected virtual WeedWanderState CreateWanderState()
    {
        return new WeedWanderState(this)
        {
            reachThreshold = weedReachThreshold,
            dwellTime = dwellTime,
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
