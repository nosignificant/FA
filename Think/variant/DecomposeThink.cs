using UnityEngine;
using CreatureTypes;

public class Dthink : Think2
{
    [Header("Decompose")]
    [Min(0f)] public float decomposeRange = 2f;
    [Min(0f)] public float chaseThreshold = 10f;   // 이 거리보다 멀면 더 다가가야 함
    [Min(0f)] public float attachDuration = 2f;
    [Min(0f)] public float decomposeCooldown = 8f;   // 분해 후 다음 분해까지 대기
    public bool needToChase = false;

    private DecomposeState decomposeState;
    private DecomposeState.DecomposeRule[] rules;

    protected override void Awake()
    {
        base.Awake();
        initDecomposeState();
    }

    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToChase())
        {
            if (CanDecompose()) return CreatureIntent.Decomposing;
            return CreatureIntent.Chase;
        }
        return CreatureIntent.Wander;
    }

    protected override ThinkState GetThinkState(CreatureIntent intent)
    {
        if (intent == CreatureIntent.Decomposing) return decomposeState;
        return base.GetThinkState(intent);
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

        if (self.intent == CreatureIntent.Chase && isLocked && currentTarget.creature != null)
        {
            Creature c = currentTarget.creature;
            float d = Vector3.Distance(SelfPos(), TargetPos(c));

            // 거리가 멀면 더 가까이 다가가야 함
            needToChase = d > chaseThreshold;
            if (needToChase) return true;
        }

        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

    // D는 발열 상태에서만, 그리고 방에서 '가장 오래된 분해가능 개체'만 대상으로 삼는다.
    // (SS 등 다른 분해자는 기존 방식 그대로)
    public override bool IsValidTarget(Creature target)
    {
        if (!base.IsValidTarget(target)) return false;
        if (self.data.creatureID != CreatureID.D) return true;

        if (self.currentRoom == null || !self.currentRoom.IsHot) return false;  // 발열 아니면 분해 안 함
        return target == OldestDecomposeTarget();
    }

    // 현재 감지된 것 중, 같은 방에서 D가 분해 가능한 가장 오래된 개체
    private Creature OldestDecomposeTarget()
    {
        if (detected == null) return null;

        Creature oldest = null;
        float oldestTime = float.MaxValue;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (t == null || t.IsDead || t.data == null) continue;
            if (t.IsGrabbed) continue;
            if (t.IsControlled) continue;   // 조종 중인 생물은 분해 안 함
            if (t.currentRoom != self.currentRoom) continue;
            if (!HasRuleFor(t.data.creatureID)) continue;
            if (!self.HasAction(t.data.creatureID, InteractionAction.Decompose)) continue;

            if (t.SpawnTime < oldestTime) { oldestTime = t.SpawnTime; oldest = t; }
        }
        return oldest;
    }

    protected bool CanDecompose()
    {
        // 쫓던 대상 유효성 확인
        Creature target = currentTarget.creature;
        if (target == null || target.IsDead || target.data == null) return false;
        if (target.IsControlled) return false;   // 조종 중인 생물은 분해 안 함
        if (!HasRuleFor(target.data.creatureID)) return false;
        if (!self.HasAction(target.data.creatureID, InteractionAction.Decompose)) return false;

        // 범위 안이면 분해 가능
        float dist = Vector3.Distance(SelfPos(), TargetPos(target));
        return dist <= decomposeRange;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private Vector3 SelfPos() =>
        self.rootTransform != null ? self.rootTransform.position : self.transform.position;

    private Vector3 TargetPos(Creature t) =>
        t.rootTransform != null ? t.rootTransform.position : t.transform.position;

    private bool HasRuleFor(CreatureID id)
    {
        if (rules == null) return false;
        for (int i = 0; i < rules.Length; i++)
            if (rules[i].targetID == id) return true;
        return false;
    }

    private void initDecomposeState()
    {
        // D는 개체수 조절용: 넘치는 입자 L/A만 분해 (산물 없음)
        rules = new DecomposeState.DecomposeRule[]
        {
            new DecomposeState.DecomposeRule { targetID = CreatureID.L, productIDs = null, spawnCount = 0 },
            new DecomposeState.DecomposeRule { targetID = CreatureID.A, productIDs = null, spawnCount = 0 },
        };

        decomposeState = new DecomposeState(this, rules, decomposeRange, attachDuration, decomposeCooldown);
    }
}
