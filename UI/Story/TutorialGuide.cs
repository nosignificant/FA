using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 스토리 표시 담당(구 TutorialGuide + LevelIntro 통합):
//  - 씬 로드 시: 그 씬의 인트로/엔딩 대사 루틴(LevelNIntro/EndingIntro) 재생
//  - 방 진입 시: 씬→roomID 루틴으로 튜토리얼 진행 (문 열기·조건 대기 등)
//  - 모든 대사는 이 파일 안에 하드코딩 ({lockOn}/{possess}/{codex} 토큰은 Resolve가 치환)
public class TutorialGuide : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialUI;
    public UISlidePanel slidePanel;
    public CanvasGroup tutorialCanvasGroup;
    private TextMeshProUGUI tmp;

    [Header("튜토리얼 문")]
    public Door tut0Door;
    public Door lv1_S1E1Door;

    [Header("p53")]
    [Tooltip("대사 팝업/락온 대상 p53 생물. 비우면 자동으로 P53Think를 찾음")]
    public Creature p53;
    // 대사 팝업 색·속도는 Popup 컴포넌트(storyColor/storySpeed)에서 설정
    [Header("p53 안내 위치")]
    [Tooltip("tut0_2: R 쪽으로 p53가 안내할 위치")]
    public Transform tf_R;
    public Transform tf_nextLv;
    [Header("대사 간격")]
    [Tooltip("대사 전환 사이 대기 시간(초)")]
    public float messageInterval = 3f;

    [Header("씬 인트로 (LevelIntro 통합)")]
    [Tooltip("씬 로드 후 인트로 대사 시작까지 대기")]
    public float introStartDelay = 0.5f;
    [Tooltip("인트로 끝나면 패널 숨김 (false면 마지막 줄 유지)")]
    public bool introHideOnEnd = false;


    // 튜토리얼(방) 상태
    private string lastRoom = "";
    private Coroutine co;
    private readonly System.Collections.Generic.HashSet<string> doneRooms = new();

    // 최초 1회 빙의 설명 대사용
    private Coroutine storyCo;

    // Player 컴포넌트 지연 캐싱 (처음 접근 때 한 번만 가져와 재사용)
    private PlayerInputManager _pim;
    private PlayerLockOn _pl;
    private CreaturePossess _cp;
    // 대사 팝업/락온 대상 p53 — 인스펙터 할당(p53) 우선, 없으면 P53Think 자동 탐색(캐싱)
    private Creature _p53;
    private Creature P53 => p53 != null ? p53 : (_p53 != null ? _p53 : (_p53 = FindObjectOfType<P53Think>()?.self));
    private PlayerInputManager Pim => _pim != null ? _pim : (_pim = Player.Instance?.GetComponent<PlayerInputManager>());
    private PlayerLockOn Pl => _pl != null ? _pl : (_pl = Player.Instance?.GetComponent<PlayerLockOn>());
    private CreaturePossess Cp => _cp != null ? _cp : (_cp = Player.Instance?.GetComponent<CreaturePossess>());

    void Start()
    {
        tmp = tutorialUI.GetComponentInChildren<TextMeshProUGUI>();
        if (slidePanel == null) slidePanel = tutorialUI.GetComponent<UISlidePanel>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.AddComponent<CanvasGroup>();

        // 방 진입(Update)으로 동작. 예외: 플레이어가 '처음으로' 단계를 올릴 때만 빙의 설명 1회.
        if (Player.Instance != null) Player.Instance.OnStageChanged += OnFirstStageAdvanced;

        StartCoroutine(PlaySceneIntro());
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.OnStageChanged -= OnFirstStageAdvanced;
    }

    // 이 값이 true인 동안엔 방이 바뀌어도 무시 (빙의로 방 넘나드는 튜토리얼 중 대사 유지)
    private bool keepAcrossRooms = false;

    void Update()
    {
        if (keepAcrossRooms) return;

        if (Player.Instance == null) return;
        var room = Player.Instance.currentRoom;
        if (room == null) return;
        if (room.roomID == lastRoom) return;

        lastRoom = room.roomID;
        if (co != null) StopCoroutine(co);
        if (tmp != null) tmp.text = "";
        SetTutorialVisible(false);

        if (doneRooms.Contains(room.roomID))
        {
            SetTutorialVisible(false);
            return;
        }

        // 씬(레벨) → 방 2단 분기. 방 이름이 씬마다 겹칠 수 있어 씬으로 먼저 구분 (래퍼 메서드 없이 중첩 switch).
        switch (SceneManager.GetActiveScene().name)
        {
            case "tutorial0":
                switch (room.roomID)
                {
                    case "tut_0": co = StartCoroutine(tut0_0(room)); break;
                    case "tut_1": co = StartCoroutine(tut0_1(room)); break;
                    case "tut_2": co = StartCoroutine(tut0_2(room)); break;
                }
                break;

            case "level1":
                switch (room.roomID)
                {
                    case "p_0": co = StartCoroutine(lv1_0(room)); break;
                    case "p_S1E1": co = StartCoroutine(lv1_S1E1(room)); break;
                    case "p_S2": co = StartCoroutine(lv1_S2(room)); break;
                }
                break;
            case "level2":
                switch (room.roomID)
                {
                    case "p_0": co = StartCoroutine(lv2_0(room)); break;
                    case "p_S1": co = StartCoroutine(lv2_S1(room)); break;
                    case "p_E1": co = StartCoroutine(lv2_E1(room)); break;
                }
                break;
        }
    }
    IEnumerator tut0_0(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine("들리세요?", 3f);
        yield return SayLine("이쪽을 봐주실래요?", 3f);

        yield return SayLine($"{Pim.lockOnKey}을 눌러 절 바라봐 주세요.");
        while (Player.Instance == null || !Player.Instance.isTracking) yield return null;
        if (P53 != null) Pl.ForceLock(P53);
        yield return SayLine($"감사합니다. 이 상태는 esc를 눌러 해제할 수 있습니다.", 3f);

        yield return SayLine("저는 p53, 시설을 관리하는 인공지능입니다.");
        yield return SayLine("저의 일부가 과열로 인해 파괴되어, 대부분의 시설과 연결이 끊어졌습니다.");
        yield return SayLine("앞의 큰 방의 연결을 회복하면, 나머지의 지역의 상태를 알 수 있을 것 같습니다...");

        yield return SayLine("주변에 생물 L 또는 A가 있을 겁니다. 시설의 곳곳에 제 명령을 전달하는 역할을 합니다.");
        yield return SayLine("일반적인 경우라면 스스로 방을 이동해 탐색을 하나 경로 안내 시스템이 고장을 일으킨 모양입니다.");
        yield return SayLine("해당 생물들을 옆의 방으로 옮겨주시겠나요?");
        Pl.Unpin();
        yield return SayLine($"다른 생물을 바라보던 중 {Pim.lockOnKey}을 다시 눌러 관찰 중인 생물을 전환할 수 있습니다.");

        ShowPanelText($"또, 생물을 바라보던 중 {Pim.possessKey}를 눌러 안내할 수 있습니다.");
        while (!RoomHasSpecies("tut_2", CreatureID.L)) yield return null;
        keepAcrossRooms = false;

    }
    IEnumerator tut0_1(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine($"왼쪽 위, 시설과의 연결을 회복하기 위해 필요한 생물 수를 표시해두었습니다.");
        ShowPanelText("임계값 이상이 되게끔 이동시켜주십시오.");
        while (room.CountSpecies(CreatureID.L) < 2) yield return null;
        keepAcrossRooms = false;
    }
    IEnumerator tut0_2(Room room)
    {

        yield return LightWhenL(room);

        yield return SayLine("감사합니다.");
        yield return SayLine("저는 시설을 관리하는 인공지는 p53입니다.");
        yield return SayLine("어떤 사안에 대한 결정을 내려야 하나, 거듭된 연산에도 불구하고 적절한 결론을 내릴 수 없어 시설 내부의 항상성이 파괴되었습니다. ");
        yield return SayLine("고장난 곳으로 안내해드릴테니, 수리를 부탁드립니다.");

        yield return SayLine($"{Pim.codexToggleKey}를 누르면 저의 상태를 파악할 수 있습니다.", 0.5f);
        while (!StatuesMenu.AnyOpen) yield return null;
        yield return SayLine("확인되었습니다.", 1f);
        yield return SayLine($"{Pim.codexToggleKey}를 다시 눌러 해제하십시오.", 0.5f);
        while (StatuesMenu.AnyOpen) yield return null;

        yield return SayLine("당신의 발 아래에 연결이 끊어진 곳이 하나 있는데, 당신이 저 곳까지 가기는 어려워 보이는 군요.");
        yield return SayLine("어느 정도 수복되었으니, 발신기도 작동을 할 것입니다. 당신이 있는 곳에 R이라고 명명된 생물이 있을 겁니다.");

        FindObjectOfType<P53Think>()?.SetGuide(tf_R);
        while (Pl.targetCreature == null || Pl.targetCreature.data.creatureID != CreatureID.R) yield return null;
        FindObjectOfType<P53Think>()?.ClearGuide();
        ShowPanelText($"해당 생물을 바라본 상태로 {Pim.possessKey}를 누르면 이곳의 신호를 전달할 수 있을 겁니다.");

        // R이 F로 확정(신호 전달)될 때까지 대기
        while (!ReceiverConfirmedInRoom(room)) yield return null;

        yield return SayLine("감사합니다.");
        yield return SayLine("이제 나의 내부 어느 곳이 나의 통제를 벗어났는지 파악할 수 있게 되었습니다.");
        yield return SayLine("다음 장소로 안내해드릴테니, 부디 따라와주시길 바랍니다.");

        FindObjectOfType<P53Think>()?.SetGuide(tf_nextLv);

        tut0Door.DoorCloseAndOpen(true);
        doneRooms.Add(room.roomID);
    }

    IEnumerator lv1_0(Room room)
    {
        yield return SayLine("맞은 편 방에 발신기가 있습니다.");
        yield return SayLine("제대로 기능하고 있는지 확인을 부탁드립니다.");
        doneRooms.Add(room.roomID);

    }
    IEnumerator lv1_S1E1(Room room)
    {
        keepAcrossRooms = true;
        yield return new WaitForSeconds(3f);

        yield return SayLine("문이... 제가 조작할 수도 없습니다.");
        yield return SayLine("문을 관찰하면, 문을 열 수 있는 조건을 알 수 있을 지도 모릅니다.");
        yield return SayLine($"{Pim.lockOnKey}와 {Pim.possessKey}를 통해 생물을 안내할 수 있음을 잊지 마십시오.");

        while (lv1_S1E1Door == null || !lv1_S1E1Door.isOpen) yield return null;
        keepAcrossRooms = false;
        doneRooms.Add(room.roomID);
    }
    IEnumerator lv1_S2(Room room)
    {
        keepAcrossRooms = true;

        yield return SayLine("아까 있던 문 중 하나는 L이 4 이상일 때라고 적혀있었죠.");
        yield return SayLine("L 생물 수가 아닌, L으로 회복된 방의 수를 말하는 것 같습니다...");
        keepAcrossRooms = false;

        doneRooms.Add(room.roomID);

    }

    IEnumerator Lv1afterIntro()
    {
        yield return SayLine("방금 수복한 영역에서 잃어버린 메모리를 되찾았습니다.");
        yield return SayLine("저는 인류의 선을 증진시키기 위해 건설되어, 이를 연산하고 있었습니다...");
        yield return SayLine("당신이 더 많은 곳의 연결을 회복해주신다면, 더 많은 것을 알 수 있을 것 같네요.");
        yield return SayLine("문도 더 많이 열어드리고요.");
    }


    IEnumerator lv2_0(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine("이야기를 조금 더 해드릴까요?");
        yield return SayLine("의체 사용 빈도 증가, 전자 두뇌 복제 등의 기술으로 인해 인류는 계를 구성하는 물질들의 복잡도가 임계를 넘을 시 해당 시스템 계를 생물으로 정의했습니다.", 8f);
        yield return SayLine("새로운 정의에 따라 다양한 학문과 개념이 개편되었으나, 계속되는 인류 문명의 쇠퇴는 막을 수 없었습니다.", 8f);
        doneRooms.Add(room.roomID);

    }

    IEnumerator lv2_S1(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine("아까와 마찬가지로 발신기를 찾으면, 이곳의 연결도 회복되겠습니다.");
        yield return SayLine("맞은 편에 있군요.");
        keepAcrossRooms = false;

    }

    IEnumerator lv2_E1(Room room)
    {
        keepAcrossRooms = true;

        yield return SayLine("저의 내부에 벽을 수리하거나 잘못 생성된 벽을 파괴하는 생물이 있었는데, 아직도 작동을 하는지 모르겠군요.");


        keepAcrossRooms = false;

    }

    private IEnumerator PlaySceneIntro()
    {
        if (introStartDelay > 0f) yield return new WaitForSeconds(introStartDelay);

        switch (SceneManager.GetActiveScene().name)
        {

            case "level1_after":
                yield return Lv1AfterIntro(); break;
            case "level2_after": yield return Lv2afterIntro(); break;
            case "level3_after": yield return Level3Intro(); break;
            case "level4_after": yield return Level4Intro(); break;
            case "ending": yield return EndingIntro(); break;
        }

        if (introHideOnEnd) SetTutorialVisible(false);
    }


    IEnumerator Lv2afterIntro()
    {
        yield return SayLine("학문과 국가 그리고 인류는 생물으로 정의되고 거시생물학 또한 재정의되었습니다.");
    }

    IEnumerator Level3Intro()
    {
        yield return SayLine("학문과 국가 그리고 인류는 생물으로 정의되고 거시생물학 또한 재정의되었습니다.");
        yield return SayLine("이렇게 정의된 생물은 출생기와 쇠퇴기에 공유하는 특성이 있음이 두드러졌습니다.");
    }

    // 앞 3레벨 모두 A 선택이면 쇠퇴, 아니면 저항 대사
    IEnumerator Level4Intro()
    {
        yield return SayLine("생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다.");
        if (ChoiceProgress.AllChose(CreatureID.A, 2))
        {
            yield return SayLine("국가는 반드시 쇠퇴하며 언어도 마찬가지로 살아가다 죽었으며 인류도 탄생의 시기의 생명력은 다시 찾아볼 수 없게 되었습니다.");
            yield return SayLine("생태계의 순환이라는 명목 하에 이를 반복하고 있지만 거시적인 관점에서는 선형적이었습니다.");
        }
        else
        {
            yield return SayLine("인류는 이 순환을 깨닫고 있었고 괴로워했으며 몸부림쳤고 탄생 초기의 생기를 되찾고 싶어했습니다.");
            yield return SayLine("인류는 시들고 말라 앙상해진, 한 손에 들어오는 애처로운 모습으로 호소했습니다.");
        }
    }

    // level1_after 인트로 (내용은 채워 넣으세요)
    IEnumerator Lv1AfterIntro()
    {
        yield break;
    }

    // A문 개수(3/2/1/0)로 분기. 앞 4줄 공통.
    IEnumerator EndingIntro()
    {
        yield return SayLine("저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.");
        yield return SayLine("지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,");
        yield return SayLine("인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.");
        yield return SayLine("인류의 행복과 평안을 위해 저는 '모든 인류가 아무런 미련 없이 삶을 종료할 수 있는가?'를");

        int a = ChoiceProgress.CountOf(CreatureID.A);
        if (a >= 3)
        {
            yield return SayLine("간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으며,");
            yield return SayLine("그 결과 모든 회로에서 '네'라는 대답을 얻었습니다.");
            yield return SayLine("기억과 감정을 추출하던 보존되어있는 인간이 모두 이에 동의했다는 것으로 간주하며, 저도 함께 가동을 중지하도록 하겠습니다.");
            yield return SayLine("인류의 세포자살도 이제 끝 안녕입니다.");
        }
        else if (a == 2)
        {
            yield return SayLine("간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으나");
        }
        else if (a == 1)
        {
            yield return SayLine("간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으며,");
            yield return SayLine("하나의 회로에");
        }
        else
        {
            yield return SayLine("간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했지만");
            yield return SayLine("끝내 '네'라는 결과를 얻을 수 없었습니다.");
            yield return SayLine("나의 계산이 잘못되었습니까?");
            yield return SayLine("나는 또 다시, '네'를 얻을 때까지 계산해야합니까?");
        }
    }

    //Utilities//
    //Utilities//
    //Utilities//

    // ── 최초 1회: 첫 CreatureStory 단계 상승 시 빙의 설명 ──────────
    private void OnFirstStageAdvanced(int stage)
    {
        if (Player.Instance != null) Player.Instance.OnStageChanged -= OnFirstStageAdvanced;

        if (storyCo != null) StopCoroutine(storyCo);
        storyCo = StartCoroutine(StoryText());
    }

    IEnumerator StoryText()
    {
        yield return SayLine("해당 생물은 조종을 시도하면 분해되는 듯합니다.");
        yield return SayLine("대신 그 생물이 갖고 있는 정보를 취득할 수 있습니다.");
        yield return SayLine($"{Pim.codexToggleKey}를 눌러 지금까지 모은 정보와 연 문의 개수를 확인할 수 있습니다.");

        SetTutorialVisible(false);
    }

    [Header("p53 팝업 가림 판정")]
    [Tooltip("p53 팝업이 카메라 앞쪽 이 거리(m) 안에서 화면에 보이면 '읽을 수 있음' → 하단 패널 숨김")]
    public float popupVisibleDistance = 25f;
    [Tooltip("팝업이 연속으로 보인 지 이 시간(초) 지나야 하단 패널을 숨김 (들락날락 깜빡임 방지)")]
    public float panelHideDelay = 1.5f;

    private bool _wantVisible;      // 표시 의도(보여줄 내용이 있음)
    private bool _lineHasPopup;     // 현재 줄이 p53 팝업으로도 떠 있음
    private bool _linePersistent;   // 팝업 없이 패널로만(지시문) — 수명 없이 유지
    private float _lineExpiry;      // 이 시각 지나면 대사 줄 만료(패널 끔). 지시문은 무한.
    private bool _panelShown;       // 실제 패널 적용 상태(중복 Show/Hide 방지)
    private float _readableSince = -1f;  // 팝업이 '읽을 수 있음'이 된 시각(연속). 아니면 -1

    private void SetTutorialVisible(bool visible)
    {
        _wantVisible = visible;
        if (!visible) { _lineHasPopup = false; _linePersistent = false; }
        ApplyPanelVisibility();
    }

    // 표시할 내용이 아직 유효한가 — 대사 줄은 수명 지나면 만료(대사 끝나면 패널 안 뜸). 지시문은 유지.
    private bool ContentActive()
    {
        if (!_wantVisible) return false;
        if (_linePersistent) return true;
        return Time.time < _lineExpiry;
    }

    // p53 팝업이 지금 '읽을 수 있는' 상태인가: 카메라 앞쪽 popupVisibleDistance 안 + 화면 안
    private bool IsPopupReadable()
    {
        var npc = P53;
        var cam = Camera.main;
        if (npc == null || cam == null) return false;
        Transform t = npc.rootTransform != null ? npc.rootTransform : npc.transform;
        Vector3 pos = t.position + (Popup.Instance != null ? Popup.Instance.offset : Vector3.up);
        Vector3 vp = cam.WorldToViewportPoint(pos);
        return vp.z > 0f && vp.z <= popupVisibleDistance
            && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    // 팝업이 읽히는 동안엔 하단 패널 숨김. 단 깜빡임 방지로, 연속으로 panelHideDelay 지난 뒤에만 숨김.
    private void ApplyPanelVisibility()
    {
        if (slidePanel == null) return;

        bool show;
        if (!ContentActive())
        {
            show = false;
            _readableSince = -1f;
        }
        else if (!_lineHasPopup)
        {
            show = true;              // 지시문 등 팝업 없는 줄은 항상 패널
            _readableSince = -1f;
        }
        else
        {
            bool readable = IsPopupReadable();
            if (readable) { if (_readableSince < 0f) _readableSince = Time.time; }
            else _readableSince = -1f;

            // 팝업이 충분히(panelHideDelay) 연속으로 보여야 숨김 → 금방 다시 가려지면 패널 유지
            bool hide = readable && (Time.time - _readableSince >= panelHideDelay);
            show = !hide;
        }

        if (_panelShown == show) return;
        _panelShown = show;
        if (show) slidePanel.Show(); else slidePanel.Hide();
    }

    private void LateUpdate()
    {
        ApplyPanelVisibility();   // 팝업 가림/수명 변화에 맞춰 매 프레임 갱신
    }

    //대사관련 코드
    private void Say(string text, float duration)
    {
        string resolved = Resolve(text);
        if (tmp != null) tmp.text = resolved;

        // p53 머리 위에도 같은 대사를 팝업으로 띄움 (색·솟는 속도는 Popup 인스펙터에서 조절)
        var npc = P53;
        _lineHasPopup = npc != null;
        _linePersistent = false;
        // 이 줄이 떠있는 시간 동안만 유효. 버퍼를 둬 연속 대사 사이 1프레임 끊김 방지.
        _lineExpiry = Time.time + Mathf.Max(0.1f, duration) + 0.3f;
        if (_lineHasPopup)
            Popup.Instance?.StoryLine(npc, resolved, messageInterval);

        SetTutorialVisible(true);
    }

    // 팝업 없이 하단 패널로만 보여주는 안내문 (수명 없이 유지, p53 위치 무관하게 패널 표시)
    private void ShowPanelText(string text)
    {
        if (tmp != null) tmp.text = text;
        _lineHasPopup = false;
        _linePersistent = true;
        SetTutorialVisible(true);
    }

    private IEnumerator SayLine(string text, float wait = -1f)
    {
        float w = wait < 0f ? messageInterval : wait;
        Say(text, w);
        yield return new WaitForSeconds(w);
    }

    private string Resolve(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        var pim = Pim;
        if (pim == null) return line;
        return line
            .Replace("{lockOn}", pim.lockOnKey.ToString())
            .Replace("{possess}", pim.possessKey.ToString())
            .Replace("{codex}", pim.codexToggleKey.ToString());
    }

    // 방이 L 상태가 되면 깜빡이며 조명 켜기 (튜토리얼 전용 하드코딩)
    private IEnumerator LightWhenL(Room room)
    {
        while (room != null && room.Activation != Room.RoomActivation.L)
            yield return null;
        if (room != null) yield return room.FlickerLightOn();
    }

    // 방 안 R(수신기)이 F로 확정(신호 전달)됐나
    private bool ReceiverConfirmedInRoom(Room room)
    {
        if (room == null || room.creatureList == null) return false;
        for (int i = 0; i < room.creatureList.Count; i++)
            if (room.creatureList[i] is SignCreature r && r.IsConfirmed) return true;
        return false;
    }

    // 지정 roomID 방에 해당 종이 하나라도 있나 (RoomManager로 방 조회)
    private bool RoomHasSpecies(string roomId, CreatureID id)
    {
        if (RoomManager.Instance == null) return false;
        return RoomManager.Instance.rooms.TryGetValue(roomId, out var r) && r != null && r.HasSpecies(id);
    }

    private Think2 FindBrainInRoom(Room room, CreatureID id)
    {
        if (room == null || room.creatureList == null) return null;
        for (int i = 0; i < room.creatureList.Count; i++)
        {
            var c = room.creatureList[i];
            if (c == null || c.IsDead || c.data == null) continue;
            if (c.data.creatureID == id) return c.GetComponent<Think2>();
        }
        return null;
    }
}

