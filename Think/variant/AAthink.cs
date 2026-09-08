using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;


public class AAThink : TentacleThink
{
    private float grabbedSince = -1f;

    // 방에 H가 있으면 L을 잡지 않음 (H가 곧 묶을 테니 물러남 → H가 깨끗이 bind)
    public override bool IsValidTarget(Creature target)
    {
        if (!base.IsValidTarget(target)) return false;
        if (target.data != null && target.data.creatureID == CreatureID.L
            && self.currentRoom != null && self.currentRoom.HasSpecies(CreatureID.H)) return false;
        return true;
    }

    private int GrabbedCount()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return 0;
        int n = 0;
        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
            if (tentacleGrab.tentacles[i].isGrabbing && tentacleGrab.tentacles[i].grabbedCreature != null)
                n++;
        return n;
    }

    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;

        // L을 잡으면 aaSingleGrabTimeout 동안 들고 대기 → 그 뒤 A로 변환 (1:1).
        if (GrabbedCount() >= 1)
        {
            if (grabbedSince < 0f) grabbedSince = Time.time;
            float hold = (self as AACreature)?.aaSingleGrabTimeout ?? 3f;
            if (Time.time - grabbedSince >= hold) return CreatureIntent.Synthesizing;
            return CreatureIntent.Wander;   // 잡은 채 대기 (새로 안 쫓음 → 2개 잡아 손실나는 것 방지)
        }

        grabbedSince = -1f;
        return DoesNeedToChase() ? CreatureIntent.Chase : CreatureIntent.Wander;
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