using UnityEngine;

// 스프라이트 바리에이션 묶음. 여기에 후보들을 넣어두면 SpriteVariantPicker가 골라 씀.
// 종류 추가는 이 애셋에 스프라이트만 추가하면 됨(코드 수정 X).
[CreateAssetMenu(fileName = "SpriteVariantSet", menuName = "Game/Sprite Variant Set")]
public class SpriteVariantSet : ScriptableObject
{
    public Sprite[] variants;

    public int Count => variants != null ? variants.Length : 0;

    public Sprite Get(int index)
    {
        if (variants == null || variants.Length == 0) return null;
        index = Mathf.Clamp(index, 0, variants.Length - 1);
        return variants[index];
    }

    // 드롭다운 표시용 이름 목록
    public string[] Names()
    {
        if (variants == null) return new string[0];
        var names = new string[variants.Length];
        for (int i = 0; i < variants.Length; i++)
            names[i] = variants[i] != null ? variants[i].name : $"({i})";
        return names;
    }
}
