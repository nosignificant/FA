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

    private void Awake()
    {
        Instance = this;
        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        // 초기화는 Awake에서 (Start는 panelRoot 비활성 시 안 돌 수 있음)
        UpdateVisual();
        SetBgAlpha(0f);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (panelRoot != null) panelRoot.SetActive(false);
    }


    public void Open()
    {
        Time.timeScale = 0f;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        SetBgAlpha(0.7f);
    }

    public void Close()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        SetBgAlpha(0f);
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
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
