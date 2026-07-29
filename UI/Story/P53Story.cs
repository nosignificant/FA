using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using UnityEngine.UI;


public class P53 : MonoBehaviour
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

        // P53은 방 진입(Update의 room 트리거)으로 동작.
        // 예외: 플레이어가 '처음으로' CreatureStory 단계를 올릴 때만 빙의 설명 대사 1회.
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

    // ── 튜토리얼: 방 진입 트리거 ────────────────────────────────
    void Update()
    {
        if (Player.Instance == null) return;
        var room = Player.Instance.currentRoom;
        if (room == null) return;
        if (room.roomID == lastRoom) return;

        lastRoom = room.roomID;
        if (co != null) StopCoroutine(co);

        // 이미 대사 끝낸 방이면 다시 재생 안 함
        if (doneRooms.Contains(room.roomID))
        {
            SetTutorialVisible(false);
            return;
        }

        switch (room.roomID)
        {
            case "tut_0": OpenDoor(0); break;
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
                SetTutorialVisible(false);
                break;
        }
    }

    private void OpenDoor(int idx)
    {
        if (doors == null || idx < 0 || idx >= doors.Length) return;
        if (doors[idx] == null || doors[idx].isOpen) return;
        doors[idx].DoorCloseAndOpen(true);
    }

    // 플레이어가 특정 종을 락온 중인지
    private bool LockedOn(CreatureID id)
    {
        var pl = Player.Instance != null ? Player.Instance.pl : null;
        var t = pl != null ? pl.targetCreature : null;
        return t != null && t.data != null && t.data.creatureID == id;
    }

    IEnumerator Tut1Routine(Room room)
    {
        SetTutorialVisible(true);

        tmp.text = "탭 키를 눌러 생물을 관찰하십시오.";
        while (!Player.Instance.isTracking) yield return null;
        if (LockedOn(CreatureID.D))
        {
            tmp.text = "이 생물은 주변을 돌아다니고 있습니다.";
            yield return new WaitForSeconds(messageInterval);
            tmp.text = "생물은 주변에 어떤 생물이 있느냐에 따라 다양한 행동을 합니다.";
        }

        tmp.text = "관찰 중 탭을 눌러 관찰 중인 생물을 전환할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "문을 관찰하면, 문을 열 수 있는 조건을 알 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "ESC로 관찰을 해제할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);
        tmp.text = "다음 방으로 이동하십시오.";

        OpenDoor(1);

        doneRooms.Add(room.roomID);
        SetTutorialVisible(false);
    }

    IEnumerator Tut2Routine(Room room)
    {
        SetTutorialVisible(true);
        yield return new WaitForSeconds(messageInterval / 2);

        doors[1].DoorCloseAndOpen(false);
        tmp.text = "어떤 생물은 다른 생물을 생산하고 합성할 수 있습니다.";
        while (!LockedOn(CreatureID.L)) yield return null;

        tmp.text = "생물 L은 S를 2마리 합쳐 SS를 만들 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "다음 방으로 이동하십시오.";
        yield return new WaitForSeconds(messageInterval);

        OpenDoor(2);
        doneRooms.Add(room.roomID);
        SetTutorialVisible(false);
    }

    IEnumerator Tut3Routine(Room room)
    {
        SetTutorialVisible(true);
        yield return new WaitForSeconds(messageInterval / 2);

        doors[2].DoorCloseAndOpen(false);

        tmp.text = "L은 같은 방에 AA가 있는 것을 싫어합니다.";
        yield return new WaitForSeconds(messageInterval);
        tmp.text = "AA가 같은 방에 있으면, L은 다른 방으로 가려고 합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "하지만 AA를 조종해 L에게서 멀리 떨어트려둘 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "E, Q로 고도를 조절하십시오.";
        yield return new WaitForSeconds(10f);

        tmp.text = "조종 중 F를 다시 눌러 조종을 해제하십시오.";

        tmp.text = "다음 방으로 이동하십시오.";
        OpenDoor(3);

        yield return new WaitForSeconds(messageInterval);

        doneRooms.Add(room.roomID);
        SetTutorialVisible(false);
    }

    IEnumerator Tut4Routine(Room room)
    {
        SetTutorialVisible(true);
        yield return new WaitForSeconds(messageInterval / 2);

        doors[3].DoorCloseAndOpen(false);

        tmp.text = "D는 방 안의 생물 수가 과도하게 많아지면 생물을 분해합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "D가 분해한 생물 수는 오른쪽 위에 표시됩니다.";
        int decomposedBefore = room.decomposedCounts.Values.Sum();
        while (room.decomposedCounts.Values.Sum() <= decomposedBefore) yield return null;

        tmp.text = "방은 내부에서 분해된 생물 수를 확인하고 가장 많이 분해된 생물을 확인하는 문을 개방합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "저는 생각합니다. 당신이 생물을 조작해 연 길에 따라.";
        yield return new WaitForSeconds(messageInterval);

        OpenDoor(4);

        doneRooms.Add(room.roomID);
        SetTutorialVisible(false);
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
        // 한 번 뜬 뒤엔 다시 안 뜨게 바로 구독 해제
        if (Player.Instance != null) Player.Instance.OnStageChanged -= OnFirstStageAdvanced;

        if (storyCo != null) StopCoroutine(storyCo);
        storyCo = StartCoroutine(StoryText());
    }

    IEnumerator StoryText()
    {
        SetTutorialVisible(true);

        tmp.text = "해당 생물은 연약하여 조종을 시도하면 죽게 되는 듯합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "대신 그 생물이 갖고 있는 정보를 취득할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        SetTutorialVisible(false);
    }
}
