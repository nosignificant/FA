using CreatureTypes;

public class WeedThink : Think2
{
    private WeedWanderState weedWander;

    protected override void Awake()
    {
        base.Awake();
        weedWander = new WeedWanderState(this);
    }

    // weed는 이동 의도를 갖지 않음 — 항상 Wander(제자리 반응)
    protected override CreatureIntent DetermineIntent() => CreatureIntent.Wander;

    // 어떤 intent든 weed 반응 상태로
    protected override ThinkState GetThinkState(CreatureIntent intent) => weedWander;
}
