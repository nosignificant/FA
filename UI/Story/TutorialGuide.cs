using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 스토리 표시 담당(구 TutorialGuide + LevelIntro 통합):
//  - 씬 로드 시: 그 씬의 인트로 대사(P53.IntroLines[씬]) 재생
//  - 방 진입 시: 씬→roomID 루틴으로 튜토리얼 진행 (문 열기·조건 대기 등)
// 대사 데이터는 P53(순수 데이터), 표시·로직은 여기.
public class TutorialGuide : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialUI;
    public UISlidePanel slidePanel;
    public CanvasGroup tutorialCanvasGroup;
    private TextMeshProUGUI tmp;

    [Header("튜토리얼 문")]
    public Door[] doors = new Door[0];

    [Header("대사 간격")]
    [Tooltip("대사 전환 사이 대기 시간(초)")]
    public float messageInterval = 3f;

    [Header("씬 인트로 (LevelIntro 통합)")]
    [Tooltip("씬 로드 후 인트로 대사 시작까지 대기")]
    public float introStartDelay = 0.5f;
    [Tooltip("인트로 끝나면 패널 숨김 (false면 마지막 줄 유지)")]
    public bool introHideOnEnd = false;

    [Header("이미지 페이드 시간")]
    public float fadeInTime = 1.5f;
    public float fadeOutTime = 1.5f;

    [Header("tut_0 키보드 조작 이미지 (나가면/일시정지 시 페이드아웃)")]
    public Image keyboardImage;
    private Coroutine _kbFadeCo;
    private bool _kbWantShown;

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

        // 씬 인트로 대사 재생 (구 LevelIntro 역할) — 그 씬의 P53 인트로가 있으면
        StartCoroutine(PlaySceneIntro());
    }

    // 씬 로드 시 인트로 대사 (P53.IntroLines[씬 이름])를 순차 재생.
    // level4는 인트로 뒤에 앞 레벨 선택(ChoiceProgress)에 따른 엔딩 대사를 이어 재생.
    private IEnumerator PlaySceneIntro()
    {
        string scene = SceneManager.GetActiveScene().name;
        string[] lines = P53.GetIntroLines(scene);

        if (introStartDelay > 0f) yield return new WaitForSeconds(introStartDelay);

        if (lines != null)
            for (int i = 0; i < lines.Length; i++)
                yield return SayLine(lines[i]);

        if (scene == "level4")
        {
            string[] level4 = P53.GetLevel4();
            for (int i = 0; i < level4.Length; i++)
                yield return SayLine(level4[i]);
        }

        // ending 씬: level1,2,3 선택 순서 조합에 따라 다른 엔딩
        if (scene == "ending")
        {
            string[] ending = P53.GetEnding();   // 조합 없으면 null
            if (ending != null)
                for (int i = 0; i < ending.Length; i++)
                    yield return SayLine(ending[i]);
        }

        if (introHideOnEnd) SetTutorialVisible(false);
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.OnStageChanged -= OnFirstStageAdvanced;
    }

    private void SetTutorialVisible(bool visible)
    {
        if (slidePanel == null) return;
        if (visible) slidePanel.Show();
        else slidePanel.Hide();
    }

    // 대사를 세팅하면서 패널을 표시 (토큰 {possess} 등은 실제 키로 치환)
    private void Say(string text)
    {
        if (tmp != null) tmp.text = P53.Resolve(text);
        SetTutorialVisible(true);
    }

    // 대사 표시 + 기본 간격(messageInterval) 대기를 한 줄로. `yield return SayLine("...")`로 사용
    // wait을 넘기면 그 시간만큼 대기 (음수면 messageInterval)
    private IEnumerator SayLine(string text, float wait = -1f)
    {
        Say(text);
        yield return new WaitForSeconds(wait < 0f ? messageInterval : wait);
    }

    // 이 값이 true인 동안엔 방이 바뀌어도 무시 (빙의로 방 넘나드는 튜토리얼 중 대사 유지)
    private bool keepAcrossRooms = false;

    // ── 방 진입 트리거 ──────────────────────────────────────────
    void Update()
    {
        UpdateKeyboardImage();         // tut_0 + 일시정지 아님일 때만 표시 (매 프레임 상태 체크)

        if (keepAcrossRooms) return;   // 방 전환 무시 (진행 중인 대사 유지)

        if (Player.Instance == null) return;
        var room = Player.Instance.currentRoom;
        if (room == null) return;
        if (room.roomID == lastRoom) return;

        lastRoom = room.roomID;
        if (co != null) StopCoroutine(co);

        // 이전 방 코루틴이 남긴 대사가 다음 방 시작 딜레이 때 튀어나오지 않게 정리
        if (tmp != null) tmp.text = "";
        SetTutorialVisible(false);

        // 방별 DoF focus는 FocusController의 Room Focus Overrides 테이블에서 자동 처리.

        if (doneRooms.Contains(room.roomID))
        {
            SetTutorialVisible(false);
            return;
        }

        // 씬마다 같은 roomID를 쓰므로 씬으로 먼저 분기
        switch (SceneManager.GetActiveScene().name)
        {
            case "tutorial0": DispatchTutorial1(room); break;
            case "level1_after": Dispatch_afterLvl1(room); break;
        }
    }

    // ── tutorial1 씬: roomID별 라우팅 ──────────────────────────
    private void DispatchTutorial1(Room room)
    {
        switch (room.roomID)
        {
            case "tut_1": co = StartCoroutine(Tut1Routine(room)); break;
            case "tut_2": co = StartCoroutine(Tut2Routine(room)); break;

            case "pro_main": SetTutorialVisible(false); break;
        }
    }

    // ── tutorial2 씬: roomID별 라우팅 (여기에 방 루틴 추가) ─────
    private void Dispatch_afterLvl1(Room room)
    {

    }

    // 방이 L 상태가 되면 깜빡이며 조명 켜기 (튜토리얼 전용 하드코딩)
    private IEnumerator LightWhenL(Room room)
    {
        while (room != null && room.Activation != Room.RoomActivation.L)
            yield return null;
        if (room != null) yield return room.FlickerLightOn();
    }

    private void OpenDoor(int idx)
    {
        if (doors == null || idx < 0 || idx >= doors.Length) return;
        if (doors[idx] == null || doors[idx].isOpen) return;
        doors[idx].DoorCloseAndOpen(true);
    }
    IEnumerator Tut0Routine(Room room)
    {
        yield return null;

    }
    IEnumerator Tut1Routine(Room room)
    {
        yield return null;

    }
    IEnumerator Tut2Routine(Room room)
    {

        yield return LightWhenL(room);
        var pim = Pim;
        var pl = Pl;
        var cp = Cp;


        yield return SayLine("farewell apoptosis 연산을 시작합니다.", 5f);

        yield return SayLine($"저는 당신의 선택에 따라 생각하고 결론을 내립니다.");
        yield return SayLine($"{pim.codexToggleKey}를 눌러 파악하십시오.");

        while (!StatuesMenu.AnyOpen) yield return null;
        yield return SayLine("확인되었습니다.", 3f);
        yield return SayLine($"{pim.codexToggleKey}를 다시 눌러 해제하십시오.");
        while (StatuesMenu.AnyOpen) yield return null;

        yield return SayLine($"생물은 나의 회로의 일부입니다. {pim.lockOnKey}를 눌러 생물을 관찰하십시오.");
        while (pl.targetCreature == null) yield return null;

        yield return SayLine("생물은 주변과 상호작용합니다. 생물을 관찰하면, 생물의 현재 행동을 알 수 있습니다.");
        yield return SayLine($"관찰 중 {pim.lockOnKey}을 한 번 더 눌러 관찰 중인 생물을 전환할 수 있습니다.");

        tmp.text = $"어떤 생물은 당신이 조종할 수 있습니다. LL을 관찰한 상태에서 {pim.possessKey}를 눌러 조종하십시오.";
        while (!cp.IsPossessing) yield return null;
        yield return SayLine($"조종 중 {pim.possessKey}를 다시 눌러 조종을 해제하십시오.");
        while (cp.IsPossessing) yield return null;

        tmp.text = "다음으로 문을 관찰하십시오.";
        while (pl.targetCreature == null || pl.targetCreature.data.creatureID != CreatureID.Door)
            yield return null;
        yield return SayLine("방의 상태는 문을 여는 조건이 됩니다.");
        yield return SayLine("방의 상태는 L과 A 두 가지로, 색으로 구분됩니다.");

        yield return SayLine("A를 관찰하십시오. A는 방을 검은 색으로 만듭니다.");
        while (pl.targetCreature == null || pl.targetCreature.data.creatureID != CreatureID.A) yield return null;

        yield return SayLine("방에 L 또는 A 생물이 과반이 되면, 방은 해당 상태가 됩니다.");
        OpenDoor(1);
        yield return SayLine("다음 방으로 이동하십시오.");

        doneRooms.Add(room.roomID);
    }

    IEnumerator Tut3Routine(Room room)
    {
        // H 활성화는 L/A 생물을 데리러 옆 방을 왕복해야 함 → 그 동안 방 전환 트리거 무시(대사 중복 방지)
        keepAcrossRooms = true;

        var pim = Pim;
        var pl = Pl;
        var cp = Cp;
        Say("H를 관찰하십시오.");
        while (pl.targetCreature == null || pl.targetCreature.data.creatureID != CreatureID.H) yield return null;

        yield return SayLine("어떤 생물은 방의 상태에 따라 활성화되거나 비활성화됩니다.");

        tmp.text = "방의 상태를 바꿔 H를 활성화시키십시오.";

        while (true)
        {
            var hBrain = FindBrainInRoom(room, CreatureID.H);
            if (hBrain != null && !hBrain.dormant) break;
            yield return null;
        }

        yield return SayLine("확인되었습니다.", 3f);
        yield return SayLine("생물은 각기 다른 특성을 가지고 있습니다. 생물을 활성화시키고 관찰해 행동을 파악하십시오.");

        doneRooms.Add(room.roomID);

        // 대사 끝 → 방 전환 감시 재개. lastRoom을 현재 방으로 맞춰 다음 방부터 정상 트리거
        lastRoom = Player.Instance != null && Player.Instance.currentRoom != null
            ? Player.Instance.currentRoom.roomID : lastRoom;
        SetTutorialVisible(false);
        keepAcrossRooms = false;
    }

    IEnumerator Afterlvl1_Routine1(Room room)
    {
        yield return SayLine("의체 사용 빈도 증가, 전자 두뇌 복제 등의 기술의 등장으로 인류는 생물에 대한 새로운 정의가 필요했습니다.");

        yield return SayLine("인류는 계를 구성하는 물질들의 복잡도가 임계를 넘을 시 해당 시스템 계를 생물으로 정의했습니다.");

        yield return SayLine("저는 인류 전체의 선을 증대시키기 위해 건설되었고, 이와 관련된 연산은 필연적이었습니다.");

        doors[4].DoorCloseAndOpen(false);

    }

    // 방 안에서 특정 종 생물의 Think2를 찾음 (없으면 null)
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

    // tut_0이고 일시정지 아닐 때만 키보드 이미지 표시. 상태 바뀔 때만 토글(페이드 재시작 방지).
    private void UpdateKeyboardImage()
    {
        if (keyboardImage == null) return;
        bool paused = Pause.Instance != null && Pause.Instance.IsOpen;
        var room = Player.Instance != null ? Player.Instance.currentRoom : null;
        // 씬이 tutorial0 이고 방이 tut_0 일 때만 표시 (그 외엔 비활성)
        bool inTut0 = SceneManager.GetActiveScene().name == "tutorial0" && room != null && room.roomID == "tut_0";
        bool want = !paused && inTut0;
        if (want != _kbWantShown)
        {
            _kbWantShown = want;
            ShowKeyboardImage(want);
        }
    }

    // tut_0 키보드 이미지 표시/숨김 (알파 페이드). on=true면 페이드인, false면 페이드아웃 후 끔.
    private void ShowKeyboardImage(bool on)
    {
        if (keyboardImage == null) return;
        if (_kbFadeCo != null) StopCoroutine(_kbFadeCo);
        _kbFadeCo = StartCoroutine(FadeImageAlpha(keyboardImage, on ? 1f : 0f, on ? fadeInTime : fadeOutTime));
    }

    private IEnumerator FadeImageAlpha(Image img, float target, float dur)
    {
        if (img == null) yield break;
        if (target > 0f && !img.gameObject.activeSelf) img.gameObject.SetActive(true);

        Color col = img.color;
        float start = col.a;
        if (dur <= 0f) { col.a = target; img.color = col; }
        else
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;   // 일시정지(timeScale=0) 중에도 진행
                col.a = Mathf.Lerp(start, target, t / dur);
                img.color = col;
                yield return null;
            }
            col.a = target; img.color = col;
        }
        if (target <= 0f) img.gameObject.SetActive(false);   // 완전 투명이면 끔
        _kbFadeCo = null;
    }

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

        var pim = Pim;
        yield return SayLine($"{pim.codexToggleKey}를 눌러 지금까지 모은 정보와 연 문의 개수를 확인할 수 있습니다.");

        SetTutorialVisible(false);
    }
}
