using System;
using System.Collections;
using UnityEngine;
using CreatureTypes;

public class Creature : MonoBehaviour
{

    [Header("References")]
    public Collider mainCollider;
    public Transform rootTransform;
    public CreatureData data;
    public CreatureIntent intent;
    public Interaction interact;
    public Room currentRoom;

    public Transform kabschIn;


    [Header("bool")]
    public bool isAttached = false;
    public bool wantToMigrate = false;

    [Header("Migration")]
    public bool canMigrate = false;
    [Min(0f)] public float migrateInterval = 8f;
    [Min(0f)] public float migrateWantDuration = 5f;
    [Range(0f, 1f)] public float migrateChance = 0.5f;

    [Header("Instance")]
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    public event Action<Creature, CreatureID> Died;

    protected virtual void Awake()
    {
        if (rootTransform == null)
        {
            if (mainCollider != null) rootTransform = mainCollider.transform;
            if (rootTransform == null) rootTransform = transform;
        }

        if (data == null)
            throw new InvalidOperationException($"{name}: data is not assigned.");

        if (interact == null) interact = gameObject.AddComponent<Interaction>();

        currentHP = data.maxHP;

        if (canMigrate) StartCoroutine(MigrateLoop());
    }

    // 서브클래스가 상황별로 확률을 바꾸려면 오버라이드 (예: AA는 옆방에 L 있으면 ↑)
    protected virtual float GetMigrateChance() => migrateChance;

    private IEnumerator MigrateLoop()
    {
        while (!IsDead)
        {
            // 일 처리 중(잡힘/합성/분해 등)이면 이주 안 함
            if (intent == CreatureIntent.Wander)
            {
                if (UnityEngine.Random.value < GetMigrateChance())
                {
                    wantToMigrate = true;
                    yield return new WaitForSeconds(migrateWantDuration);
                    wantToMigrate = false;
                }
                yield return new WaitForSeconds(migrateInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void TakeDamage(int amount, Creature who)
    {
        if (amount <= 0 || IsDead) return;

        SetHP(currentHP - amount);
        CreatureID w = who.data.creatureID;

        if (currentHP == 0)
            Die(w);
    }

    public void SetHP(int newHp)
    {
        int old = currentHP;
        currentHP = Mathf.Clamp(newHp, 0, data.maxHP);
    }


    public void Die(CreatureID who)
    {
        if (!gameObject.activeSelf) return;

        Died?.Invoke(this, who);
        Destroy(gameObject);
    }

    public bool HasAction(CreatureID targetCreatureId, InteractionAction action)
    {
        return interact != null && interact.HasAction(data.creatureID, targetCreatureId, action);
    }

    public int GetActionPriority(CreatureID targetCreatureId, InteractionAction action)
    {
        return interact != null
            ? interact.GetActionPriority(data.creatureID, targetCreatureId, action)
            : int.MinValue;
    }
    public virtual void AttachedTo(Transform attachPoint)
    {
        intent = CreatureIntent.Grabbed;
        // 자기 자신 처리
        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        transform.SetParent(attachPoint);
        transform.localPosition = Vector3.zero;

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = false;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (data.creatureID == CreatureID.S || data.creatureID == CreatureID.A)
            kabschIn.localPosition = Vector3.zero;

        foreach (var t in GetComponentsInChildren<Transform>())
            t.localPosition = Vector3.zero;

        rootTransform.SetParent(attachPoint);
        rootTransform.localPosition = Vector3.zero;
    }

    public virtual void Release()
    {
        intent = CreatureIntent.Wander;

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        transform.SetParent(null);

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = true;
        }

        // AttachedTo에서 끈 콜라이더 복구
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }
}
