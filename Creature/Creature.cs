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
    public bool grabbable = true;
    public bool canMigrate = true;
    private Coroutine _grabImmunityCo;

    public void SetGrabImmunity(float seconds)
    {
        if (_grabImmunityCo != null) StopCoroutine(_grabImmunityCo);
        _grabImmunityCo = StartCoroutine(GrabImmunityRoutine(seconds));
    }

    private System.Collections.IEnumerator GrabImmunityRoutine(float seconds)
    {
        grabbable = false;
        yield return new WaitForSeconds(seconds);
        grabbable = true;
        _grabImmunityCo = null;
    }



    [Header("Instance")]
    public int currentHP;
    public bool IsDead => currentHP <= 0;
    public bool IsGrabbed => intent == CreatureIntent.Decomposed || intent == CreatureIntent.Synthesized;


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
    public virtual void AttachedTo(Transform attachPoint)
    {
        intent = grabbedBy != null && grabbedBy.data?.creatureID == CreatureID.D
            ? CreatureIntent.Decomposed
            : CreatureIntent.Synthesized;

        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        transform.SetParent(attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = false;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if ((data.creatureID == CreatureID.S || data.creatureID == CreatureID.A)
            && kabschIn != null)
        {
            kabschIn.localPosition = Vector3.zero;
            foreach (var t in kabschIn.GetComponentsInChildren<Transform>())
                t.localPosition = Vector3.zero;
        }

        if (rootTransform != null) rootTransform.localPosition = Vector3.zero;
    }

    [System.NonSerialized] public Creature grabbedBy;

    void Update()
    {
        if (IsGrabbed)
        {
            if (grabbedBy == null || grabbedBy.IsDead)
            {
                grabbedBy = null;
                Release();
            }
        }

        UpdateRoomMembership();
    }

    // 방 소속은 hierarchy/intent와 무관하게 오직 위치(bounds)로 결정 — 모든 생물 공통 단일 경로
    void UpdateRoomMembership()
    {
        if (RoomManager.Instance == null || rootTransform == null) return;
        if (data == null) return;
        if (data.creatureID == CreatureID.Player || data.creatureID == CreatureID.Door) return;
        if (IsGrabbed) return;   // 잡힌 동안은 위치 기반 소속 갱신 안 함

        Vector3 pos = rootTransform.position;

        // 이미 현재 방 안이면 빠른 종료 (대부분의 프레임)
        if (currentRoom != null && currentRoom.homeBound != null &&
            currentRoom.homeBound.bounds.Contains(pos)) return;

        foreach (var kvp in RoomManager.Instance.rooms)
        {
            Room r = kvp.Value;
            if (r == null || r == currentRoom || r.homeBound == null) continue;
            if (r.homeBound.bounds.Contains(pos))
            {
                currentRoom?.UnregisterCreature(this);
                r.RegisterCreature(this);
                return;
            }
        }
    }

    public virtual void Release()
    {
        grabbedBy = null;
        intent = CreatureIntent.Wander;

        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;

        transform.SetParent(null);

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = true;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }
}
