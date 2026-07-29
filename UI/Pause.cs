using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public static Pause Instance;
    public GameObject panelRoot;
    public Image bg;

    [Header("Fade")]
    [Tooltip("배경+메뉴를 함께 감싸는 오브젝트의 CanvasGroup. 알파가 같이 연동됨")]
    public CanvasGroup canvasGroup;
    public float fadeTime = 0.2f;

    private Coroutine fadeCo;

    [System.Serializable]
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    public MenuItem[] items;

    private int selected = 0;

    private void Start()
    {
        UpdateVisual();
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelRoot != null) panelRoot.SetActive(false);
    }


    public void Open()
    {
        Time.timeScale = 0f;
        if (panelRoot != null) panelRoot.SetActive(true);
        StartFade(1f);
    }

    public void Close()
    {
        StartFade(0f, deactivateOnEnd: true);
        Time.timeScale = 1f;
    }

    // CanvasGroup 알파를 unscaled time으로 페이드 (timeScale=0에서도 진행)
    private void StartFade(float targetAlpha, bool deactivateOnEnd = false)
    {
        if (canvasGroup == null)
        {
            // CanvasGroup 없으면 즉시 처리
            if (deactivateOnEnd && panelRoot != null) panelRoot.SetActive(false);
            return;
        }

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeRoutine(targetAlpha, deactivateOnEnd));
    }

    private IEnumerator FadeRoutine(float target, bool deactivateOnEnd)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, fadeTime > 0f ? t / fadeTime : 1f);
            yield return null;
        }
        canvasGroup.alpha = target;

        if (deactivateOnEnd && panelRoot != null) panelRoot.SetActive(false);
        fadeCo = null;
    }
    public void Move(int dir)
    {
        selected = (selected + dir + items.Length) % items.Length;
        UpdateVisual();
    }

    public void Confirm()
    {
        if (items.Length == 0) return;
        switch (items[selected].label)
        {
            case "RETURN TO MAIN": LoadScene("title"); break;
            case "RESTART_LEVEL": RestartLevel(); break;
            case "Quit": Application.Quit(); break;
        }
    }

    private static void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    private static void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void UpdateVisual()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            items[i].entry.Set(items[i].label, i == selected);
        }
    }


    public void OnStartButton() => LoadScene("title");
    public void OnQuitButton() => Application.Quit();
}
