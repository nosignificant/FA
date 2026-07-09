using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 생물 1마리 = 텍스트박스 1개
public class Entry : MonoBehaviour
{
    public TMP_Text label;
    [Tooltip("선택 시 테두리로 쓸 Image (선택 안 됐을 땐 투명)")]
    public Image border;
    public Color selectedColor = Color.white;
    public Color normalColor = Color.white;

    public void Set(string itemLabel, bool selected)
    {
        if (label != null) label.text = itemLabel;
        if (border != null) border.color = selected ? selectedColor : normalColor;
    }

    public void Set(string creatureName, string signature, bool selected)
    {
        if (label != null)
            label.text = $"{creatureName}  [{signature}]";
        if (border != null) border.color = selected ? selectedColor : normalColor;
    }
}
