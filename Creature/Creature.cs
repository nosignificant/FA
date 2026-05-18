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
    public bool grabbable = true;   // 인스턴스별 grab 가능 여부 (면역 쿨다운용)

    private Coroutine _grabImmunityCo;

    /// <summary>일정 시간 동안 grab 면역 (예: L에서 풀린 직후 재포획 방지)</summary>
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


    [Header("Migration")]
    public bool canMigrate = false;
    [Min(0f)] public float migrateInterval = 8f;
    [Min(0f)] public float migrateWantDuration = 5f;
    [Range(0f, 1f)] public float migrateChance = 0.5f;
    [Tooltip("방 이동 후 이 시간 동안 재이동 금지")]
    public float migrateCooldown = 8f;
    [System.NonSerialized] public float lastMigrateTime = -999f;
    public bool MigrateOnCooldown => Time.time - lastMigrateTime < migrateCooldown;
    [Tooltip("이 종들 중 하나라도 같은 방에 있으면 다른 방으로 가고 싶어함")]
    public CreatureID[] avoidCreatureIDs;

    protected bool AvoidedKindInRoom()
    {
        if (avoidCreatureIDs == null || avoidCreatureIDs.Length == 0) return false;
        if (currentRoom == null || currentRoom.creatureList == null) return false;

        for (int i = 0; i < currentRoom.creatureList.Count; i++)
        {
            var c = currentRoom.creatureList[i];
            if (c == null || c == this || c.data == null) continue;

            for (int j = 0; j < avoidCreatureIDs.Length; j++)
                if (c.data.creatureID == avoidCreatureIDs[j]) return true;
        }
        return false;
    }

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
    protected virtual float GetMigrateChance()
    {
        // 피하고 싶은 종이 방에 있으면 무조건 떠나고 싶어함
        if (AvoidedKindInRoom()) return 1f;
        return migrateChance;
    }

    private IEnumerator MigrateLoop()
    {
        while (!IsDead)
        {
            // 방 이동 직후 쿨타임 동안은 이주 의향 끔
            if (MigrateOnCooldown)
            {
                wantToMigrate = false;
                yield return null;
                continue;
            }

            if (intent == CreatureIntent.Flee)
            {
                // 도망 중엔 무조건 다른 방으로 탈출 시도
                wantToMigrate = true;
                yield return null;
            }
            else if (intent == CreatureIntent.Wander)
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
                // 잡힘/합성/분해 등 — 이주 안 함
                wantToMigrate = false;
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

        if ((data.creatureID == CreatureID.S || data.creatureID == CreatureID.A)
            && kabschIn != null)
        {
            kabschIn.localPosition = Vector3.zero;
            // kabschIn 하위 오브젝트들도 전부 localPosition 0
            foreach (var t in kabschIn.GetComponentsInChildren<Transform>())
                t.localPosition = Vector3.zero;
        }

        // OLD 방식: rootTransform은 부모 안 바꾸고 localPosition만 0
        if (rootTransform != null) rootTransform.localPosition = Vector3.zero;
    }

    public virtual void Release()
    {
        intent = CreatureIntent.Wander;

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        // OLD 방식: transform만 떼면 됨 (rootTransform은 애초에 안 옮겼음)
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
