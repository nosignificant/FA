using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 씬 전환 시 로딩바를 띄우는 로더. 씬이 언로드돼도 살아남게 DontDestroyOnLoad.
// LevelPortal·TitleMenu 등이 SceneLoader.Instance.Load(sceneName)로 호출.
// 첫 씬(title 등)에 이 컴포넌트가 붙은 오브젝트를 하나 두면 이후 계속 유지된다.
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading UI")]
    public GameObject loadingPanel;      // 로딩 화면 전체 (평소엔 꺼둠)
    public Image fillBar;                // 진행바 (Image Type = Filled)
    public TMP_Text percentText;         // "12%" (선택)

    [Header("옵션")]
    [Tooltip("로딩이 너무 빨라도 최소 이 시간(초)은 로딩화면 유지")]
    public float minShowTime = 0.5f;
    [Tooltip("바가 실제 진행률로 부드럽게 따라가는 속도")]
    public float barLerpSpeed = 5f;

    private bool loading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void Load(string sceneName)
    {
        if (loading) return;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoader] sceneName이 비어 있습니다.");
            return;
        }
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        Time.timeScale = 1f;                     // 일시정지/빙의로 0이던 경우 대비

        if (loadingPanel != null) loadingPanel.SetActive(true);
        SetBar(0f);
        yield return null;                       // 패널 한 프레임 렌더

        float shown = 0f;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;         // 바가 100% 찰 때까지 활성화 보류

        float startTime = Time.unscaledTime;

        while (!op.isDone)
        {
            // op.progress는 0~0.9까지 로딩, 0.9에서 활성화 대기
            float target = Mathf.Clamp01(op.progress / 0.9f);
            shown = Mathf.MoveTowards(shown, target, barLerpSpeed * Time.unscaledDeltaTime);
            SetBar(shown);

            // 로딩 다 됐고(0.9) + 바도 다 찼고 + 최소 시간 지났으면 활성화
            bool ready = op.progress >= 0.9f && shown >= 0.999f
                         && Time.unscaledTime - startTime >= minShowTime;
            if (ready) op.allowSceneActivation = true;

            yield return null;
        }

        SetBar(1f);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        loading = false;
    }

    private void SetBar(float t)
    {
        if (fillBar != null) fillBar.fillAmount = t;
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(t * 100)}%";
    }
}
