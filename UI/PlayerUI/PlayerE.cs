using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerE : MonoBehaviour
{
    public TMP_Text playerE;
    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
    }

    void Update()
    {
        CheckIfLearned();
    }

    void CheckIfLearned()
    {
        if (Player.Instance == null) { cg.alpha = 0f; return; }

        // C
        if (Player.Instance.isSelectingTransform)
        {
            cg.alpha = 1f;
            var selected = Player.Instance.plUI.GetSelectedData();
            if (selected == null) { cg.alpha = 0f; return; }

            bool canTransform = Player.Instance.learnedForms.Contains(selected);
            playerE.text = "press E to transform";
            return;
        }

        // 락온
        if (!Player.Instance.isTracking) { cg.alpha = 0f; return; }

        var target = Player.Instance.pl?.targetCreature;
        if (target == null) { cg.alpha = 0f; return; }

        // var spawner = target.GetComponent<GeneralSpawner>();
        // if (spawner == null) { cg.alpha = 0f; return; }

        // if (spawner.spawnData == null) { cg.alpha = 0f; return; }

        // cg.alpha = 1f;
        // bool isLearned = Player.Instance.learnedForms.Contains(spawner.spawnData);

        // if (!isLearned)
        //     playerE.text = "press E to learn";
        else if (!Player.Instance.isInteract)
            playerE.text = "press E to interact";
        else
            playerE.text = "press E to confirm";
    }


}