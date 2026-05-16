using CreatureTypes;

// ah, as: L에게서 도망, AA에게 다가감 (AA가 잡아 aa로 합성).
// 자기는 합성 안 함. 행동 규칙은 Interaction.GetActionForAssembledA에 정의됨.
public class AHASthink : Think2
{
    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;   // L 회피
        if (DoesNeedToChase()) return CreatureIntent.Chase;  // AA 접근
        return CreatureIntent.Wander;
    }
}
