using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;

public abstract class TentacleThink : Think2
{
    [Header("Tentacle")]
    public TentacleGrab2 tentacleGrab;

    [Header("State")]
    private SynthesizeState synthesizeState;

    protected override void Awake()
    {
        base.Awake();
        if (tentacleGrab == null) tentacleGrab = GetComponentInChildren<TentacleGrab2>();
        initSynthesizeState();
    }

    protected override ThinkState GetThinkState(CreatureIntent intent)
    {
        if (intent == CreatureIntent.Synthesizing) return synthesizeState;
        return base.GetThinkState(intent);
    }

    // 촉수 양쪽에 생물을 잡았는지 확인
    protected bool CanSynthesize()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return false;
        //잡은 개수 확인
        int grabbedCount = 0;
        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
        {
            if (tentacleGrab.tentacles[i].isGrabbing && tentacleGrab.tentacles[i].grabbedCreature != null)
                grabbedCount++;
        }

        CreatureID selfID = self.data.creatureID;
        if (selfID == CreatureID.A) return grabbedCount >= 1;
        if (selfID == CreatureID.AA || selfID == CreatureID.L) return grabbedCount >= 2;
        return false;
    }

    private void initSynthesizeState()
    {
        synthesizeState = new SynthesizeState(this);
    }

    protected bool IsGrabbedByMe(Creature target)
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return false;
        foreach (var slot in tentacleGrab.tentacles)
            if (slot.grabbedCreature == target) return true;
        return false;
    }
}