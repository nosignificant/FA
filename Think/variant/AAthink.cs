using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;


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