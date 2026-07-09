using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class FleeState : ThinkState
{
    public int currentPriority;
    public float runThreshold = 20f;
    public bool needToFlee = false;
    private Creature fleeTarget;

    public FleeState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        currentPriority = 0;
    }
    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        // 도망 중 다른 방으로 탈출 시도
        var mig = think.migration;
        if (mig != null && think.self.canMigrate && mig.TickMigration())
        {
            //mig의 point로 proxy 이동시킴
            newTarget.point = mig.migrateTargetPoint;
            return;
        }

        //현재 가장 도망쳐야하는 타겟 받아옴 
        Creature temp = BestFleeTarget(detected);
        if (temp == null)
        {
            think.LockState(false);
            return;
        }

        // lock은 "타겟 교체"만 막음
        bool locked = think.isLocked && newTarget.creature != null;
        if (locked && temp != newTarget.creature)
        {
            int priority = think.self.GetActionPriority(temp.data.creatureID, InteractionAction.Flee);
            if (priority < currentPriority)
                think.currentLockTime += think.waitInterval;
        }
        else
        {
            // 새 타겟 찾아옴
            newTarget.creature = temp;
            think.LockState(true);
            currentPriority = think.self.GetActionPriority(temp.data.creatureID, InteractionAction.Flee);
        }

        Creature ft = newTarget.creature;
        if (ft == null) return;
        fleeTarget = ft;

        float d = Vector3.Distance(think.self.rootTransform.position, ft.rootTransform.position);
        needToFlee = d < runThreshold;

        newTarget.point = GetNewPoint(points);
    }


    public Creature BestFleeTarget(IReadOnlyList<Creature> detected)
    {
        Creature self = think.self;
        if (detected == null || self == null || self.data == null) return null;

        float bestScore = float.NegativeInfinity;
        Creature best = null;

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!think.IsValidTarget(t)) continue;
            if (!self.HasAction(t.data.creatureID, InteractionAction.Flee)) continue;

            int priority = self.GetActionPriority(t.data.creatureID, InteractionAction.Flee);
            float dist = Vector3.Distance(GetSelfPos(), GetTargetPos(t));
            float score = priority * 1000f - dist;

            if (score > bestScore) { bestScore = score; best = t; }
        }

        fleeTarget = best;
        return best;
    }

    public override float Score(Vector3 point)
    {
        Creature self = think.self;
        if (self == null || self.data == null || detected == null || detected.Count == 0) return 0f;

        float total = 0f;
        Vector3 selfPos = self.rootTransform.position;

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (t == null || t.data == null) continue;

            Transform tf = t.rootTransform;

            if (IsBlocked(point, tf.position)) continue;

            float d = Vector3.Distance(point, tf.position);
            float distTerm = 1f / Mathf.Pow(d + 1f, 1.5f);

            float w = Mathf.Max(0f, t.data.weight);

            if (self.HasAction(t.data.creatureID, InteractionAction.Flee))
            {
                total -= self.data.fleeWeight * Mathf.Max(w, 1f) * distTerm;
            }
        }

        // fleeTarget 반대 방향 보너스
        if (fleeTarget != null)
        {
            Transform ft = fleeTarget.rootTransform;
            // 위협 - 나 방향 
            Vector3 awayDir = (selfPos - ft.position).normalized;
            // 나랑 지금 계산중인 포인트 방향 
            Vector3 toPoint = (point - selfPos).normalized;
            float awayDot = Mathf.Max(0f, Vector3.Dot(awayDir, toPoint)); // 반대 방향일수록 1
            total += awayDot * self.data.fleeWeight; ;
        }

        return total;
    }
}