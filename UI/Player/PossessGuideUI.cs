using UnityEngine;
using CreatureTypes;

// 조종(possess/따라오기) 중일 때 안내 패널을 상시 띄우는 UI.
//  - panel: 조종 중에만 켜질 묶음 ("() is following", "E/Q 고도 조절" 등 안내)
//  - iconMap: () 자리의 '생물 아이콘 오브젝트'들. 따라오는 생물에 해당하는 것만 켬(SetActive).
//    (L room/A room 숫자처럼 스프라이트를 미리 오브젝트로 만들어 두고 켜고 끄는 방식)
// 씬 Canvas 밑에 두면 됨. Player를 자동으로 찾아 조종 상태에 따라 panel을 켜고 끔.
public class PossessGuideUI : MonoBehaviour
{
    [System.Serializable]
    public struct IconEntry { public CreatureID id; public GameObject obj; }

    [Tooltip("조종 중에만 켜질 안내 패널")]
    public GameObject panel;

    [Header("따라오는 생물 아이콘 (선택) — 해당 생물일 때만 켬")]
    [Tooltip("생물마다 한 줄씩: CreatureID → 켤 아이콘 오브젝트. 목록에 없는 생물은 아이콘 없음")]
    public IconEntry[] iconMap;

    private CreaturePossess possess;
    private CreatureID _shownId = (CreatureID)(-1);

    private void Start()
    {
        if (panel == gameObject)
        {
            Debug.LogError("[PossessGuideUI] panel이 이 컴포넌트와 같은 오브젝트예요. " +
                           "panel을 끄면 이 스크립트도 멈춥니다 → panel은 '자식' 오브젝트로, 스크립트는 '부모(항상 active)'에 두세요.");
            return;
        }
        if (panel != null) panel.SetActive(false);
        SetIconFor((CreatureID)(-1));   // 시작 시 모든 아이콘 끔
    }

    private void Update()
    {
        if (possess == null && Player.Instance != null)
            possess = Player.Instance.GetComponent<CreaturePossess>();

        bool show = possess != null && possess.IsPossessing;
        if (panel != null && panel.activeSelf != show) panel.SetActive(show);

        Creature c = show ? possess.ControlledCreature : null;
        SetIconFor(c != null && c.data != null ? c.data.creatureID : (CreatureID)(-1));
    }

    // 해당 생물의 아이콘 오브젝트만 켜고 나머지는 끔 (숫자 글리프 켜듯이). 대상 바뀔 때만 적용.
    private void SetIconFor(CreatureID id)
    {
        if (id == _shownId) return;
        _shownId = id;
        if (iconMap == null) return;
        for (int i = 0; i < iconMap.Length; i++)
            if (iconMap[i].obj != null)
                iconMap[i].obj.SetActive(iconMap[i].id == id);
    }
}
