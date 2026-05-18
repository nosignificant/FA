using UnityEngine;
using CreatureTypes;

public struct SynthesisResult
{
    public GameObject prefab;
    public int count;
    public static SynthesisResult None => default;
    public bool IsValid => prefab != null && count > 0;

    public static SynthesisResult Of(GameObject prefab, int count)
        => new SynthesisResult { prefab = prefab, count = count };
}

public class Synthesis : MonoBehaviour
{
    [Header("A Synthesis (1 grab)")]
    [Tooltip("A + h → ah")]
    public GameObject ahPrefab;
    [Tooltip("A + s → as")]
    public GameObject asPrefab;

    [Header("AA Synthesis — 결과는 a")]
    [Tooltip("AA가 합성한 결과로 만드는 a 프리팹")]
    public GameObject aPrefab;

    [Header("L Synthesis (2 grabs)")]
    [Tooltip("L + h+h → hh")]
    public GameObject hhPrefab;
    [Tooltip("L + s+s → ss")]
    public GameObject ssPrefab;
    [Tooltip("L + (a 포함) → aa")]
    public GameObject aaPrefab;

    public SynthesisResult Resolve(CreatureID selfID, CreatureID idA, CreatureID idB)
    {
        if (selfID == CreatureID.A) return ResolveA(idA);
        if (selfID == CreatureID.AA) return ResolveAA(idA, idB);
        if (selfID == CreatureID.L) return ResolveL(idA, idB);
        return SynthesisResult.None;
    }

    // a + h → ah, a + s → as
    SynthesisResult ResolveA(CreatureID idA)
    {
        if (idA == CreatureID.H) return SynthesisResult.Of(ahPrefab, 1);
        if (idA == CreatureID.S) return SynthesisResult.Of(asPrefab, 1);
        return SynthesisResult.None;
    }

    // h/s → a 1개, ah/as 또는 a+a → aa 1개
    SynthesisResult ResolveAA(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.H || idA == CreatureID.S) return SynthesisResult.Of(aPrefab, 1);
        if (idA == CreatureID.AH || idA == CreatureID.AS) return SynthesisResult.Of(aaPrefab, 1);
        if (idA == CreatureID.A && idB == CreatureID.A) return SynthesisResult.Of(aaPrefab, 1);
        if (idA == CreatureID.A) return SynthesisResult.Of(aPrefab, 1);
        return SynthesisResult.None;
    }

    // L: h+h → hh, s+s → ss, a 포함 → aa
    SynthesisResult ResolveL(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.A || idB == CreatureID.A)
            return SynthesisResult.Of(aaPrefab, 1);

        if (idA != idB) return SynthesisResult.None;
        if (idA == CreatureID.H) return SynthesisResult.Of(hhPrefab, 1);
        if (idA == CreatureID.S) return SynthesisResult.Of(ssPrefab, 1);
        return SynthesisResult.None;
    }
}
