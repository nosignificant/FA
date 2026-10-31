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
        ApplyLevelLabels(sceneName);             // 'loading' 글자만 표시
        HideHudWhileLoading();                   // 로딩 중 RoomStateBar·나침반 숨김
        // 로딩 중 상태 패널도 같이 켬 (C로 여는 것과 동일). DoF 흐림은 FocusController가 자동 적용.
        if (StatuesMenu.Instance != null && !StatuesMenu.AnyOpen) StatuesMenu.Instance.Toggle();
        SetBar(0f);
        yield return null;                       // 패널 한 프레임 렌더

        float shown = 0f;
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)   // Build Settings에 없거나 이름 오타 → LoadSceneAsync가 null 반환
        {
            Debug.LogWarning($"[SceneLoader] '{sceneName}' 로드 실패 — Build Settings에 없거나 이름 오타. 로딩 취소.");
            loading = false;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            HideAllLabels();
            yield break;
        }
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

    // 이제 'loading' 글자만 표시한다. level/숫자/clear/L/A 조각은 더 이상 쓰지 않음(인스펙터 참조는 유지).
    private void ApplyLevelLabels(string toScene)
    {
        _pendingClear = false;   // 소비만 하고 clear 표시는 안 함
        ShowLoading(ParseLevel(toScene));
    }

    // 'loading' 글자만 켬 (나머지 조각은 전부 끔)
    public void ShowLoading(int level)
    {
        HideAllLabels();
        SetActiveSafe(loadingWord, true);
    }

    // 더 이상 쓰지 않음 — 호출돼도 'loading'만 표시.
    public void ShowClear(int level, CreatureID choice)
    {
        HideAllLabels();
        SetActiveSafe(loadingWord, true);
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

    // 로딩 중에는 떠나는 씬의 RoomStateBar·나침반을 숨긴다(로딩 패널 뒤로 비치지 않게).
    // 새 씬이 로드되면 그쪽 HUD는 새로 활성 상태라 따로 복구할 필요 없음.
    private void HideHudWhileLoading()
    {
        foreach (var bar in FindObjectsOfType<RoomStateBar>()) if (bar != null) bar.gameObject.SetActive(false);
        foreach (var c in FindObjectsOfType<CompassStrip>()) if (c != null) c.gameObject.SetActive(false);
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
