using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    [Header("DoF 블러 (focusDistance 조절)")]
    [Tooltip("DoF(Bokeh) 오버라이드가 있는 Volume. 비우면 PlayerLockOn 카메라에서 자동 탐색")]
    public Volume dofVolume;
    [Tooltip("상태창 열렸을 때 focusDistance (0=카메라 바로 앞, 전체 흐림)")]
    public float statusFocus = 0f;
    [Tooltip("생물 조종 중 focusDistance (약한 흐림)")]
    public float possessFocus = 15f;
    [Tooltip("평소(상태창·조종 아닐 때) focusDistance 기본값")]
    public float defaultFocus = 25f;
    [Tooltip("focusDistance 전환 속도(단위/초). 0 이하면 즉시")]
    public float blurFadeSpeed = 60f;
    private DepthOfField _dof;
    private bool _dofReady;
    private CreaturePossess _possess;

    private bool _hasBaseOverride; // 외부(튜토리얼 등)가 평소 focusDistance를 덮어씀
    private float _baseOverride;
    // 평소(상태창/조종 아닐 때) focusDistance 기준값을 외부에서 설정/해제. 상태창·조종 블러보다 우선순위 낮음.
    public void SetBaseFocus(float value) { _baseOverride = value; _hasBaseOverride = true; }
    public void ClearBaseFocus() { _hasBaseOverride = false; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetActiveSafe(storyPanel, false);
        SetActiveSafe(statuesPanel, false);
        isOpen = false;
        ResolveDoF();
    }

    // DoF Volume을 찾아 DepthOfField 오버라이드 + 원래 focusDistance 저장.
    // dofVolume이 비어있으면 PlayerLockOn의 카메라(그 오브젝트의 Volume)에서 자동 탐색.
    private void ResolveDoF()
    {
        if (_dofReady) return;

        Volume vol = dofVolume;
        if (vol == null)
        {
            Camera cam = (Player.Instance != null && Player.Instance.pl != null) ? Player.Instance.pl.cam : null;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                vol = cam.GetComponent<Volume>();
                if (vol == null) vol = cam.GetComponentInChildren<Volume>();
            }
        }

        if (vol != null && vol.profile != null && vol.profile.TryGet(out _dof))
        {
            _dofReady = true;
        }
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

    // 상태에 따라 DoF focusDistance를 매 프레임 목표값으로 수렴:
    //  상태창 열림 → statusFocus(우선) / 생물 조종 중 → possessFocus / 그 외 → 원래값
    private void Update()
    {
        if (!_dofReady) { ResolveDoF(); if (!_dofReady) return; }

        bool paused = Pause.Instance != null && Pause.Instance.IsOpen;
        float target = _hasBaseOverride ? _baseOverride : defaultFocus;   // 평소값(인스펙터 기본, 외부 override 가능)
        if (isOpen || paused) target = statusFocus;       // 상태창·일시정지 우선(가장 강한 흐림)
        else if (IsPossessing()) target = possessFocus;

        float cur = _dof.focusDistance.value;
        _dof.focusDistance.value = (blurFadeSpeed <= 0f)
            ? target
            : Mathf.MoveTowards(cur, target, blurFadeSpeed * Time.unscaledDeltaTime);
    }

    private bool IsPossessing()
    {
        if (_possess == null) _possess = FindObjectOfType<CreaturePossess>();
        return _possess != null && _possess.IsPossessing;
    }

    private static void SetActiveSafe(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}
