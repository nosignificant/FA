using UnityEngine;

// 코덱스 오버레이 코디네이터.
// 키 입력은 PlayerInputManager가 읽어 public 메서드(Toggle/Navigate/Back)를 호출한다.
// (모든 플레이어 입력은 PlayerInputManager에서 처리 원칙)
// J로 열면 story 목록과 statues(열린 문) 목록을 함께 띄운다. 메뉴 단계 없음.
//  Toggle  : 열기/닫기
//  Navigate: story 항목 선택 이동 (statues는 짧아 그냥 표시)
//  Back    : 닫기
public class StatuesMenu : MonoBehaviour
{
    public static StatuesMenu Instance;
    public static bool AnyOpen => Instance != null && Instance.isOpen;

    private bool isOpen = false;

    [Header("Story 목록")]
    public GameObject storyPanel;
    public StoryCodexUI story;

    [Header("Statues (열린 문) 목록")]
    public GameObject statuesPanel;
    public StatuesUI statues;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetActiveSafe(storyPanel, false);
        SetActiveSafe(statuesPanel, false);
        isOpen = false;
    }

    // ── PlayerInputManager가 호출하는 public 입력 API ────────
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    // W/S(-1/+1): story 항목 선택 이동
    public void Navigate(int dir)
    {
        if (isOpen) story?.MoveSelection(dir);
    }

    // ESC: 닫기
    public void Back() => Close();

    private void Open()
    {
        isOpen = true;
        // 상태창은 이동 가능한 오버레이 — 플레이어 이동/시점을 막지 않음

        SetActiveSafe(storyPanel, true);
        SetActiveSafe(statuesPanel, true);

        story?.Show();
        statues?.Show();
    }

    private void Close()
    {
        SetActiveSafe(storyPanel, false);
        SetActiveSafe(statuesPanel, false);
        isOpen = false;
    }

    private static void SetActiveSafe(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}
