using UnityEngine;
using TMPro;

// 생물 1마리 = 텍스트박스 1개
public class ObservationEntry : MonoBehaviour
{
    public TMP_Text label;
    [Tooltip("선택됐을 때 켜질 하이라이트 (선택)")]
    public GameObject highlight;

    public void Set(string creatureName, int cur, int req, bool learned, bool selected)
    {
        if (label != null)
        {
            label.text = learned
                ? $"{creatureName}  <color=#6fdc6f>✓</color>"
                : $"{creatureName}  {cur}/{req}";
        }
        if (highlight != null) highlight.SetActive(selected);
    }
}
