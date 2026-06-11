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
        // 도망으로 lock되어있으면 도망
        if (self.intent == CreatureIntent.Flee && isLocked) return true;

        //스캐너 안에 생물 중에 flee가 있는지 확인
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (t == null || t.data == null) continue;
            bool flee = self.HasAction(t.data.creatureID, InteractionAction.Flee);
        }

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
