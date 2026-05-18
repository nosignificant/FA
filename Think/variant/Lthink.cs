using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;

public class Lthink : TentacleThink
{
    // L은 다른 L이 있는 방으로는 이주하지 않음
    protected override bool ShouldAvoidRoom(Room room)
    {
        if (room == null || room.creatureList == null) return false;
        return room.creatureList.Any(x =>
            x != null && x.data != null && x.data.creatureID == CreatureID.L);
    }

    private bool AAInRoom()
    {
        var room = self.currentRoom;
        if (room == null || room.creatureList == null) return false;
        return room.creatureList.Any(c =>
            c != null && c != self && c.data != null && c.data.creatureID == CreatureID.AA);
    }

    protected override CreatureIntent DetermineIntent()
    {
        // 같은 방에 AA 있으면 하던 일 다 제치고 이주 (Wander → GetMigrateChance=1)
        if (AAInRoom()) return CreatureIntent.Wander;

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
                if (c.currentRoom.creatureList.Any(x =>
                        x != null && x.data != null && x.data.creatureID == CreatureID.L))
                    return false;
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
