using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;


public class AAThink : TentacleThink
{
    private float grabbedSince = -1f;

    // H가 잡은 AA는 isBound로 알아서 멈추므로, 방에 H가 있어도 나머지 AA는 계속 L을 쫓아 변환.
    // (예전의 "방에 H 있으면 L 제외" 규칙은 제거 — 방 전체 AA가 멈춰버리는 부작용)
    public override bool IsValidTarget(Creature target)
    {
        return base.IsValidTarget(target);
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
            if (IsCreatureDormant(t)) continue;   // 휴면 H로부턴 안 도망
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