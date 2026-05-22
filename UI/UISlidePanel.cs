using System.Collections;
using UnityEngine;

// 오른쪽에서 슬라이드 인/아웃 하는 UI 패널.
// Show() → 화면 안으로, Hide() → 오른쪽 밖으로.
[RequireComponent(typeof(RectTransform))]
public class UISlidePanel : MonoBehaviour
{
    [Header("Positions (anchoredPosition.x)")]
    [Tooltip("화면에 보일 때 x (보통 0)")]
    public float shownX = 0f;
    [Tooltip("숨었을 때 x — 화면 오른쪽 밖. 비워두면 패널 너비로 자동")]
    public float hiddenX = 0f;
    public bool autoHiddenX = true;

    [Header("Anim")]
    public float slideTime = 0.35f;
    [Tooltip("시작 시 숨긴 상태로")]
    public bool startHidden = true;

    private RectTransform rt;
    private Coroutine anim;
    public bool IsShown { get; private set; }

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (autoHiddenX) hiddenX = rt.rect.width;   // 패널 너비만큼 오른쪽 밖

        if (startHidden)
        {
            SetX(hiddenX);
            IsShown = false;
        }
        else
        {
            SetX(shownX);
            IsShown = true;
        }
    }

    public void Show()
    {
        if (IsShown) return;
        IsShown = true;
        StartSlide(shownX);
    }

    public void Hide()
    {
        if (!IsShown) return;
        IsShown = false;
        StartSlide(hiddenX);
    }

    public void Toggle()
    {
        if (IsShown) Hide();
        else Show();
    }

    private void StartSlide(float targetX)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(SlideTo(targetX));
    }

    private IEnumerator SlideTo(float targetX)
    {
        float startX = rt.anchoredPosition.x;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, slideTime);
            // ease-out
            float e = 1f - (1f - t) * (1f - t);
            SetX(Mathf.Lerp(startX, targetX, e));
            yield return null;
        }
        SetX(targetX);
        anim = null;
    }

    private void SetX(float x)
    {
        Vector2 p = rt.anchoredPosition;
        p.x = x;
        rt.anchoredPosition = p;
    }
}
