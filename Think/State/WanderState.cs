using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class WanderState : ThinkState
{
    public float changeTargetThreshold = 5f;
    public float maxStayTime = 3f;          // 점에 못 닿아도 이 시간 지나면 강제 갱신
    public bool needToGetNewPoint = false;
    public bool hasTarget = false;
    private float lastRefreshTime;
    private float goToOtherRoom = 5f;

    public WanderState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        hasTarget = false;
        lastRefreshTime = Time.time;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        if (!hasTarget)
        {
            newTarget.point = GetNewPoint(points);
            hasTarget = true;
            lastRefreshTime = Time.time;
        }
        // 옆방에 Chase 대상이 있거나 랜덤값 이상이 나오면 이주 시도
        var mig = think.migration;
        float r = Random.Range(1, 10) * think.self.data.wanderWeight;
        if (mig != null && mig.canMigrate && mig.TickMigration())
        {
            if (r > goToOtherRoom && mig.HasChaseTargetInAdjacentRoom())
                newTarget.point = mig.migrateTargetPoint;
            return;
        }

        // 도달했거나 너무 오래 머물렀으면 새 점
        float d = Vector3.Distance(think.self.rootTransform.position, newTarget.point);
        bool reached = d < changeTargetThreshold;
        bool tooLong = Time.time - lastRefreshTime > maxStayTime;
        needToGetNewPoint = reached || tooLong;

        if (needToGetNewPoint)
        {
            oldTarget = newTarget;
            newTarget.point = GetNewPoint(points);
            lastRefreshTime = Time.time;
        }
    }

    public override float Score(Vector3 point)
    {
        float total = 0f;

        float dAnchor = Vector3.Distance(point, oldTarget.point);
        float minDist = 10f;
        float anchorPenalty = 2.0f;
        if (dAnchor < minDist)
            total -= (minDist - dAnchor) * anchorPenalty;

        return total;
    }
}
