using UnityEngine;

// 조종(possess) 중일 때 "E, Q로 고도 조절" 안내를 상시 띄우는 UI.
// 씬 Canvas 밑에 두면 됨. Player를 자동으로 찾아 조종 상태에 따라 panel을 켜고 끔.
// (전역 유지 X — 씬마다 이 UI를 두되 Player 연결은 자동)
public class PossessGuideUI : MonoBehaviour
{
    [Tooltip("조종 중에만 켜질 안내 패널")]
    public GameObject panel;

    private CreaturePossess possess;

    private void Start()
    {
        if (panel == gameObject)
        {
            Debug.LogError("[PossessGuideUI] panel이 이 컴포넌트와 같은 오브젝트예요. " +
                           "panel을 끄면 이 스크립트도 멈춥니다 → panel은 '자식' 오브젝트로, 스크립트는 '부모(항상 active)'에 두세요.");
            return;
        }
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (possess == null && Player.Instance != null)
            possess = Player.Instance.GetComponent<CreaturePossess>();

        bool show = possess != null && possess.IsPossessing;
        if (panel != null && panel.activeSelf != show) panel.SetActive(show);
    }
}
