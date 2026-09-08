using UnityEngine;
using CreatureTypes;

public struct SynthesisResult
{
    public CreatureID resultID;
    public int count;
    public bool valid;

    public static SynthesisResult None => default;   // valid = false
    public bool IsValid => valid && count > 0;

    public static SynthesisResult Of(CreatureID id, int count)
        => new SynthesisResult { resultID = id, count = count, valid = true };
}

public class Synthesis : MonoBehaviour
{
    public SynthesisResult Resolve(CreatureID selfID, CreatureID idA, CreatureID idB)
    {
        if (selfID == CreatureID.AA) return ResolveAA(idA, idB);
        return SynthesisResult.None;
    }

    // AA: L → A 1개 (변환)
    SynthesisResult ResolveAA(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.L) return SynthesisResult.Of(CreatureID.A, 1);
        return SynthesisResult.None;
    }
}
