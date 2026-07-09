using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenu : MonoBehaviour
{
    public static EscMenu Instance;
    public GameObject panelRoot;

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
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                Move(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                Move(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Confirm();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && CanOpen())
            Open();
    }

    private bool CanOpen()
    {
        bool observing = ObservationUI.Instance.IsOpen;
        bool tracking = Player.Instance.isTracking;
        // 이번 프레임에 ESC로 락온을 막 해제했다면 같은 ESC로 메뉴 열지 않음
        bool justUnlocked = Player.Instance.lastUnlockFrame == Time.frameCount;
        return !observing && !tracking && !justUnlocked;
    }

    private void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;

    }

    private void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }
    private void Move(int dir)
    {
        selected = (selected + dir + items.Length) % items.Length;
        UpdateVisual();
    }


    private void Confirm()
    {
        if (items.Length == 0) return;
        switch (items[selected].label)
        {
            case "RETURN TO MAIN": LoadScene("title"); break;
            case "ENCYCLOPEDIA": LoadScene("encyclopedia"); break;
            case "Quit": Application.Quit(); break;
        }
    }

    // 씬 전환 전 timeScale 복구 (Open에서 0으로 멈춘 게 다음 씬까지 남는 것 방지)
    private static void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
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
    public void OnEncyclopediaButton() => LoadScene("encyclopedia");
    public void OnQuitButton() => Application.Quit();
}
