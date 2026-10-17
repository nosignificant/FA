using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CreatureTypes;

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

    [Header("로딩 라벨 이미지 조각 (SetActive로 조립)")]
    [Tooltip("'level' 글자 이미지 (loading/clear 둘 다에서 켜짐)")]
    public GameObject levelWord;
    [Tooltip("숫자 이미지: [0]=1, [1]=2, [2]=3 ... (레벨번호-1)")]
    public GameObject[] levelDigits;
    [Tooltip("'loading' 글자 이미지")]
    public GameObject loadingWord;
    [Tooltip("'clear' 글자 이미지")]
    public GameObject clearWord;
    [Tooltip("'L' 글자 이미지 (clear + L 선택일 때)")]
    public GameObject letterL;
    [Tooltip("'A' 글자 이미지 (clear + A 선택일 때)")]
    public GameObject letterA;

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
        ApplyLevelLabels(sceneName);             // 로딩할 레벨 + 방금 클리어한 레벨(L/A) 토글
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
        HideAllLabels();
        if (loadingPanel != null) loadingPanel.SetActive(false);
        loading = false;
    }

    private void SetBar(float t)
    {
        if (fillBar != null) fillBar.fillAmount = t;
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(t * 100)}%";
    }

    private bool _pendingClear;
    private CreatureID _pendingChoice;

    // LevelPortal이 통과 직전 호출 → 다음 로딩 화면을 "level N clear L/A"로 표시.
    public void RequestClear(CreatureID choice) { _pendingClear = true; _pendingChoice = choice; }

    // LevelPortal이 clear를 요청했으면 "level N clear L/A", 아니면 "level N loading".
    // GetActiveScene()은 LoadSceneAsync 활성화 전이라 아직 떠나는 씬 = 클리어한 레벨.
    private void ApplyLevelLabels(string toScene)
    {
        if (_pendingClear)
        {
            _pendingClear = false;
            int fromLv = ParseLevel(SceneManager.GetActiveScene().name);
            if (fromLv >= 1) { ShowClear(fromLv, _pendingChoice); return; }
        }
        ShowLoading(ParseLevel(toScene));
    }

    // "level N loading" 조립 (level<1이면 전부 숨김)
    public void ShowLoading(int level)
    {
        HideAllLabels();
        if (level < 1) return;
        SetActiveSafe(levelWord, true);
        SetDigit(level);
        SetActiveSafe(loadingWord, true);
    }

    // "level N clear L/A" 조립
    public void ShowClear(int level, CreatureID choice)
    {
        HideAllLabels();
        if (level < 1) return;
        SetActiveSafe(levelWord, true);
        SetDigit(level);
        SetActiveSafe(clearWord, true);
        SetActiveSafe(letterL, choice == CreatureID.L);
        SetActiveSafe(letterA, choice == CreatureID.A);
    }

    public void HideAllLabels()
    {
        SetActiveSafe(levelWord, false);
        SetActiveSafe(loadingWord, false);
        SetActiveSafe(clearWord, false);
        SetActiveSafe(letterL, false);
        SetActiveSafe(letterA, false);
        if (levelDigits != null) foreach (var g in levelDigits) SetActiveSafe(g, false);
    }

    // 숫자 조각 중 해당 레벨 것만 켜기 ([0]=1, [1]=2 ...)
    private void SetDigit(int level)
    {
        if (levelDigits == null) return;
        for (int i = 0; i < levelDigits.Length; i++)
            SetActiveSafe(levelDigits[i], i == level - 1);
    }

    // "level3", "level1_after" 등에서 레벨 번호 추출. 레벨 씬 아니면 -1.
    private static int ParseLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;
        var m = System.Text.RegularExpressions.Regex.Match(
            sceneName, @"level\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }

    private static void SetActiveSafe(GameObject g, bool on) { if (g != null) g.SetActive(on); }
}
