using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 생물 1마리 = 텍스트박스 1개
public class ObservationEntry : MonoBehaviour
{
    public TMP_Text label;
    [Tooltip("선택 시 테두리로 쓸 Image (선택 안 됐을 땐 투명)")]
    public Image border;
    public Color selectedColor = Color.white;
    public Color normalColor = new Color(1f, 1f, 1f, 0f);   // 투명

    public void Set(string creatureName, string signature, int cur, int req, bool learned, bool selected)
    {
        if (label != null)
        {
            label.text = learned
                ? $"{creatureName}  [{signature}]  <color=#6fdc6f>✓</color>"
                : $"{creatureName}  [{signature}]  {cur}/{req}";
        }
        if (border != null) border.color = selected ? selectedColor : normalColor;
    }
}
