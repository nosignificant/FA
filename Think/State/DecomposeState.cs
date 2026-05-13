using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CreatureTypes;

// Decomposer MonoBehaviour의 로직을 ThinkState 서브클래스로 이식
// config(rules, range, duration)는 Think2 서브클래스에서 생성자로 주입
class DecomposeState : ThinkState
{
    [Serializable]
    public struct DecomposeRule
    {
        public CreatureID targetID;
        public GameObject[] options;
        public int spawnCount;
    }

    // ── config (Think2 서브클래스에서 주입) ──────────────────────────────
    private readonly DecomposeRule[] rules;
    private readonly float decomposeRange;
    private readonly float attachDuration;

    // ── 런타임 상태 ──────────────────────────────────────────────────────
    public Creature decomposeTarget { get; private set; }
    private bool isAttached = false;

    public DecomposeState(Think2 think, DecomposeRule[] rules, float decomposeRange, float attachDuration)
        : base(think)
    {
        this.rules      = rules;
        this.decomposeRange  = decomposeRange;
        this.attachDuration  = attachDuration;
    }

    // ── ThinkState overrides ─────────────────────────────────────────────

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);

        // ChaseState에서 쫓던 대상을 이어받음
        if (prev.creature != null && think.IsValidTarget(prev.creature))
            newTarget = prev;

        isAttached = false;
        decomposeTarget = null;
    }

    public override void Refresh(List<Vector3> points)
    {
        if (think.self == null || think.self.IsDead) return;
        if (isAttached) return;

        // D: 방 포화 상태일 때만 / 그 외: 항상
        Room current = think.self.currentRoom;
        bool shouldDecompose = think.self.data.creatureID == CreatureID.D
            ? (current != null && CountRoomCreatures(current) >= current.maxCreaturesInRoom)
            : true;
        if (!shouldDecompose) return;

        // 쫓던 대상 유효성 확인
        Creature target = newTarget.creature;
        if (target == null || target.IsDead || target.data == null) return;
        if (!HasRuleFor(target.data.creatureID)) return;
        if (!think.self.HasAction(target.data.creatureID, InteractionAction.Decompose)) return;

        // 범위 체크 → 달라붙기
        Vector3 selfPos  = GetSelfPos();
        Vector3 targetPos = target.rootTransform != null ? target.rootTransform.position : target.transform.position;

        if (Vector3.Distance(selfPos, targetPos) <= decomposeRange)
        {
            decomposeTarget       = target;
            target.intent         = CreatureIntent.Decomposing;
            think.self.intent     = CreatureIntent.Decomposing;
            think.StartCoroutine(AttachRoutine(target));
        }
    }

    public override ThinkTarget Exit()
    {
        // 강제 상태 이탈 시 붙어있던 것 정리
        if (isAttached)
        {
            think.self.transform.SetParent(null);
            RestoreComponents();
            isAttached = false;
        }
        decomposeTarget = null;
        return base.Exit();
    }

    // ── 핵심 루틴 ────────────────────────────────────────────────────────

    private IEnumerator AttachRoutine(Creature target)
    {
        isAttached = true;

        // Think2(MonoBehaviour)는 유지, 나머지 컴포넌트 비활성화
        foreach (var mono in think.self.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono == think)   continue;   // 브레인 자신은 유지
            mono.enabled = false;
        }

        foreach (var col in think.self.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Rigidbody rb = think.self.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 타겟에 붙이고 위치 초기화
        Transform attachTo = target.rootTransform != null ? target.rootTransform : target.transform;
        think.self.transform.SetParent(attachTo);
        think.self.transform.localPosition = Vector3.zero;
        foreach (var t in think.self.GetComponentsInChildren<Transform>())
            t.localPosition = Vector3.zero;

        yield return new WaitForSeconds(attachDuration);

        // 분해 실행
        if (target != null && !target.IsDead)
            DoDecompose(target);

        // 떼어내기 & 복원
        think.self.transform.SetParent(null);
        if (rb != null) rb.isKinematic = false;
        RestoreComponents();

        decomposeTarget   = null;
        isAttached        = false;
        think.self.intent = CreatureIntent.Wander;
    }

    private void RestoreComponents()
    {
        foreach (var mono in think.self.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            mono.enabled = true;
        }
        foreach (var col in think.self.GetComponentsInChildren<Collider>())
            col.enabled = true;
    }

    private void DoDecompose(Creature target)
    {
        DecomposeRule rule = default;
        bool found = false;
        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i].targetID == target.data.creatureID)
            {
                rule  = rules[i];
                found = true;
                break;
            }
        }
        if (!found) return;

        Vector3 pos        = target.transform.position;
        Room    targetRoom = target.currentRoom;

        // 즉사
        target.TakeDamage(target.currentHP, think.self);

        // D일 때만 분해 카운트 알림
        if (think.self.data.creatureID == CreatureID.D)
            targetRoom?.NotifyDecomposed(target, think.self.data.creatureID);

        // 분해 산물 스폰
        if (rule.options != null && rule.options.Length > 0 && rule.spawnCount > 0)
        {
            Room registerTo = targetRoom != null ? targetRoom : think.self.currentRoom;
            for (int i = 0; i < rule.spawnCount; i++)
            {
                GameObject pick = rule.options[UnityEngine.Random.Range(0, rule.options.Length)];
                if (pick == null) continue;

                Vector3 spawnPos = pos + new Vector3(
                    UnityEngine.Random.Range(-1f, 1f), 0f,
                    UnityEngine.Random.Range(-1f, 1f));

                if (registerTo != null)
                {
                    var roomCol = registerTo.GetComponent<Collider>();
                    if (roomCol != null)
                        spawnPos = roomCol.ClosestPoint(spawnPos);
                }

                GameObject  obj = UnityEngine.Object.Instantiate(pick, spawnPos, Quaternion.identity);
                Creature    c   = obj.GetComponent<Creature>();
                if (c != null) registerTo?.RegisterCreature(c);
            }
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

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
            if (c.data.creatureID == CreatureID.Door)   continue;
            count++;
        }
        return count;
    }
}
