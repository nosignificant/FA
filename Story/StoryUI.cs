using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// C로 메뉴 진입, W/S로 이야기 선택, E로 읽기, ESC로 뒤로/닫기.
/// Closed → Menu → Reading 상태머신.
/// </summary>
public class StoryUI : MonoBehaviour
{
    public static StoryUI Instance;

    [Header("References")]
    public StoryUnlockManager manager;
    public GameObject panel;
    public GameObject menuView;
    public GameObject readingView;

    [Header("Menu Texts")]
    public TMP_Text countsText;     // 상단: H × 3, L × 2 ...
    public TMP_Text storyListText;  // 하단: 이야기 목록

    [Header("Reading Texts")]
    public TMP_Text storyContentText;

    public enum State { Closed, Menu, Reading }
    public State state { get; private set; } = State.Closed;
    public bool IsOpen => state != State.Closed;

    private int selectedIndex = 0;
    private List<StoryData> currentStories = new List<StoryData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (manager == null) manager = StoryUnlockManager.Instance;
        if (panel != null) panel.SetActive(false);
        if (manager != null) manager.OnFulfillmentChanged += RefreshIfMenu;
    }

    void OnDestroy()
    {
        if (manager != null) manager.OnFulfillmentChanged -= RefreshIfMenu;
    }

    void Update()
    {
        switch (state)
        {
            case State.Closed:
                break;
            case State.Menu:
                HandleMenuInput();
                break;
            case State.Reading:
                HandleReadingInput();
                break;
        }
    }

    void Open()
    {
        state = State.Menu;
        if (panel != null) panel.SetActive(true);
        if (menuView != null) menuView.SetActive(true);
        if (readingView != null) readingView.SetActive(false);
        PlayerControl.SetPlayerMove(false);
        Refresh();
    }

    void Close()
    {
        state = State.Closed;
        if (panel != null) panel.SetActive(false);
        PlayerControl.SetPlayerMove(true);
    }

    void Refresh()
    {
        if (manager == null) return;

        // 상단: 분해 누적 현황
        var counts = manager.GetAllCounts();
        var sb = new StringBuilder();
        foreach (var kv in counts)
        {
            if (kv.Key == null) continue;
            sb.AppendLine($"{kv.Key.creatureName} × {kv.Value}");
        }
        if (countsText != null) countsText.text = sb.ToString();

        // 하단: 해금된 이야기 목록
        currentStories = manager.GetAllUnlockedStories();
        if (selectedIndex >= currentStories.Count) selectedIndex = Mathf.Max(0, currentStories.Count - 1);
        UpdateStoryList();
    }

    void UpdateStoryList()
    {
        var sb = new StringBuilder();
        if (currentStories.Count == 0)
        {
            sb.AppendLine("(no stories unlocked yet)");
        }
        else
        {
            for (int i = 0; i < currentStories.Count; i++)
            {
                string prefix = (i == selectedIndex) ? "▶ " : "  ";
                sb.AppendLine($"{prefix}- {currentStories[i].title}");
            }
        }
        if (storyListText != null) storyListText.text = sb.ToString();
    }

    void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (currentStories.Count > 0)
            {
                selectedIndex = (selectedIndex - 1 + currentStories.Count) % currentStories.Count;
                UpdateStoryList();
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (currentStories.Count > 0)
            {
                selectedIndex = (selectedIndex + 1) % currentStories.Count;
                UpdateStoryList();
            }
        }
        if (Input.GetKeyDown(KeyCode.E) && currentStories.Count > 0)
        {
            EnterReading(currentStories[selectedIndex]);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void HandleReadingInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMenu();
        }
    }

    void EnterReading(StoryData story)
    {
        state = State.Reading;
        if (menuView != null) menuView.SetActive(false);
        if (readingView != null) readingView.SetActive(true);
        if (storyContentText != null)
            storyContentText.text = $"<b>{story.title}</b>\n\n{story.content}";
    }

    void BackToMenu()
    {
        state = State.Menu;
        if (readingView != null) readingView.SetActive(false);
        if (menuView != null) menuView.SetActive(true);
        Refresh();
    }

    void RefreshIfMenu()
    {
        if (state == State.Menu) Refresh();
    }
}
