using UnityEngine;
using CreatureTypes;

// "statues" 패널: 레벨별 클리어 선택(ChoiceProgress의 L/A)을 이미지로 보여준다.
//  각 레벨 행의 "level N is"는 고정 이미지(패널에 그냥 배치),
//  이 스크립트는 레벨별 L/A 이미지 중 '기록된 선택'만 켠다. (없으면 둘 다 끔)
public class StatuesUI : MonoBehaviour
{
    [Header("레벨별 선택 이미지 ( [0]=level1, [1]=level2 ... )")]
    [Tooltip("그 레벨을 L로 클리어했을 때 켤 이미지")]
    public GameObject[] levelChoiceL;
    [Tooltip("그 레벨을 A로 클리어했을 때 켤 이미지")]
    public GameObject[] levelChoiceA;

    // StatuesMenu가 열 때 호출
    public void Show() => Refresh();

    private void Refresh()
    {
        var choices = ChoiceProgress.All();
        int n = Mathf.Max(Len(levelChoiceL), Len(levelChoiceA));
        for (int i = 0; i < n; i++)
        {
            bool isL = i < choices.Count && choices[i] == CreatureID.L;
            bool isA = i < choices.Count && choices[i] == CreatureID.A;
            SetAt(levelChoiceL, i, isL);
            SetAt(levelChoiceA, i, isA);
        }
    }

    private static int Len(GameObject[] a) => a != null ? a.Length : 0;
    private static void SetAt(GameObject[] a, int i, bool on)
    {
        if (a != null && i >= 0 && i < a.Length && a[i] != null) a[i].SetActive(on);
    }
}
