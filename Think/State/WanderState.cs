using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class WanderState : ThinkState
{
    public float changeTargetThreshold = 2f;
    public bool needToGetNewPoint = false;
    public bool hasTarget = false;

    public WanderState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        hasTarget = false;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        if (!hasTarget)
        {
            newTarget.point = GetNewPoint(points);
            hasTarget = true;
        }

        float d = Vector3.Distance(think.self.rootTransform.position, newTarget.point);
        needToGetNewPoint = d < changeTargetThreshold;
        if (needToGetNewPoint)
        {
            oldTarget = newTarget;
            newTarget.point = GetNewPoint(points);
        }
    }

    public override float Score(Vector3 point)
    {
        float total = 0f;

        float dAnchor = Vector3.Distance(point, oldTarget.point);
        float minDist = 5f;
        float anchorPenalty = 2.0f;
        if (dAnchor < minDist)
            total -= (minDist - dAnchor) * anchorPenalty;

        return total;
    }
}
