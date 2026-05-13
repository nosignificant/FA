using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerLearnedUI : MonoBehaviour
{
    [Header("prefab")]

    public GameObject creatureInfoPrefab;

    [Header("ref")]

    public Transform listParent;
    public CreatureDatabase cdBase;
    private CanvasGroup cg;
    public GameObject currentSelected;
    public int currentIndex = 0;

    [Header("private")]

    private List<(CreatureData data, bool isLearned)> sortedList = new();

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);
    }
    private void OnEnable()
    {
        BuildAndRefresh();
    }

    private void BuildAndRefresh()
    {
        if (Player.Instance.learnedForms.Count == 0) return;
        sortedList = cdBase.allCreatures
            .OrderBy(d => d.creatureID)
            .Select(d => (d, Player.Instance.learnedForms.Contains(d)))
            .ToList();

        RefreshUI();
        RefreshCurrentSelected();
    }

    private void RefreshUI()
    {
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        for (int i = 0; i < sortedList.Count; i++)
        {
            bool isCurrent = i == currentIndex;
            var go = Instantiate(creatureInfoPrefab, listParent);
            var info = go.GetComponent<CreatureInfoEntry>();
            //info.SetData(sortedList[i].data, sortedList[i].isLearned, isCurrent);
        }
    }

    public void IndexUpDown(int index)
    {
        currentIndex += index;

        if (currentIndex >= sortedList.Count) currentIndex = 0;
        if (currentIndex < 0) currentIndex = sortedList.Count - 1;

        RefreshUI();
        RefreshCurrentSelected();
    }



    public void RefreshCurrentSelected()
    {
        if (sortedList.Count == 0) return;

        //currentSelected.GetComponent<CurrentSelected>().SetData(sortedList[currentIndex].data, sortedList[currentIndex].isLearned);
    }
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            BuildAndRefresh();
            cg.alpha = 1f;
        }
        else
        {
            cg.alpha = 0f;
        }
    }

    public CreatureData GetSelectedData()
    {
        if (sortedList.Count == 0) return null;
        return sortedList[currentIndex].data;
    }

}
