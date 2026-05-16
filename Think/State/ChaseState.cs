using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class ChaseState : ThinkState
{
    public int currentPriority;
    public float runThreshold = 20f;
    public bool needToChase = false;
    public bool targetChangedRoom = false;
    public ChaseState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        // 새로 chase 진입 시 lock 해제: 첫 Refresh가 전체 경로를 타서
        // newTarget.point가 반드시 세팅되도록 (안 그러면 Vector3.zero로 남음)
        think.LockThink(false);
        if (prev.creature != null && think.IsValidTarget(prev.creature))
            newTarget = prev;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        Creature temp = BestChaseTarget(detected);
        if (temp == null || temp.data == null) return;

        // lock은 "타겟 교체"만 막음. 같은 타겟이면 위치는 계속 갱신.
        bool locked = think.isLocked && newTarget.creature != null;
        if (locked && temp != newTarget.creature)
        {
            // 더 우선순위 높은 대상이면 lock 시간 소모(곧 풀림)
            int priority = think.self.GetActionPriority(temp.data.creatureID, InteractionAction.Chase);
            if (priority < currentPriority)
                think.currentLockTime += think.waitInterval;
            // 교체는 안 하지만 기존 타겟 추적은 계속 (아래로 진행)
        }
        else
        {
            // 미잠금 or 같은 타겟 → 타겟 갱신/확정
            newTarget.creature = temp;
            think.LockThink(true);
            currentPriority = think.self.GetActionPriority(temp.data.creatureID, InteractionAction.Chase);
        }

        // 항상 현재 타겟 위치로 갱신 (한 자리 고정 방지)
        Creature chaseC = newTarget.creature;
        if (chaseC == null) return;
        newTarget.point = chaseC.rootTransform.position;

        float d = Vector3.Distance(think.self.rootTransform.position, newTarget.point);

        //나랑 타겟 사이 거리가 더 멀면 다가가야함
        needToChase = d > runThreshold;
        if (needToChase) newTarget.point = GetNewPoint(points);

        if (think is TentacleThink tt && tt.tentacleGrab != null)
        {
            tt.tentacleGrab.TryGrab(newTarget.creature);
        }
    }

    public Creature BestChaseTarget(IReadOnlyList<Creature> detected)
    {
        Creature self = think.self;
        if (detected == null || self == null || self.data == null) return null;

        float bestScore = float.NegativeInfinity;
        Creature best = null;

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!think.IsValidTarget(t)) continue;
            if (!self.HasAction(t.data.creatureID, InteractionAction.Chase)) continue;

            int priority = self.GetActionPriority(t.data.creatureID, InteractionAction.Chase);
            float dist = Vector3.Distance(GetSelfPos(), GetTargetPos(t));
            float score = priority * 1000f - dist;

            if (score > bestScore) { bestScore = score; best = t; }
        }

        return best;
    }


    public override float Score(Vector3 point)
    {
        Creature self = think.self;
        Creature chaseTarget = newTarget.creature;
        if (self == null || self.data == null) return 0f;
        if (chaseTarget == null || chaseTarget.data == null) return 0f;
        if (detected == null) return 0f;

        float total = 0f;
        Vector3 targetPos = chaseTarget.rootTransform.position;
        float distToTarget = Vector3.Distance(point, targetPos);

        if (IsBlocked(point, targetPos)) return float.NegativeInfinity;

        total += self.data.chaseWeight * (1f / (distToTarget + 1f));

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (t == null || t.data == null) continue;

            Transform tf = t.rootTransform;
            if (IsBlocked(point, tf.position)) continue;

            float d = Vector3.Distance(point, tf.position);
            float distTerm = 1f / Mathf.Pow(d + 1f, 1.5f);
            float w = t.data.weight;

            if (self.HasAction(t.data.creatureID, InteractionAction.Flee))
            {
                total -= self.data.fleeWeight * Mathf.Max(1f, w) * distTerm;
                continue;
            }
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase))
            {
                total += 0.25f * distTerm;
            }
        }

        return total;
    }
}