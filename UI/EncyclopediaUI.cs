using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Collections.Generic;

public class EncyclopediaUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text nameText;

    [Header("Action List")]
    public ActionEntry actionEntryPrefab;
    public Transform actionListParent;   // Horizontal Layout Group

    private readonly List<ActionEntry> pool = new();

    public void Show(Creature creature)
    {
        if (nameText != null) nameText.text = "";
        HideAll();

        if (creature == null || creature.data == null) return;

        string n = string.IsNullOrEmpty(creature.data.creatureName)
            ? creature.data.creatureID.ToString()
            : creature.data.creatureName;
        if (nameText != null) nameText.text = n;

        if (creature.interact == null) return;

        CreatureID selfID = creature.data.creatureID;
        int idx = 0;

        foreach (InteractionAction action in System.Enum.GetValues(typeof(InteractionAction)))
        {
            if (action == InteractionAction.Ignore) continue;

            var targets = new List<string>();
            foreach (CreatureID tid in System.Enum.GetValues(typeof(CreatureID)))
            {
                if (tid == selfID) continue;
                if (creature.interact.HasAction(selfID, tid, action))
                    targets.Add(tid.ToString());
            }

            if (targets.Count == 0) continue;

            ActionEntry e = GetEntry(idx++);
            e.gameObject.SetActive(true);
            e.Set(action.ToString(), targets);
        }
    }

    private ActionEntry GetEntry(int index)
    {
        while (pool.Count <= index)
        {
            var e = Instantiate(actionEntryPrefab, actionListParent);
            pool.Add(e);
        }
        pool[index].gameObject.SetActive(true);
        return pool[index];
    }

    private void HideAll()
    {
        foreach (var e in pool) e.gameObject.SetActive(false);
    }
}
