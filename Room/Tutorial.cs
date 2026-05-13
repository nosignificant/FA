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

    public Image ProductionImage;
    public CanvasGroup tutorialCanvasGroup;

    [Header("ProductionImage 페이드 설정")]
    public float fadeInTime = 1.5f;
    public float holdTime = 2.0f;
    public float fadeOutTime = 1.5f;
    private bool productionPlayed = false;

    void Start()
    {
        tmp = tutorialUI.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = "Press TAB to observe creatures";

        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.AddComponent<CanvasGroup>();
    }

    private void SetTutorialVisible(bool visible)
    {
        if (tutorialCanvasGroup != null) tutorialCanvasGroup.alpha = visible ? 1f : 0f;
    }

    void Update()
    {
        Room room = Player.Instance.currentRoom;
        if (room == null) return;

        string nowRoom = room.roomID;

        if (nowRoom == "tut_0")
        {
            SetTutorialVisible(true);
            if (Player.Instance.isTracking)
                doors[0].DoorCloseAndOpen(true);
            if (doors[0].isOpen == true) tmp.text = "Press ESC to escape";
        }
        else if (nowRoom == "tut_1")
        {
            SetTutorialVisible(true);
            tmp.text = "Press TAB again to cycle between creatures";
            if (Player.Instance.isTracking)
                tmp.text = "some creatures spawn others";
            if (room.creatureList.Exists(c => c != null && c.data.creatureID == CreatureID.HH) ||
            (room.decomposedCounts.Values.Sum() > 1))
            {
                tmp.text = "some creatures can synthesize into new ones";
                doors[1].DoorCloseAndOpen(true);
            }
        }
        else if (nowRoom == "tut_2")
        {
            SetTutorialVisible(true);
            tmp.text = "decompositions are the key to opening the door";
            if (Player.Instance.isTracking)
                tmp.text = "check the decomposition count";
            if (room.decomposedCounts.Values.Sum() > 0) doors[2].DoorCloseAndOpen(true);
        }
        else if (nowRoom == "pro_main")
        {
            SetTutorialVisible(false);
            if (!productionPlayed && ProductionImage != null)
            {
                productionPlayed = true;
                StartCoroutine(PlayProductionImage());
            }
        }
        else { SetTutorialVisible(false); }
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