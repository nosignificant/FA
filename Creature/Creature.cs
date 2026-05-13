using System;
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
    public void OnGrabbed(Transform attachPoint)
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
            mono.enabled = false;
        }

        if (data.creatureID == CreatureID.S || data.creatureID == CreatureID.A)
            kabschIn.localPosition = Vector3.zero;
    }

    public void OnReleased()
    {
        intent = CreatureIntent.Wander;

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        transform.SetParent(null);

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            mono.enabled = true;
        }
    }
}
