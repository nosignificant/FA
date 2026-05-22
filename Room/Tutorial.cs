using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public GameObject tutorialUI;
    public Door[] doors = new Door[0];

    private TextMeshProUGUI tmp;
    public UISlidePanel slidePanel;

    public Image ProductionImage;
    public CanvasGroup tutorialCanvasGroup;

    [Header("ProductionImage 페이드 설정")]
    public float fadeInTime = 1.5f;
    public float holdTime = 2.0f;
    public float fadeOutTime = 1.5f;
    private bool productionPlayed = false;

    private string lastRoom = "";
    Coroutine co;


    void Start()
    {
        tmp = tutorialUI.GetComponentInChildren<TextMeshProUGUI>();
        if (slidePanel == null) slidePanel = tutorialUI.GetComponent<UISlidePanel>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.AddComponent<CanvasGroup>();
    }

    private void SetTutorialVisible(bool visible)
    {
        if (slidePanel == null) return;
        if (visible) slidePanel.Show();
        else slidePanel.Hide();
    }

    void Update()
    {
        if (Player.Instance == null) return;
        var room = Player.Instance.currentRoom;
        if (room == null) return;
        if (room.roomID == lastRoom) return;

        lastRoom = room.roomID;
        if (co != null) StopCoroutine(co);

        Debug.Log($"[Tutorial] 방 진입 roomID='{room.roomID}' / tmp={(tmp==null?"NULL":"ok")} / slidePanel={(slidePanel==null?"NULL":"ok")}");

        switch (room.roomID)
        {
            case "tut_0":
                OpenDoor(0);
                break;
            case "tut_1":
                co = StartCoroutine(Tut1Routine(room));
                break;
            case "tut_2":
                co = StartCoroutine(Tut2Routine(room));
                break;
            case "tut_3":
                co = StartCoroutine(Tut3Routine(room));
                break;
            case "tut_4":
                co = StartCoroutine(Tut4Routine(room));
                break;
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

    // 이미 열린 문은 다시 호출 안 함 (매번 호출하면 애니메이션 재시작됨)
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

        tmp.text = "탭 키를 눌러 생물을 관찰하세요.";
        while (!Player.Instance.isTracking) yield return null;

        tmp.text = "이 생물은 주변을 돌아다니고 있습니다.";
        OpenDoor(1);
        yield return new WaitForSeconds(3f);
        tmp.text = "생물은 주변에 어떤 생물이 있느냐에 따라 다양한 행동을 합니다.";
        yield return new WaitForSeconds(3f);
        tmp.text = "ESC로 관찰을 해제할 수 있습니다.";
        yield return new WaitForSeconds(3f);

        SetTutorialVisible(false);

    }

    IEnumerator Tut2Routine(Room room)
    {
        SetTutorialVisible(true);

        tmp.text = "탭을 한 번 더 눌러 생물 관찰을 전환하세요.";
        while (!LockedOn(CreatureID.L)) yield return null;

        tmp.text = "어떤 생물은 다른 생물을 생산하고 합성할 수 있습니다.";
        yield return new WaitForSeconds(3f);
        tmp.text = "L은 H를 2마리 합쳐 HH를 만들 수 있습니다.";
        yield return new WaitForSeconds(3f);

        // HH 생기거나 분해 진행되면 문 열기
        while (!room.creatureList.Exists(c => c != null && c.data != null && c.data.creatureID == CreatureID.HH)
               && room.decomposedCounts.Values.Sum() <= 1)
            yield return null;
        OpenDoor(2);

        tmp.text = "생물마다 각자의 기능이 있고, 그를 통해 다양한 생물을 생산할 수 있습니다.";
        yield return new WaitForSeconds(3f);
        SetTutorialVisible(false);
    }
    IEnumerator Tut3Routine(Room room)
    {
        SetTutorialVisible(true);

        tmp.text = "L은 같은 방에 AA가 있는 것을 싫어합니다.";
        yield return new WaitForSeconds(3f);
        tmp.text = "AA가 같은 방에 있으면, L은 다른 방으로 가려고 합니다.";
        yield return new WaitForSeconds(3f);

        OpenDoor(3);

        tmp.text = "어떤 생물이 어떤 생물을 좋아하고 싫어하는지 알아내십시오.";
        yield return new WaitForSeconds(3f);
        SetTutorialVisible(false);
    }
    IEnumerator Tut4Routine(Room room)
    {
        SetTutorialVisible(true);

        tmp.text = "D는 방 안의 생물 수가 과도해지면 생물을 분해합니다.";
        yield return new WaitForSeconds(3f);
        tmp.text = "D를 관찰하십시오.";

        while (!doors[4].isOpen) yield return null;

        tmp.text = "생물이 다른 생물과 상호작용하는 방식을 관찰하십시오.";
        yield return new WaitForSeconds(3f);

        tmp.text = "그리고 다양한 생물을 생산하고 분해해 문의 잠금을 해제하고 새로운 곳으로 나아가십시오.";

        yield return new WaitForSeconds(3f);
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

        // 유지
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
}