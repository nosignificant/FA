using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 생물 1마리 = 텍스트박스 1개. 메뉴 항목으로도 재사용 — 마우스 hover/클릭 지원.
public class Entry : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public TMP_Text label;
    [Tooltip("선택 시 테두리로 쓸 Image (선택 안 됐을 땐 투명). 마우스 hover/클릭을 받으려면 Raycast Target 켜기")]
    public Image border;
    public Color selectedColor = Color.white;
    public Color normalColor = Color.white;

    // 메뉴가 연결 — 마우스 올리면 onHover(선택 이동), 클릭하면 onClick(실행). 비워두면 마우스 입력 무시.
    private System.Action _onHover, _onClick;
    public void BindMouse(System.Action onHover, System.Action onClick)
    {
        _onHover = onHover;
        _onClick = onClick;
    }

    public void Set(string itemLabel, bool selected)
    {
        if (label != null) label.text = itemLabel;
        if (border != null) border.color = selected ? selectedColor : normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData) => _onHover?.Invoke();
    public void OnPointerClick(PointerEventData eventData) => _onClick?.Invoke();
}
