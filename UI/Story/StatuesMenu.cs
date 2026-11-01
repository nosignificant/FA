using UnityEngine;

// 코덱스 오버레이 코디네이터.
// 키 입력은 PlayerInputManager가 읽어 public 메서드(Toggle/Scroll/Back)를 호출한다.
// (모든 플레이어 입력은 PlayerInputManager에서 처리 원칙)
// C로 열면 story 목록 + statues(레벨별 클리어 선택) 목록을 함께 띄운다.
//  Toggle : 열기/닫기
//  Scroll : 마우스 휠로 story 목록 스크롤
//  Back   : 닫기
public class StatuesMenu : MonoBehaviour
{
    public static StatuesMenu Instance;
    public static bool AnyOpen => Instance != null && Instance.isOpen;

    private bool isOpen = false;

    [Header("Story 목록")]
    public GameObject storyPanel;
    public StoryCodexUI story;

    [Header("Status 패널 (레벨별 클리어 선택) — 코드명은 Statues")]
    public GameObject statuesPanel;
    public StatuesUI statues;

    // DoF 초점 설정은 Player/FocusController로 이동 (StatuesMenu.AnyOpen을 읽어 처리).

    // 홈(먼저 로드된 씬)의 상태창이 주인 — 중복(레벨 씬)은 양보해서 자기 패널만 닫아둔다.
    private bool _duplicate;

    // 도메인 리로드 끄는 fast play-mode 대비: 재생 시작 때 static 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        // 이미 다른 StatuesMenu가 주인이면(홈 것) Instance를 뺏지 않고 중복으로 표시만.
        if (Instance != null && Instance != this) { _duplicate = true; return; }
        Instance = this;
    }

    private void Start()
    {
        // 중복이든 주인이든 패널은 닫힌 채로 시작. 중복은 아무도 Open 안 하니 그대로 닫혀 있음.
        SetActiveSafe(storyPanel, false);
        SetActiveSafe(statuesPanel, false);
        isOpen = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;   // 주인이 사라지면 자리 비움 (다음 씬이 주인 될 수 있게)
    }

    // ── PlayerInputManager가 호출하는 public 입력 API ────────
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    // 마우스 휠: story 목록 스크롤 (PlayerInputManager가 휠 값 전달)
    public void Scroll(float wheel)
    {
        if (isOpen) story?.ScrollBy(wheel);
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
