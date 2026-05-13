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

    [Header("AA Synthesis (1 grab) — 결과는 a")]
    [Tooltip("AA가 합성한 결과로 만드는 a 프리팹 (h/s 잡으면 1개, ah/as 잡으면 2개)")]
    public GameObject aPrefab;

    [Header("L Synthesis (2 grabs)")]
    [Tooltip("L + h+h → hh")]
    public GameObject hhPrefab;
    [Tooltip("L + s+s → ss")]
    public GameObject ssPrefab;
    [Tooltip("L + (anything containing a) → aa")]
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

    //무조건 2개여야 합성
    // ab둘 중하나라도 h나 s면 a1개
    // ah나 as가 들어오면 aa로 만듦
    SynthesisResult ResolveAA(CreatureID idA, CreatureID idB)
    {
        if (idA == CreatureID.H || idA == CreatureID.S) return SynthesisResult.Of(aPrefab, 1);
        if (idA == CreatureID.AH || idA == CreatureID.AS) return SynthesisResult.Of(aaPrefab, 1);
        if (idA == CreatureID.A && idB == CreatureID.A) return SynthesisResult.Of(aaPrefab, 1);
        if (idA == CreatureID.A) return SynthesisResult.Of(aPrefab, 1);
        return SynthesisResult.None;
    }

    // L: a 들어가면 무조건 aa, 아니면 같은 종 짝일 때만 hh/ss
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
