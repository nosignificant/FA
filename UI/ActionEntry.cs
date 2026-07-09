using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ActionEntry : MonoBehaviour
{
    public TMP_Text actionLabel;
    public Transform childList;       // Vertical Layout Group
    public Entry childEntryPrefab;

    private readonly List<Entry> pool = new();

    public void Set(string actionName, List<string> targets)
    {
        if (actionLabel != null) actionLabel.text = actionName;

        // 필요한 만큼만 꺼내 쓰기
        for (int i = 0; i < targets.Count; i++)
        {
            Entry e = GetEntry(i);
            e.Set(targets[i], false);
        }

        // 남는 거 숨기기
        for (int i = targets.Count; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private Entry GetEntry(int index)
    {
        while (pool.Count <= index)
        {
            var e = Instantiate(childEntryPrefab, childList);
            pool.Add(e);
        }
        pool[index].gameObject.SetActive(true);
        return pool[index];
    }
}
