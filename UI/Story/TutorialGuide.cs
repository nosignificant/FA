using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

// 튜토리얼 흐름 담당: 방 진입 트리거로 안내 대사를 재생하고 문을 연다.
// 전용 패널(tutorialUI)을 쓰며, 방 이동 때 이 패널만 껐다 켠다.
// (LevelIntro/엔딩은 별도 패널을 써야 겹치지 않음. 대사 데이터는 P53에.)
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

    [Header("ProductionImage 페이드 설정")]
    public Image ProductionImage;
    public float fadeInTime = 1.5f;
    public float holdTime = 2.0f;
    public float fadeOutTime = 1.5f;
    private bool productionPlayed = false;

    // 튜토리얼(방) 상태
    private string lastRoom = "";
    private Coroutine co;
    private readonly System.Collections.Generic.HashSet<string> doneRooms = new();

    // 최초 1회 빙의 설명 대사용
    private Coroutine storyCo;

    void Start()
    {
        tmp = tutorialUI.GetComponentInChildren<TextMeshProUGUI>();
        if (slidePanel == null) slidePanel = tutorialUI.GetComponent<UISlidePanel>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.AddComponent<CanvasGroup>();

        // 방 진입(Update)으로 동작. 예외: 플레이어가 '처음으로' 단계를 올릴 때만 빙의 설명 1회.
        if (Player.Instance != null) Player.Instance.OnStageChanged += OnFirstStageAdvanced;
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

    // 대사를 세팅하면서 패널을 표시 (빈 패널이 뜨는 구간을 없애기 위해 텍스트+표시를 묶음)
    private void Say(string text)
    {
        if (tmp != null) tmp.text = text;
        SetTutorialVisible(true);
    }

    // ── 방 진입 트리거 ──────────────────────────────────────────
    void Update()
    {
        if (Player.Instance == null) return;
        var room = Player.Instance.currentRoom;
        if (room == null) return;
        if (room.roomID == lastRoom) return;

        lastRoom = room.roomID;
        if (co != null) StopCoroutine(co);

        // 이전 방 코루틴이 남긴 대사가 다음 방 시작 딜레이 때 튀어나오지 않게 정리
        if (tmp != null) tmp.text = "";
        SetTutorialVisible(false);

        if (doneRooms.Contains(room.roomID))
        {
            SetTutorialVisible(false);
            return;
        }

        switch (room.roomID)
        {
            case "tut_0": StartCoroutine(Tut0Routine(room)); break;
            case "tut_1": co = StartCoroutine(Tut1Routine(room)); break;
            case "tut_2": co = StartCoroutine(Tut2Routine(room)); break;
            case "tut_3": co = StartCoroutine(Tut3Routine(room)); break;
            case "tut_4": co = StartCoroutine(Tut4Routine(room)); break;
            case "pro_main":
                SetTutorialVisible(false);
                if (!productionPlayed && ProductionImage != null)
                {
                    productionPlayed = true;
                    StartCoroutine(PlayProductionImage());
                }
                break;
            default:
                break;
        }
    }

    private void OpenDoor(int idx)
    {
        if (doors == null || idx < 0 || idx >= doors.Length) return;
        if (doors[idx] == null || doors[idx].isOpen) return;
        doors[idx].DoorCloseAndOpen(true);
    }
    IEnumerator Tut0Routine(Room room)
    {
        Say("farewell apoptosis 회로 연산을 시작합니다.");
        yield return new WaitForSeconds(messageInterval);
        doors[0].DoorCloseAndOpen(true);

        doneRooms.Add(room.roomID);
    }
    IEnumerator Tut1Routine(Room room)
    {
        var pim = Player.Instance.GetComponent<PlayerInputManager>();

        Say($"{pim.lockOnKey}를 눌러 생물을 관찰하십시오.");
        while (!Player.Instance.isTracking) yield return null;
        doors[0].DoorCloseAndOpen(false);

        tmp.text = "생물은 회로의 일부입니다. 생물을 관찰하면, 해당 생물을 락온합니다. 락온 중에는 생물이 관심 갖는 생물을 알 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = $"관찰 중 {pim.lockOnKey}을 한 번 더 눌러 관찰 중인 생물을 전환할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "문을 관찰하면, 문을 열 수 있는 조건을 알 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "문은 회로의 게이트입니다. 어느 문을 열었냐에 따라 다른 연산 결과가 도출됩니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "ESC로 관찰을 해제할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        doors[1].DoorCloseAndOpen(true);
        yield return new WaitForSeconds(messageInterval);

        doneRooms.Add(room.roomID);
        // 패널은 방을 떠날 때(Update의 방 전환 정리)에서 숨김 — 방에 있는 동안 유지
    }

    IEnumerator Tut2Routine(Room room)
    {
        yield return new WaitForSeconds(messageInterval / 2);

        doors[1].DoorCloseAndOpen(false);
        Say("어떤 생물은 다른 생물을 생산하고 합성하는 능력을 갖고 있습니다.");
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "생물 L은 S를 2마리 합쳐 SS를 만들 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "생물 L이 합성하는 모습을 관찰하십시오.";
        while (!room.HasSpecies(CreatureID.SS)) yield return null;

        tmp.text = "이렇게 생물 L이 S생물 두 마리를 포획하면, SS로 합성할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "생물은 각기 다른 특성을 갖고 있고, 종마다 그 특성을 공유하기도 합니다.";
        yield return new WaitForSeconds(messageInterval);

        OpenDoor(2);
        doneRooms.Add(room.roomID);
        // 패널은 방을 떠날 때(Update의 방 전환 정리)에서 숨김 — 방에 있는 동안 유지
    }

    IEnumerator Tut3Routine(Room room)
    {
        yield return new WaitForSeconds(messageInterval / 2);

        doors[2].DoorCloseAndOpen(false);

        Say("L은 같은 방에 AA가 있는 것을 싫어합니다.");
        yield return new WaitForSeconds(messageInterval);
        tmp.text = "AA가 같은 방에 있으면, L은 다른 방으로 가려고 합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "하지만 AA를 조종해 L에게서 멀리 떨어트려둘 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        var pim = Player.Instance.GetComponent<PlayerInputManager>();
        var cp = Player.Instance.GetComponent<CreaturePossess>();

        tmp.text = $"{pim.possessKey}를 눌러 생물을 조종하십시오.";
        while (!cp.IsPossessing) yield return null;

        tmp.text = "생물을 조종하면, 생물의 상태가 controlled가 됩니다.";
        yield return new WaitForSeconds(10f);

        tmp.text = "E, Q로 고도를 조절하십시오.";
        yield return new WaitForSeconds(10f);

        tmp.text = "조종 중 F를 다시 눌러 조종을 해제하십시오.";

        tmp.text = "다음 방으로 이동하십시오.";
        OpenDoor(3);

        yield return new WaitForSeconds(messageInterval);

        doneRooms.Add(room.roomID);
        // 패널은 방을 떠날 때(Update의 방 전환 정리)에서 숨김 — 방에 있는 동안 유지
    }

    IEnumerator Tut4Routine(Room room)
    {
        yield return new WaitForSeconds(messageInterval / 2);

        doors[3].DoorCloseAndOpen(false);

        Say("D는 방 안의 생물 수가 과도하게 많아지면 생물을 분해합니다.");
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "D가 분해한 생물 수는 오른쪽 위에 표시됩니다.";
        int decomposedBefore = room.decomposedCounts.Values.Sum();
        while (room.decomposedCounts.Values.Sum() <= decomposedBefore) yield return null;

        tmp.text = "방은 내부에서 분해된 생물 수를 확인하고 가장 많이 분해된 생물을 확인하는 문을 개방합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "문에 대한 정보는 하단의 방위 표시, 또는 관찰을 통해 알 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "생물은 회로의 일부이고 당신이 조작한 회로에 따라 저는 생각합니다.";
        yield return new WaitForSeconds(messageInterval);

        OpenDoor(4);

        doneRooms.Add(room.roomID);
        // 패널은 방을 떠날 때(Update의 방 전환 정리)에서 숨김 — 방에 있는 동안 유지
    }

    private IEnumerator PlayProductionImage()
    {
        ProductionImage.gameObject.SetActive(true);
        Color col = ProductionImage.color;
        col.a = 0f;
        ProductionImage.color = col;

        // 페이드 인
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            col.a = Mathf.Clamp01(t / fadeInTime);
            ProductionImage.color = col;
            yield return null;
        }
        col.a = 1f;
        ProductionImage.color = col;

        yield return new WaitForSeconds(holdTime);

        // 페이드 아웃
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            col.a = 1f - Mathf.Clamp01(t / fadeOutTime);
            ProductionImage.color = col;
            yield return null;
        }
        col.a = 0f;
        ProductionImage.color = col;

        ProductionImage.gameObject.SetActive(false);
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
        Say("해당 생물은 연약하여 조종을 시도하면 죽게 되는 듯합니다.");
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "대신 그 생물이 갖고 있는 정보를 취득할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        var pim = Player.Instance.GetComponent<PlayerInputManager>();
        tmp.text = $"{pim.codexToggleKey}를 눌러 지금까지 모은 정보와 연 문의 개수를 확인할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        SetTutorialVisible(false);
    }
}
