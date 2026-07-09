using System.Collections;
using UnityEngine;
using TMPro;

// 짧은 반응 알림용 토스트. 페이드 인 → 유지 → 페이드 아웃 후 사라짐.
// 씬: Canvas 밑에 CanvasGroup + TMP_Text 붙은 오브젝트에 이 스크립트 부착.
[RequireComponent(typeof(CanvasGroup))]
public class ToastUI : MonoBehaviour
{
    public static ToastUI Instance { get; private set; }

    [Header("References")]
    public TMP_Text label;

    [Header("Anim")]
    public float fadeTime = 0.2f;
    public float defaultDuration = 1.5f;

    private CanvasGroup group;
    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        if (label != null) label.text = message;

        if (routine != null) StopCoroutine(routine);   // 연속 호출 시 최신 메시지 우선
        routine = StartCoroutine(ShowRoutine(duration));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        yield return Fade(group.alpha, 1f);

        yield return new WaitForSeconds(duration);

        yield return Fade(group.alpha, 0f);
        routine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;   // 일시정지(timeScale=0) 중에도 동작
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        group.alpha = to;
    }
}
