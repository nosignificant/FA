using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;

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
