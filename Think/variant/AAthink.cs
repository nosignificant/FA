using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;


public class AAThink : TentacleThink
{
    private float singleGrabSince = -1f;

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

        int g = GrabbedCount();

        // 2마리 → 즉시 합성
        if (g >= 2) { singleGrabSince = -1f; return CreatureIntent.Synthesizing; }

        // 1마리 → 2마리째 계속 사냥, 일정 시간 못 잡으면 그거 소비해 A 합성
        if (g == 1)
        {
            if (singleGrabSince < 0f) singleGrabSince = Time.time;
            float timeout = (self as AACreature)?.aaSingleGrabTimeout ?? 5f;
            if (Time.time - singleGrabSince >= timeout)
                return CreatureIntent.Synthesizing;
            return DoesNeedToChase() ? CreatureIntent.Chase : CreatureIntent.Wander;
        }

        singleGrabSince = -1f;
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