using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class ChaseState : ThinkState
{
    public int currentPriority;
    public float runThreshold = 10f;
    public bool needToChase = false;
    public bool targetChangedRoom = false;
    public ChaseState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        if (prev.creature != null && think.IsValidTarget(prev.creature))
            newTarget = prev;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        if (newTarget.creature != null &&
            (newTarget.creature.IsGrabbed ||
             !think.IsValidTarget(newTarget.creature))) //쫓고 있던 대상이 유효하지 않으면 취소함
        {
            newTarget.creature = null;
            think.LockState(false);
        }

        Creature temp = BestChaseTarget(detected);
        if (temp == null || temp.data == null)
        {
            // 같은 방엔 쫓을 대상이 없지만, 잠금된 타겟이 다른 방으로 갔으면 migration으로 따라감
            // (안 그러면 BestChaseTarget가 옆방 타겟을 빼버려서 proxy가 얼어붙음 = EQS 멈춤)
            Creature lockedTarget = newTarget.creature;
            if (lockedTarget != null && lockedTarget.currentRoom != think.self.currentRoom)
            {
                var mig = think.migration;
                if (mig != null && think.self.canMigrate && mig.TickMigration())
                {
                    newTarget.point = mig.migrateTargetPoint;
                    return;
                }
            }
            return;
        }

        bool locked = think.isLocked && newTarget.creature != null;
        if (locked && temp != newTarget.creature)
        {
            if (newTarget.creature.currentRoom != think.self.currentRoom)
            {
                var mig = think.migration;
                if (mig != null && think.self.canMigrate && mig.TickMigration())
                {
                    //mig의 point로 proxy 이동시킴
                    newTarget.point = mig.migrateTargetPoint;
                    return;
                }
            }
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

            // 잠금 상태고 쫓고 있는 대상이 다른 방으로 이동했으면 같이 이동 
            if (newTarget.creature.currentRoom != think.self.currentRoom)
            {
                var mig = think.migration;
                if (mig != null && think.self.canMigrate && mig.TickMigration())
                {
                    newTarget.point = mig.migrateTargetPoint;
                    return;
                }
            }
            think.LockState(true);
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

        if (think is TentacleThink tt && tt.tentacleGrab != null && newTarget.creature != null)
            tt.tentacleGrab.TryGrab(newTarget.creature);
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
            if (t.IsGrabbed) continue;   // 이미 잡힌 건 안 쫓음
            if (t.currentRoom != self.currentRoom) continue;    // 다른 방 생물은 안 쫓음 (방 밖으로 새는 것 방지)
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