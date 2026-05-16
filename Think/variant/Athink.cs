using CreatureTypes;

// a: h/s를 쫓아 grab → 합성(ah/as). hh/ss로부터 도망.
// 행동 규칙은 Interaction.GetActionForA에 정의돼 있음.
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
