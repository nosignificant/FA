using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 레벨(씬)이 로드되면 그 씬의 인트로 대사를 순차로 띄운다.
// 대사 데이터는 P53.IntroLines(스토리 텍스트 단일 출처)에서 가져오고, 여기선 표시만 담당한다.
// 각 레벨 씬에 이 컴포넌트를 하나 두고 panelUI만 연결하면, 씬 이름으로 알아서 대사를 고른다.
public class LevelIntro : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelUI;          // UISlidePanel + TMP가 붙은 오브젝트
    public UISlidePanel slidePanel;     // 비우면 panelUI에서 자동 획득
    private TextMeshProUGUI tmp;

    [Header("타이밍")]
    public float startDelay = 0.5f;
    public float messageInterval = 3f;
    [Tooltip("대사가 끝나도 패널을 숨기지 않고 레벨 내내 유지 (false면 마지막 줄 유지)")]
    public bool hideOnEnd = false;
    [Tooltip("끝난 뒤 유지할 마지막 문구 (비우면 마지막 대사 줄 유지)")]
    public string keepLine = "";

    private void Start()
    {
        if (panelUI != null)
        {
            if (slidePanel == null) slidePanel = panelUI.GetComponent<UISlidePanel>();
            tmp = panelUI.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (tmp == null) return;

        string scene = SceneManager.GetActiveScene().name;
        string[] lines = P53.GetIntroLines(scene);   // 대사는 P53에서
        if (lines == null || lines.Length == 0) return;

        StartCoroutine(Play(lines));
    }

    private IEnumerator Play(string[] lines)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        slidePanel?.Show();

        for (int i = 0; i < lines.Length; i++)
        {
            tmp.text = lines[i];
            yield return new WaitForSeconds(messageInterval);
        }

        if (hideOnEnd)
        {
            slidePanel?.Hide();
        }
        else
        {
            // 레벨 내내 유지: 원하면 마지막 문구로 교체하고 패널은 계속 표시
            if (!string.IsNullOrEmpty(keepLine)) tmp.text = keepLine;
            slidePanel?.Show();
        }
    }
}
