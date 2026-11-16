using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public static Pause Instance;
    public GameObject panelRoot;
    public Image bg;

    [Header("표시")]
    [Tooltip("배경+메뉴를 함께 감싸는 오브젝트의 CanvasGroup. 알파가 같이 연동됨")]
    public CanvasGroup canvasGroup;

    [System.Serializable]
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    public MenuItem[] items;

    private int selected = 0;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        // 패널 초기화는 중복이든 주인이든 해둠 (각 씬 패널 숨김). Start는 panelRoot 비활성 시 안 돌 수 있어 Awake에서.
        UpdateVisual();
        SetBgAlpha(0f);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panelRoot != null) panelRoot.SetActive(false);

        // Instance는 홈(먼저 로드) 것이 주인 — 중복(레벨)은 양보. ESC는 항상 홈 패널을 토글.
        if (Instance != null && Instance != this) return;
        Instance = this;

        WireMouse();
    }

    // 각 항목에 마우스 hover(선택 이동) / 클릭(실행)을 연결
    private void WireMouse()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            int idx = i;
            items[i].entry.BindMouse(
                () => { selected = idx; UpdateVisual(); },
                () => { selected = idx; Confirm(); });
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    public void Open()
    {
        Time.timeScale = 0f;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;   // 마우스 클릭/hover 받게
            canvasGroup.interactable = true;
        }
        SetBgAlpha(0.7f);

        // 첫 오픈 시 하위 항목 정렬이 틀어지는 문제 방지:
        //  Awake에서 패널이 비활성인 채 텍스트를 넣어 레이아웃(ContentSizeFitter/LayoutGroup)이
        //  리빌드되지 않은 상태로 열림 → 활성화 직후 라벨 갱신 + 강제 리빌드로 즉시 정렬.
        UpdateVisual();
        if (panelRoot != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panelRoot.transform);
        }

        // 메뉴 버튼을 마우스로 누를 수 있게 커서 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        SetBgAlpha(0f);
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;

        // 게임 복귀 → 커서 다시 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [Tooltip("배경 색 (알파는 코드가 페이드로 조절)")]
    public Color bgColor = Color.black;

    private void SetBgAlpha(float a)
    {
        if (bg == null) return;
        Color c = bgColor;   // RGB는 항상 bgColor(검정)로 강제
        c.a = a;
        bg.color = c;
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
            case "RESTART LEVEL": RestartLevel(); break;
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
