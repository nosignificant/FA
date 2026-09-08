using CreatureTypes;

// a: 수동 입자 — 상호작용 없음. 열린 문으로 흐르기만 함(RoomMigration). Wander 고정.
public class Athink : TentacleThink
{
    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        if (DoesNeedToChase())
        {
            if (CanSynthesize()) return CreatureIntent.Synthesizing;
            return CreatureIntent.Chase;
        }
        return CreatureIntent.Wander;
    }
}
