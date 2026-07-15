using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class Story : MonoBehaviour
{
    public GameObject tutorialUI;
    public int lastStage = 0;

    private TextMeshProUGUI tmp;
    public UISlidePanel slidePanel;


    public CanvasGroup tutorialCanvasGroup;

    [Header("대사 간격")]
    [Tooltip("튜토리얼 대사 전환 사이 대기 시간(초)")]
    public float messageInterval = 3f;

    [Header("ProductionImage 페이드 설정")]
    public float fadeInTime = 1.5f;
    public float holdTime = 2.0f;
    public float fadeOutTime = 1.5f;
    private bool productionPlayed = false;

    private string lastRoom = "";
    Coroutine co;
    private readonly System.Collections.Generic.HashSet<string> doneRooms = new();


    void Start()
    {
        tmp = tutorialUI.GetComponentInChildren<TextMeshProUGUI>();
        if (slidePanel == null) slidePanel = tutorialUI.GetComponent<UISlidePanel>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (tutorialCanvasGroup == null) tutorialCanvasGroup = tutorialUI.AddComponent<CanvasGroup>();

        // 스토리 단계 오를 때(빙의로 해금) 대사 출력
        if (Player.Instance != null) Player.Instance.OnStageChanged += OnStoryAdvanced;
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.OnStageChanged -= OnStoryAdvanced;
    }

    private Coroutine storyCo;

    // Player.OnStageChanged 구독 핸들러
    private void OnStoryAdvanced(int stage)
    {
        if (lastStage == stage) return;   // 같은 단계 중복 방지
        lastStage = stage;

        if (storyCo != null) StopCoroutine(storyCo);
        storyCo = StartCoroutine(StartText());   // 표시/숨김은 코루틴이 관리
    }

    private void SetTutorialVisible(bool visible)
    {
        if (slidePanel == null) return;
        if (visible) slidePanel.Show();
        else slidePanel.Hide();
    }

    IEnumerator StartText()
    {

        tmp.text = "해당 생물은 조종을 시도하면 죽게 되는 듯합니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "대신 그 생물이 갖고 있는 정보를 취득할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "대신 그 생물이 갖고 있는 정보를 취득할 수 있습니다.";
        yield return new WaitForSeconds(messageInterval);

        tmp.text = "이 생물이 추출된 원본의 생물은 기뻐하고 있네요. 현재의 삶에.";
        yield return new WaitForSeconds(messageInterval);
    }
}