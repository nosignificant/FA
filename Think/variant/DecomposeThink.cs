using UnityEngine;
using CreatureTypes;

public class Dthink : Think2
{
    [Header("Decompose")]
    [Min(0f)] public float decomposeRange = 2f;
    [Min(0f)] public float chaseThreshold = 10f;   // 이 거리보다 멀면 더 다가가야 함
    [Min(0f)] public float attachDuration = 2f;
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

    protected bool CanDecompose()
    {
        // D: 방 포화 상태일 때만 / 그 외: 항상
        Room current = self.currentRoom;
        bool shouldDecompose = self.data.creatureID == CreatureID.D
            ? (current != null && CountRoomCreatures(current) >= current.maxCreaturesInRoom)
            : true;
        if (!shouldDecompose) return false;

        // 쫓던 대상 유효성 확인
        Creature target = currentTarget.creature;
        if (target == null || target.IsDead || target.data == null) return false;
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

    private int CountRoomCreatures(Room room)
    {
        if (room == null || room.creatureList == null) return 0;
        int count = 0;
        for (int i = 0; i < room.creatureList.Count; i++)
        {
            Creature c = room.creatureList[i];
            if (c == null || c.data == null) continue;
            if (c.data.creatureID == CreatureID.Player) continue;
            if (c.data.creatureID == CreatureID.Door) continue;
            count++;
        }
        return count;
    }

    private void initDecomposeState()
    {
        // D는 H, HH, S, SS, A를 분해 (산물 없음)
        var dRules = new DecomposeState.DecomposeRule[]
        {
            new DecomposeState.DecomposeRule { targetID = CreatureID.H,  productIDs = null, spawnCount = 0 },
            new DecomposeState.DecomposeRule { targetID = CreatureID.HH, productIDs = null, spawnCount = 0 },
            new DecomposeState.DecomposeRule { targetID = CreatureID.S,  productIDs = null, spawnCount = 0 },
            new DecomposeState.DecomposeRule { targetID = CreatureID.SS, productIDs = null, spawnCount = 0 },
            new DecomposeState.DecomposeRule { targetID = CreatureID.A,  productIDs = null, spawnCount = 0 },
        };

        // SS는 A→H/S, AA→A 2개
        var ssRules = new DecomposeState.DecomposeRule[]
        {
            new DecomposeState.DecomposeRule
            {
                targetID   = CreatureID.A,
                productIDs = new[] { CreatureID.H, CreatureID.S },
                spawnCount = 1
            },
            new DecomposeState.DecomposeRule
            {
                targetID   = CreatureID.AA,
                productIDs = new[] { CreatureID.A },
                spawnCount = 2
            },
        };

        rules = (self.data.creatureID == CreatureID.D) ? dRules : ssRules;
        decomposeState = new DecomposeState(this, rules, decomposeRange, attachDuration);
    }
}
