using UnityEngine;
using System;
using System.Collections;
using CreatureTypes;

[DisallowMultipleComponent]
public sealed class Decomposer : MonoBehaviour
{
    [Serializable]
    public struct DecomposeRule
    {
        [Tooltip("분해 대상 ID (CreatureID 참조)")]
        public CreatureID targetID;
        [Tooltip("스폰 후보 프리팹들 (각 스폰마다 랜덤 픽). 비어 있으면 스폰 없이 제거만.")]
        public GameObject[] options;
        [Tooltip("스폰 개수")]
        public int spawnCount;
    }

    [Header("References")]
    [SerializeField] private Creature self;
    [SerializeField] private ThinkChase thinkChase;

    [Header("Inspector Debug")]
    public Creature decomposeTarget;

    [Header("Range / Timing")]
    [Min(0f)] public float decomposeRange = 2.0f;
    [Min(0f)] public float attachDuration = 2.0f;

    [Header("Rules")]
    [SerializeField] private DecomposeRule[] rules;

    private bool isAttached = false;

    private void Start()
    {
        if (self == null) self = GetComponent<Creature>();
        if (thinkChase == null) thinkChase = GetComponent<ThinkChase>();
    }

    private void Update()
    {
        if (self == null || self.IsDead) return;
        if (isAttached) return;

        // D는 방 안 실제 생물 수가 한계 이상일 때만 분해하고, SS는 항상 분해한다.
        Room current = self.currentRoom;
        bool shouldDecompose = self.data.creatureID == CreatureID.D
            ? (current != null && CountRoomCreatures(current) >= current.maxCreaturesInRoom)
            : true;
        if (!shouldDecompose) return;

        // ThinkChase가 쫓고 있는 대상 가져옴
        Creature target = thinkChase != null ? thinkChase.chaseTarget : null;
        if (target == null || target.IsDead || target.data == null) return;
        if (!HasRuleFor(target.data.creatureID)) return;
        if (!self.HasAction(target.data.creatureID, InteractionAction.Decompose)) return;

        // 범위 안이면 달라붙기
        Vector3 selfPos = GetSelfPos();
        Vector3 tPos = target.rootTransform != null ? target.rootTransform.position : target.transform.position;
        if (Vector3.Distance(selfPos, tPos) <= decomposeRange)
        {
            decomposeTarget = target;
            target.intent = CreatureIntent.Decomposing;
            self.intent = CreatureIntent.Decomposing;
            // commit: 이 시점부터 chase는 손 떼게 함 (다른 target으로 갱신 방지)
            if (thinkChase != null) thinkChase.ClearChase();
            StartCoroutine(AttachRoutine(target));
        }
    }

    private IEnumerator AttachRoutine(Creature target)
    {
        isAttached = true;
        foreach (var mono in self.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            mono.enabled = false;
        }

        // 콜라이더 모두 비활성화 (target과 겹쳐서 물리 충돌 일어나는 거 방지)
        foreach (var col in self.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 리지드바디 kinematic으로
        Rigidbody rb = self.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 타겟에 붙이고 위치 초기화

        Transform attachTo = target.rootTransform;
        self.transform.SetParent(attachTo);
        self.transform.localPosition = Vector3.zero;
        foreach (var t in self.GetComponentsInChildren<Transform>())
            t.localPosition = Vector3.zero;



        // attachDuration 동안 대기
        yield return new WaitForSeconds(attachDuration);

        // 타겟이 살아있으면 분해
        if (target != null && !target.IsDead)
            DoDecompose(target);

        // 떼어내기
        self.transform.SetParent(null);
        if (rb != null) rb.isKinematic = false;
        foreach (var mono in self.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            mono.enabled = true;
        }
        foreach (var col in self.GetComponentsInChildren<Collider>())
            col.enabled = true;

        decomposeTarget = null;
        isAttached = false;
        self.intent = CreatureIntent.Wander;

        if (thinkChase != null)
        {
            thinkChase.ClearChase();
            thinkChase.enabled = true;
        }
    }

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

        // 즉사 처리
        target.TakeDamage(target.currentHP, self);

        // D일때만 분해 갯수 카운트
        if (self.data.creatureID == CreatureID.D)
            targetRoom?.NotifyDecomposed(target, self.data.creatureID);

        // 분해 산물 스폰
        if (rule.options != null && rule.options.Length > 0 && rule.spawnCount > 0)
        {
            Room registerTo = targetRoom != null ? targetRoom : self.currentRoom;
            for (int i = 0; i < rule.spawnCount; i++)
            {
                GameObject pick = rule.options[UnityEngine.Random.Range(0, rule.options.Length)];
                if (pick == null) continue;

                Vector3 spawnPos = pos + new Vector3(
                    UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

                if (registerTo != null)
                {
                    var roomCol = registerTo.GetComponent<Collider>();
                    if (roomCol != null)
                        spawnPos = roomCol.ClosestPoint(spawnPos);
                }

                GameObject obj = Instantiate(pick, spawnPos, Quaternion.identity);
                Creature c = obj.GetComponent<Creature>();
                if (c != null) registerTo?.RegisterCreature(c);
            }
        }
    }

    private Vector3 GetSelfPos()
    {
        return (self.rootTransform != null) ? self.rootTransform.position : self.transform.position;
    }
}
