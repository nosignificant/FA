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

    [Header("좌우 페이지 전환 (←/→)")]
    [Tooltip("status/memory·dialogue를 모두 담은 '슬라이드되는 부모'. anchoredPosition.x를 0 ↔ -pageWidth로 옮겨 전환")]
    public RectTransform slideParent;
    [Tooltip("dialogue 패널(선택) — 열 때 같이 켜둠. 세부 내용은 나중에 제작")]
    public GameObject dialoguePanel;
    [Tooltip("페이지 간 X 거리(px). dialogue가 오른쪽으로 이만큼 떨어져 있음")]
    public float pageWidth = 1920f;
    [Tooltip("슬라이드 전환 시간(초). 0이면 즉시 이동. ease in-out(SmoothStep)으로 가속→감속")]
    public float slideDuration = 0.35f;

    [Header("이동 가능 방향 표시 (선택)")]
    [Tooltip("오른쪽(→)으로 더 이동 가능할 때 켜질 오브젝트. 0페이지(status/memory)일 때 켜짐")]
    public GameObject rightArrow;
    [Tooltip("왼쪽(←)으로 더 이동 가능할 때 켜질 오브젝트. 1페이지(dialogue)일 때 켜짐")]
    public GameObject leftArrow;

    [Header("dialogue 세부 메뉴 (선택)")]
    [Tooltip("dialogue 페이지의 tutorial/level 리스트. 이 페이지에선 W/S로 선택 이동")]
    public DialogueMenuUI dialogueMenu;

    private int _page = 0;       // 0 = status/memory, 1 = dialogue
    private float _baseX = 0f;        // 프리팹에 설정된 slideParent의 원래 x(0페이지 기준점)
    private bool _baseCaptured = false;
    private float _targetX = 0f;
    private float _slideFromX = 0f;
    private float _slideStart = -1f;   // 슬라이드 시작 시각(unscaled). <0이면 멈춤 상태

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
        SetActiveSafe(rightArrow, false);   // C 누르기 전엔 화살표도 꺼둠
        SetActiveSafe(leftArrow, false);
        isOpen = false;

        // 프리팹에 세팅된 slideParent의 현재 x를 '0페이지 기준점'으로 기억.
        //  (이걸 안 하면 GoToPage가 x=0으로 밀어버려 가운데 정렬이 왼쪽으로 튐)
        if (slideParent != null) { _baseX = slideParent.anchoredPosition.x; _baseCaptured = true; }
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

    // →: dialogue 페이지로,  ←: status/memory 페이지로 (PlayerInputManager가 방향키로 호출)
    public void PageRight() => GoToPage(1, false);
    public void PageLeft()  => GoToPage(0, false);

    // dialogue 페이지에서만 W/S 선택 이동 전달 (다른 페이지에선 무시)
    public void DialogueMove(int dir)
    {
        if (_page == 1 && dialogueMenu != null) dialogueMenu.Move(dir);
    }

    private void GoToPage(int p, bool instant)
    {
        _page = Mathf.Clamp(p, 0, 1);

        UpdateArrows();   // 방향 표시는 slideParent 유무와 무관하게 먼저 갱신

        // dialogue 페이지 진입/이탈에 맞춰 세부 메뉴 선택 초기화/하이라이트 정리
        if (dialogueMenu != null) { if (_page == 1) dialogueMenu.Show(); else dialogueMenu.Hide(); }

        if (slideParent == null) return;

        // 기준점 미확보면 지금 위치를 기준으로 삼음(Start 못 탄 경우 대비)
        if (!_baseCaptured) { _baseX = slideParent.anchoredPosition.x; _baseCaptured = true; }

        // 0페이지 = 프리팹 원래 위치(_baseX), 1페이지 = 거기서 왼쪽으로 pageWidth만큼
        _targetX = _baseX - _page * pageWidth;

        if (instant || slideDuration <= 0f)
        {
            var ap = slideParent.anchoredPosition; ap.x = _targetX; slideParent.anchoredPosition = ap;
            _slideStart = -1f;                       // 즉시 → 애니메이션 없음
        }
        else
        {
            _slideFromX = slideParent.anchoredPosition.x;   // 현재 위치에서 시작 (전환 중 재입력도 자연스럽게)
            _slideStart = Time.unscaledTime;
        }
    }

    private void Update()
    {
        if (slideParent == null || _slideStart < 0f) return;   // 멈춤 상태면 아무것도 안 함

        float t = (Time.unscaledTime - _slideStart) / slideDuration;
        if (t >= 1f) { t = 1f; _slideStart = -1f; }            // 끝 → 정지
        float eased = Mathf.SmoothStep(0f, 1f, t);             // ease in-out (가속→감속)

        Vector2 ap = slideParent.anchoredPosition;
        ap.x = Mathf.Lerp(_slideFromX, _targetX, eased);
        slideParent.anchoredPosition = ap;
    }

    private void Open()
    {
        isOpen = true;
        // 상태창은 이동 가능한 오버레이 — 플레이어 이동/시점을 막지 않음

        SetActiveSafe(storyPanel, true);
        SetActiveSafe(statuesPanel, true);

        story?.Show();
        statues?.Show();

        SetActiveSafe(dialoguePanel, true);   // 오른쪽에 대기(슬라이드로 진입). 세부 내용은 추후
        GoToPage(0, true);                    // 열 땐 항상 status/memory 페이지부터 (즉시)
    }

    private void Close()
    {
        SetActiveSafe(storyPanel, false);
        SetActiveSafe(statuesPanel, false);
        SetActiveSafe(dialoguePanel, false);
        SetActiveSafe(rightArrow, false);   // 닫을 땐 방향 표시도 둘 다 끔
        SetActiveSafe(leftArrow, false);
        if (dialogueMenu != null) dialogueMenu.Hide();   // dialogue live 갱신 중단
        isOpen = false;
    }

    // 현재 페이지에서 갈 수 있는 방향만 켬. 0페이지→오른쪽(→) 가능, 1페이지→왼쪽(←) 가능.
    private void UpdateArrows()
    {
        SetActiveSafe(rightArrow, _page == 0);
        SetActiveSafe(leftArrow,  _page == 1);
    }

    private static void SetActiveSafe(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}
