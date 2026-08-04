using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// weed 전용 wander: 평소엔 proxy를 제자리에 두고(배회 없음),
// 스캐너에 다른 생물이 잡히면 가장 가까운 그 생물 쪽으로만 proxy를 뻗는다.
class WeedWanderState : ThinkState
{
    public WeedWanderState(Think2 think) : base(think) { }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        Creature target = FindNearest();
        if (target != null)
            newTarget.point = target.rootTransform.position;   // 생물 쪽으로 뻗음
        else
            newTarget.point = GetSelfPos();                    // 아무도 없으면 제자리(회수)
    }

    // 스캔된 생물 중 가장 가까운 유효 타겟
    private Creature FindNearest()
    {
        if (detected == null) return null;

        Vector3 selfPos = GetSelfPos();
        Creature best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < detected.Count; i++)
        {
            var c = detected[i];
            if (!think.IsValidTarget(c)) continue;

            float sqr = (c.rootTransform.position - selfPos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }
}
