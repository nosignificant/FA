using UnityEngine;

// 이 오브젝트의 SpriteRenderer 그림을 SpriteVariantSet에서 골라 적용.
// 인스펙터에서 variantIndex를 드롭다운(커스텀 에디터)으로 이름 보고 선택.
[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]   // 에디터에서도 바로 미리보기
public class SpriteVariantPicker : MonoBehaviour
{
    public SpriteVariantSet set;
    public int variantIndex = 0;

    private SpriteRenderer _sr;
    private SpriteRenderer SR => _sr != null ? _sr : (_sr = GetComponent<SpriteRenderer>());

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();   // 인스펙터에서 값 바꾸면 즉시 반영

    public void Apply()
    {
        if (set == null || SR == null) return;
        var sp = set.Get(variantIndex);
        if (sp != null) SR.sprite = sp;
    }
}
