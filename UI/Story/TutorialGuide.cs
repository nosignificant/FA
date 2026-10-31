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
//  - 대사는 하드코딩 ({lockOn}/{possess}/{codex} 토큰은 Resolve가 치환)
//  - 레벨별 대사 루틴은 partial로 분리 가능 (예: level1 → Level1Text.cs)
public partial class TutorialGuide : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialUI;
    public UISlidePanel slidePanel;
    public CanvasGroup tutorialCanvasGroup;
    private TextMeshProUGUI tmp;

    [Header("튜토리얼 문")]
    public Door tut0Door;
    public Door lv1_S1E1Door;
    public Door lv1_Door;
    public Door lv2_Door;
    public Door lv3_Door;
    public Door lv4_Door;
    public Door lv2_p0Door;

    public Door lv1ClearDoor;

    [Header("p53")]
    [Tooltip("대사 팝업/락온 대상 p53 생물. 비우면 자동으로 P53Think를 찾음")]
    public Creature p53;

    [Header("p53 안내 위치")]
    [Tooltip("tut0_2: R 쪽으로 p53가 안내할 위치")]
    public Transform tf_tutL;
    public Transform tf_tut;
    public Transform tf_R;
    public Transform tf_nextLv;
    public Transform tf_ending1;
    public Transform tf_ending2;

    [Tooltip("Ending tut_5에서 넘어갈 'thank you for playing' 씬 이름")]
    public string thankYouScene = "ThankYouForPlaying";

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

        // Level 2 p_0 통로는 한 번 열린 뒤 방 상태가 바뀌어도 계속 열린다.
        if (SceneManager.GetActiveScene().name == "level2" && lv2_p0Door != null)
        {
            lv2_p0Door.stayOpenAfterFirstOpen = true;
            if (lv2_p0Door.isOpen)
                lv2_p0Door.SetAlwaysOpen();
        }

        StartCoroutine(PlaySceneIntro());
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
            case "tutorial":
                switch (room.roomID)
                {
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
                    case "p_S1": co = StartCoroutine(lv1_S1(room)); break;

                }
                break;
            case "level1_after":
                switch (room.roomID)
                {
                    case "tut_2": co = StartCoroutine(Lv1After_tut0()); break;
                }
                break;

            case "level2":
                switch (room.roomID)
                {
                    case "p_0": co = StartCoroutine(lv2_0(room)); break;
                }
                break;

            case "level2_after":
                switch (room.roomID)
                {
                    case "tut_2": co = StartCoroutine(Lv2After_tut2()); break;
                }
                break;
            case "level3_after":
                switch (room.roomID)
                {
                    case "tut_2": co = StartCoroutine(Lv3After_tut2()); break;
                }
                break;
            case "Ending":
                switch (room.roomID)
                {
                    case "tut_2": co = StartCoroutine(Ending_tut2()); break;
                    case "tut_3": co = StartCoroutine(Ending_tut3()); break;
                    case "tut_5": co = StartCoroutine(Ending_tut5()); break;
                }
                break;
        }
    }


    private IEnumerator PlaySceneIntro()
    {
        if (introStartDelay > 0f) yield return new WaitForSeconds(introStartDelay);

        switch (SceneManager.GetActiveScene().name)
        {
            case "level3": yield return Level3Intro(); break;
            case "level1_after": yield return Lv1AfterIntro(); break;
            case "level2_after": yield return Lv2AfterIntro(); break;
            case "level3_after": yield return Lv3AfterIntro(); break;
            case "ending": yield return EndingIntro(); break;
        }

        if (introHideOnEnd) SetTutorialVisible(false);
    }



    // A문 개수(3/2/1/0)로 분기. 앞 4줄 공통.
    [Header("엔딩 연출 — 특정 대사 후 오브젝트 낙하")]
    [Tooltip("엔딩 특정 대사 후 아래로 내릴 오브젝트")]
    public Transform endingDropTarget;
    [Tooltip("내릴 Y 거리(음수=아래). 기본 -60")]
    public float endingDropY = -60f;
    [Tooltip("내리는 데 걸리는 시간(0이면 즉시)")]
    public float endingDropDuration = 1.5f;

    [Tooltip("엔딩에서 WallActivationTint를 모두 A색으로 바꿀 오브젝트들(하위 전부 적용)")]
    public GameObject[] endingAColorObjects;

    // 등록한 오브젝트들의 하위 WallActivationTint를 전부 A색으로.
    private void ApplyEndingAColor()
    {
        if (endingAColorObjects == null) return;
        foreach (var go in endingAColorObjects)
        {
            if (go == null) continue;
            foreach (var tint in go.GetComponentsInChildren<WallActivationTint>(true))
                if (tint != null) tint.Apply(Room.RoomActivation.A);
        }
    }

    // 지금까지 더 많이 선택된 쪽(A/L)으로 이 레벨의 '모든 방'을 방송처럼 변환 — 생물(입자) A↔L 실제 전환. (동점이면 A)
    private void ApplyEndingChoiceRooms()
    {
        if (RoomManager.Instance == null) return;
        int a = ChoiceProgress.CountOf(CreatureID.A);
        int l = ChoiceProgress.CountOf(CreatureID.L);
        Room.RoomActivation state = a >= l ? Room.RoomActivation.A : Room.RoomActivation.L;
        foreach (Room room in RoomManager.Instance.rooms.Values)
            if (room != null) room.ApplyBroadcast(state);
    }

    // 지정 오브젝트를 월드 Y로 dy만큼 (dur초에 걸쳐, 0이면 즉시) 이동
    private IEnumerator DropObject(Transform t, float dy, float dur)
    {
        if (t == null) yield break;
        Vector3 from = t.position;
        Vector3 to = from + new Vector3(0f, dy, 0f);
        if (dur <= 0f) { t.position = to; yield break; }
        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            t.position = Vector3.Lerp(from, to, e / dur);
            yield return null;
        }
        t.position = to;
    }





    //Utilities//
    //Utilities//
    //Utilities//


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

        // {stat:L일때문구|A일때문구} → 직전(가장 최근) 레벨을 무엇으로 클리어했는지(L/A)에 따라 치환.
        // 기록이 없으면 L 쪽 문구 사용. (레벨 클리어는 R 있는 레벨에서만 기록되므로 levelN_after에선 그 레벨의 선택 = 마지막 기록)
        line = System.Text.RegularExpressions.Regex.Replace(line, @"\{stat:([^|}]*)\|([^}]*)\}", m =>
        {
            var all = ChoiceProgress.All();
            bool isA = all.Count > 0 && all[all.Count - 1] == CreatureID.A;
            return isA ? m.Groups[2].Value : m.Groups[1].Value;
        });

        var pim = Pim;
        if (pim != null)
            line = line
                .Replace("{lockOn}", pim.lockOnKey.ToString())
                .Replace("{possess}", pim.possessKey.ToString())
                .Replace("{codex}", pim.codexToggleKey.ToString());

        return line;
    }

    // 방이 L 상태가 되면 깜빡이며 조명 켜기 (튜토리얼 전용 하드코딩)
    private IEnumerator LightWhenL(Room room)
    {
        while (room != null && room.Activation != Room.RoomActivation.L)
            yield return null;
        if (room != null) yield return room.FlickerLightOn();
    }

    // 방 안 R이 자기 방의 L/A 상태를 감지 중인가 (방이 L/A면 R이 연결된 문을 연다)
    private bool ReceiverConfirmedInRoom(Room room)
    {
        if (room == null || room.creatureList == null) return false;
        for (int i = 0; i < room.creatureList.Count; i++)
            if (room.creatureList[i] is SignCreature r
                && (r.CurrentSignalState == Room.RoomActivation.L || r.CurrentSignalState == Room.RoomActivation.A))
                return true;
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

