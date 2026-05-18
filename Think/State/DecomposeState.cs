using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CreatureTypes;

class DecomposeState : ThinkState
{
    [Serializable]
    public struct DecomposeRule
    {
        public CreatureID targetID;
        public CreatureID[] productIDs;   // 스폰될 후보 ID들 (랜덤 픽). 빈 배열이면 산물 없음
        public int spawnCount;
    }
    private readonly DecomposeRule[] rules;
    private readonly float decomposeRange;
    private readonly float attachDuration;
    private readonly float decomposeCooldown;

    // ── 런타임 상태 ──────────────────────────────────────────────────────
    public Creature decomposeTarget { get; private set; }
    private bool isAttached = false;
    private float nextDecomposeTime = 0f;

    public DecomposeState(Dthink think, DecomposeRule[] rules, float decomposeRange, float attachDuration, float decomposeCooldown)
        : base(think)
    {
        this.rules = rules;
        this.decomposeRange = decomposeRange;
        this.attachDuration = attachDuration;
        this.decomposeCooldown = decomposeCooldown;
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
        if (Time.time < nextDecomposeTime) return;   // 분해 쿨타임

        // 결정(범위/룰 체크)은 Dthink.CanDecompose가 이미 했음.
        // 여기선 넘겨받은 target에 붙기만 함.
        Creature target = newTarget.creature;
        if (target == null || target.IsDead) return;

        decomposeTarget = target;
        target.grabbedBy = think.self;          // D가 사라지면 타겟이 스스로 풀림
        target.intent = CreatureIntent.Grabbed;
        think.StartCoroutine(AttachRoutine(target));
    }

    public override ThinkTarget Exit()
    {
        // 강제 상태 이탈 시 붙어있던 것 정리
        if (isAttached)
        {
            think.self.Release();
            isAttached = false;
        }
        decomposeTarget = null;
        return base.Exit();
    }

    // ── 핵심 루틴 ────────────────────────────────────────────────────────

    private IEnumerator AttachRoutine(Creature target)
    {
        isAttached = true;

        // 타겟의 rootTransform에 정확히 붙임
        Transform attachTo = target.rootTransform != null ? target.rootTransform : target.transform;
        think.self.AttachedTo(attachTo);

        // D 본체 + 모든 자식 localPosition 0 → 타겟에 딱 겹치게
        var st = think.self.transform;
        st.localPosition = Vector3.zero;
        foreach (var t in think.self.GetComponentsInChildren<Transform>(true))
            t.localPosition = Vector3.zero;
        if (think.self.rootTransform != null)
            think.self.rootTransform.localPosition = Vector3.zero;

        yield return new WaitForSeconds(attachDuration);

        // 먼저 타겟에서 떨어진다 (타겟이 죽으면 자식인 D도 같이 파괴되므로)
        think.self.Release();

        // 그 다음 분해 (타겟 사망 처리)
        if (target != null && !target.IsDead)
            DoDecompose(target);

        decomposeTarget = null;
        isAttached = false;
        nextDecomposeTime = Time.time + decomposeCooldown;   // 다음 분해까지 쿨타임
        think.self.intent = CreatureIntent.Wander;
    }

    private void DoDecompose(Creature target)
    {
        DecomposeRule rule = default;
        bool found = false;
        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i].targetID == target.data.creatureID)
            {
                rule = rules[i];
                found = true;
                break;
            }
        }
        if (!found) return;

        Vector3 pos = target.transform.position;
        Room targetRoom = target.currentRoom;

        // 즉사
        target.TakeDamage(target.currentHP, think.self);

        // D일 때만 분해 카운트 알림
        if (think.self.data.creatureID == CreatureID.D)
            targetRoom?.NotifyDecomposed(target, think.self.data.creatureID);

        // 분해 산물 스폰
        if (rule.productIDs != null && rule.productIDs.Length > 0 && rule.spawnCount > 0)
        {
            Room registerTo = targetRoom != null ? targetRoom : think.self.currentRoom;
            CreatureDatabase db = registerTo?.creatureDB;
            if (db == null)
            {
                Debug.LogWarning($"[DecomposeState] {registerTo?.roomID}에 creatureDB가 없어 산물을 스폰할 수 없습니다.");
                return;
            }

            for (int i = 0; i < rule.spawnCount; i++)
            {
                CreatureID pickID = rule.productIDs[UnityEngine.Random.Range(0, rule.productIDs.Length)];
                GameObject pick = db.GetPrefab(pickID);
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

                GameObject obj = UnityEngine.Object.Instantiate(pick, spawnPos, Quaternion.identity);
                Creature c = obj.GetComponent<Creature>();
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
            if (c.data.creatureID == CreatureID.Door) continue;
            count++;
        }
        return count;
    }
}
