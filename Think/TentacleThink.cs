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

    protected bool CanSynthesize()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return false;

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

public class Lthink : TentacleThink
{
    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        else if (DoesNeedToChase())
        {
            if (CanSynthesize()) return CreatureIntent.Synthesizing;
            return CreatureIntent.Chase;
        }
        return CreatureIntent.Wander;
    }
    protected override bool DoesNeedToFlee()
    {
        if (detected == null) return false;
        if (self.intent == CreatureIntent.Flee && isLocked) return true;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }
    protected override bool DoesNeedToChase()
    {
        if (detected == null || self == null || self.data == null) return false;

        if (self.intent == CreatureIntent.Chase && isLocked && currentTarget.creature != null)
        {
            Creature c = currentTarget.creature;
            for (int i = 0; i < detected.Count; i++)
            {
                var t = detected[i];
                if (!IsValidTarget(t)) continue;
                if (self.HasAction(t.data.creatureID, InteractionAction.Flee))
                {
                    return
                        self.GetActionPriority(t.data.creatureID, InteractionAction.Flee) <=
                        self.GetActionPriority(c.data.creatureID, InteractionAction.Chase);
                }
            }
            if (c.currentRoom != self.currentRoom)
            {
                if (!IsValidTarget(c) || IsGrabbedByMe(c)) return false;
            }
            return true;
        }

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

}

public class AAthink : TentacleThink
{
    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        else if (DoesNeedToChase())
        {
            if (CanSynthesize()) return CreatureIntent.Synthesizing;
            return CreatureIntent.Chase;
        }
        return CreatureIntent.Wander;
    }
    protected override bool DoesNeedToFlee()
    {
        if (detected == null) return false;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }
    protected override bool DoesNeedToChase()
    {
        if (detected == null) return false;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }
}