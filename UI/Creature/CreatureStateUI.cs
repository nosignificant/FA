using UnityEngine;
using TMPro;
using System;
using CreatureTypes;

public class CreatureStateUI : MonoBehaviour
{
    public GameObject statuesRectPrefab;
    public Transform gridParent;

    private PlayerLockOn pl;

    private string lastKnownState;

    private Creature targetCreature;
    private CreatureIntent? lastIntent;

    private void Awake()
    {
        if (pl == null && Player.Instance != null)
            pl = Player.Instance.GetComponent<PlayerLockOn>();
    }

    void Update()
    {
        if (pl == null) return;
        targetCreature = pl.targetCreature;

        CreatureIntent cur = targetCreature.intent;
        if (lastIntent == null || cur != lastIntent.Value)
        {
            lastIntent = cur;
            Refresh();
        }
    }

    private void Refresh()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        if (targetCreature == null) return;

        CreatureIntent current = targetCreature.intent;

        foreach (CreatureIntent intent in Enum.GetValues(typeof(CreatureIntent)))
        {
            if (intent == current) continue;

            var go = Instantiate(statuesRectPrefab, gridParent);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = intent.ToString();
        }
    }
}