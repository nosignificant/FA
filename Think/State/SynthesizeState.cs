using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CreatureTypes;

class SynthesizeState : ThinkState
{
    private TentacleThink ttThink;
    private TentacleCreature tCreature;
    private TentacleGrab2 tentacleGrab;
    private float nextSynthTime = 0f;

    public SynthesizeState(TentacleThink think) : base(think)
    {
        ttThink = think;
    }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        if (prev.creature != null && think.IsValidTarget(prev.creature))
            newTarget = prev;
        tentacleGrab = ttThink.tentacleGrab;
        tCreature = ttThink.self as TentacleCreature;
    }

    public override void Refresh(List<Vector3> points)
    {
        if (tCreature == null || tentacleGrab == null) return;
        if (CanStartSynth()) ttThink.StartCoroutine(Synthesize());
    }

    private bool CanStartSynth()
    {
        if (tCreature.IsDead) return false;
        if (tCreature.isSynthesizing) return false;
        if (Time.time < nextSynthTime) return false;
        if (tentacleGrab.tentacles == null) return false;

        int grabbedCount = 0;
        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
        {
            if (tentacleGrab.tentacles[i].isGrabbing && tentacleGrab.tentacles[i].grabbedCreature != null)
                grabbedCount++;
        }

        CreatureID selfID = tCreature.data.creatureID;
        if (selfID == CreatureID.A) return grabbedCount >= 1;
        if (selfID == CreatureID.L) return grabbedCount >= 2;
        // AA 진입 판단(2마리 OR 1마리+타임아웃)은 AAThink가 이미 함 → 1마리 이상이면 진행
        if (selfID == CreatureID.AA) return grabbedCount >= 1;
        return false;
    }

    private IEnumerator Synthesize()
    {
        tCreature.isSynthesizing = true;
        yield return new WaitForSeconds(tentacleGrab.holdTime);

        Creature first = null;
        Creature second = null;

        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
        {
            Creature grabbed = tentacleGrab.tentacles[i].grabbedCreature;
            if (grabbed == null || !tentacleGrab.tentacles[i].isGrabbing) continue;

            if (first == null) first = grabbed;
            else if (second == null) { second = grabbed; break; }
        }

        if (first == null) { tCreature.isSynthesizing = false; tCreature.intent = CreatureIntent.Wander; yield break; }

        CreatureID selfID = tCreature.data.creatureID;
        // L은 2마리 필수. AA는 1마리(타임아웃)도 진행 → ResolveAA가 h/s→A 처리
        if (selfID == CreatureID.L && second == null)
        { tCreature.isSynthesizing = false; tCreature.intent = CreatureIntent.Wander; yield break; }

        CreatureID idA = first.data.creatureID;
        CreatureID idB = second != null ? second.data.creatureID : CreatureID.Player;

        SynthesisResult result = tCreature.synthesis.Resolve(selfID, idA, idB);

        Debug.Log($"[Synthesizer] {selfID}: {idA} + {idB} → valid={result.IsValid}");

        if (result.IsValid)
        {
            for (int n = 0; n < result.count; n++)
            {
                Vector3 spawnPos = tCreature.transform.position + new Vector3(
                    Random.Range(-0.5f, 0.5f), tCreature.transform.forward.y, Random.Range(-0.5f, 0.5f));
                GameObject obj = Object.Instantiate(result.prefab, spawnPos, Quaternion.identity);
                Creature newCreature = obj.GetComponent<Creature>();
                if (newCreature != null)
                    tCreature.currentRoom?.RegisterCreature(newCreature);
            }
        }

        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
        {
            if (tentacleGrab.tentacles[i].grabbedCreature != null)
                Object.Destroy(tentacleGrab.tentacles[i].grabbedCreature.gameObject);
            tentacleGrab.tentacles[i].isGrabbing = false;
            tentacleGrab.tentacles[i].grabbedCreature = null;
            tentacleGrab.tentacles[i].tentacle.target = tentacleGrab.tentacles[i].oldTarget;
        }

        nextSynthTime = Time.time + tCreature.synthesisCooldown;
        tCreature.isSynthesizing = false;
        tCreature.intent = CreatureIntent.Wander;
    }
}
