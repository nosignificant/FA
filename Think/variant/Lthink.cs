using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;

public class Lthink : TentacleThink
{
    public override bool IsFleeTargetInScope(Creature target)
    {
        return target != null && self != null && self.currentRoom != null
            && target.currentRoom == self.currentRoom;
    }

    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        var tc = self as TentacleCreature;
        if (tc != null && tc.isSynthesizing) return CreatureIntent.Synthesizing;
        else if (DoesNeedToChase())
        {
            if (CanSynthesize()) return CreatureIntent.Synthesizing;
            return CreatureIntent.Chase;
        }
        return CreatureIntent.Wander;
    }
    protected override bool DoesNeedToFlee()
    {
        //스캐너 동작안하면 false
        if (detected == null) return false;
        // 도망 대상으로 lock되어 있어도 대상이 유효하고 같은 방에 있을 때만 유지.
        // 대상이 사라졌거나 방을 벗어났는데 lock만으로 Flee가 유지되는 것을 막음.
        Creature lockedTarget = currentTarget.creature;
        if (self.intent == CreatureIntent.Flee && isLocked && IsValidTarget(lockedTarget)
            && IsFleeTargetInScope(lockedTarget)
            && !IsCreatureDormant(lockedTarget)
            && self.HasAction(lockedTarget.data.creatureID, InteractionAction.Flee))
        {
            return true;
        }

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (!IsFleeTargetInScope(t)) continue;   // 다른 방 생물엔 도망 안 감
            if (self.HasAction(t.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }
    protected override bool DoesNeedToChase()
    {
        if (detected == null) return false;

        //락이 걸려있을 때 
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
                if (migration == null) return false;
                if (migration.ShouldAvoidRoom(c.currentRoom)) return false;
            }
            return true;
        }

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (t.IsGrabbed) continue;
            if (t.currentRoom != self.currentRoom) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

}
