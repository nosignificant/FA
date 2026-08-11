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
        if (selfID == CreatureID.A) return ResolveA(idA);
        if (selfID == CreatureID.AA) return ResolveAA(idA, idB);
        if (selfID == CreatureID.LL) return ResolveL(idA, idB);   // 합성은 LL만 (L은 생산기)
        return SynthesisResult.None;
    }

    // a + h → a 2개, a + s → a 2개
    SynthesisResult ResolveA(CreatureID idA)
    {
        if (idA == CreatureID.H) return SynthesisResult.Of(CreatureID.A, 2);
        if (idA == CreatureID.S) return SynthesisResult.Of(CreatureID.A, 2);
        return SynthesisResult.None;
    }

    // h/s → a 1개, ah/as 또는 a+a → aa 1개
    SynthesisResult ResolveAA(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.H || idA == CreatureID.S) return SynthesisResult.Of(CreatureID.A, 1);
        if (idA == CreatureID.AH || idA == CreatureID.AS) return SynthesisResult.Of(CreatureID.AA, 1);
        if (idA == CreatureID.A && idB == CreatureID.A) return SynthesisResult.Of(CreatureID.AA, 1);
        if (idA == CreatureID.A) return SynthesisResult.Of(CreatureID.A, 1);
        return SynthesisResult.None;
    }

    // L: h+h → hh, s+s → ss, a 포함 → aa
    SynthesisResult ResolveL(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.A || idB == CreatureID.A)
            return SynthesisResult.Of(CreatureID.AA, 1);

        if (idA != idB) return SynthesisResult.None;
        if (idA == CreatureID.H) return SynthesisResult.Of(CreatureID.HH, 1);
        if (idA == CreatureID.S) return SynthesisResult.Of(CreatureID.SS, 1);
        return SynthesisResult.None;
    }
}
