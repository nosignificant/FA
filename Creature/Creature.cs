using System;
using UnityEngine;
using CreatureTypes;

public class Creature : MonoBehaviour
{

    [Header("References")]
    public Collider mainCollider;
    public Transform rootTransform;
    public CreatureData data;
    public Interaction interact;
    public Room currentRoom;

    [Header("bool")]
    public bool isAttached = false;

    [Header("Instance")]
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    public event Action<Creature, CreatureID> Died;

    protected void Awake()
    {
        if (rootTransform == null)
        {
            if (mainCollider != null) rootTransform = mainCollider.transform;
            if (rootTransform == null) rootTransform = transform;
        }

        if (data == null)
            throw new InvalidOperationException($"{name}: data is not assigned.");

        if (interact == null) interact = gameObject.AddComponent<Interaction>();

        ApplyIdentity();
        currentHP = data.maxHP;
    }

    protected void ApplyIdentity()
    {
        if (!string.IsNullOrWhiteSpace(data.creatureName)) ;
        // gameObject.name = data.creatureName;
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
}