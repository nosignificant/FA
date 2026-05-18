using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;

[DisallowMultipleComponent]
public sealed class CreatureAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Creature self;
    [SerializeField] private Think2 think;

    [Header("Targeting")]
    [Min(0.02f)] public float thinkInterval = 0.2f;

    [Header("Attack")]
    [Min(0f)] public float attackRange = 2.0f;
    public int attackDamage = 1;
    [Min(0.01f)] public float attackCooldown = 10.0f;
    [Min(0f)] public float restAfterHit = 0.35f;

    [Header("Runtime")]
    public Creature enemy;
    public Creature lastEnemy;

    private float nextThinkTime;
    private float nextAttackTime;

    private void Awake()
    {
        if (self == null) self = GetComponent<Creature>();
        if (think == null) think = GetComponent<Think2>();
    }

    private void OnEnable()
    {
        nextThinkTime = 0f;
        nextAttackTime = 0f;
        enemy = null;
        lastEnemy = null;
    }

    private void Update()
    {
        if (self == null || self.IsDead) return;

        // 일정 주기로 공격 대상 갱신
        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + thinkInterval;
            Creature picked = FindBestAttackTarget();
            if (picked != null) lastEnemy = picked;
            enemy = picked;
        }

        if (enemy == null || enemy.IsDead) return;
        if (Time.time < nextAttackTime) return;

        StartCoroutine(DoAttack());
        nextAttackTime = Time.time + attackCooldown;
    }

    /// <summary>
    /// scanner 결과 중 Attack priority가 가장 높고 attackRange 안에 있는 대상 반환.
    /// </summary>
    private Creature FindBestAttackTarget()
    {
        if (think == null || think.scanner == null) return null;
        IReadOnlyList<Creature> results = think.scanner.Results;
        if (results == null) return null;

        Vector3 selfPos = GetSelfPos();

        Creature best = null;
        int bestPriority = int.MinValue;
        float bestDist = float.MaxValue;

        for (int i = 0; i < results.Count; i++)
        {
            var t = results[i];
            if (t == null || t == self || t.IsDead || t.data == null) continue;
            if (!t.data.canAttackThis) continue;
            if (!self.HasAction(t.data.creatureID, InteractionAction.Attack)) continue;

            Vector3 tPos = (t.rootTransform != null) ? t.rootTransform.position : t.transform.position;
            float dist = Vector3.Distance(selfPos, tPos);
            if (dist > attackRange) continue;

            int priority = self.GetActionPriority(t.data.creatureID, InteractionAction.Attack);

            if (priority > bestPriority || (priority == bestPriority && dist < bestDist))
            {
                bestPriority = priority;
                bestDist = dist;
                best = t;
            }
        }

        return best;
    }

    private IEnumerator DoAttack()
    {
        if (enemy == null || enemy.IsDead) yield break;

        enemy.TakeDamage(attackDamage, self);
        yield return new WaitForSeconds(restAfterHit);
    }

    private Vector3 GetSelfPos()
    {
        return (self.rootTransform != null) ? self.rootTransform.position : self.transform.position;
    }
}
