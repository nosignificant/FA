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
    private bool _dead;
    public bool IsDead => _dead;
    public bool IsGrabbed => intent == CreatureIntent.Decomposed || intent == CreatureIntent.Synthesized;
    // 플레이어가 조종 중 — 이 생물은 분해 대상이 되지 않음
    public bool IsControlled => intent == CreatureIntent.Controlled;

    [Tooltip("이 개체만 락온 불가로 (종 전체 data.lockable과 별개). 둘 다 true여야 락온 가능")]
    public bool lockable = true;
    public bool IsLockable => lockable && (data == null || data.lockable);

    // bind 상태: 움직임만 멈춤(정지) + 활성 유지(색 안 잃음). dormant(휴면)와 별개.
    [System.NonSerialized] public bool isBound;

    // 생성 시각(초). 개체 나이 판정 등에 사용. 작을수록 오래됨.
    public float SpawnTime { get; private set; }
    public float Age => Time.time - SpawnTime;


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

        SpawnTime = Time.time;
    }

    public void Die(CreatureID who)
    {
        if (_dead || !gameObject.activeSelf) return;

        _dead = true;
        Died?.Invoke(this, who);
        Destroy(gameObject);
    }

    public bool HasAction(CreatureID targetCreatureId, InteractionAction action)
    {
        return interact != null && interact.HasAction(data.creatureID, targetCreatureId, action);
    }

    // 이 생물이 대상에게 아무 상호작용이라도 갖는지 (관계 없으면 타겟 안 삼음)
    public bool HasAnyAction(CreatureID targetCreatureId)
    {
        return interact != null && interact.HasAnyAction(data.creatureID, targetCreatureId);
    }

    public int GetActionPriority(CreatureID targetCreatureId, InteractionAction action)
    {
        return interact != null
            ? interact.GetActionPriority(data.creatureID, targetCreatureId, action)
            : int.MinValue;
    }
    public virtual void AttachedTo(Transform attachPoint)
    {
        intent = grabbedBy != null && grabbedBy.HasAction(data.creatureID, InteractionAction.Decompose)
            ? CreatureIntent.Decomposed
            : CreatureIntent.Synthesized;
        _grabBodies = GetComponentsInChildren<Rigidbody>();
        _grabInterp = new RigidbodyInterpolation[_grabBodies.Length];
        for (int k = 0; k < _grabBodies.Length; k++)
        {
            _grabInterp[k] = _grabBodies[k].interpolation;
            _grabBodies[k].interpolation = RigidbodyInterpolation.None;
            _grabBodies[k].isKinematic = true;
        }

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

    // 잡힐 때 끈 Interpolation을 놓을 때 원복하기 위한 캐시
    [System.NonSerialized] private Rigidbody[] _grabBodies;
    [System.NonSerialized] private RigidbodyInterpolation[] _grabInterp;

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

        // 어느 방에도 없음(맵 이탈) → 마지막 방 안으로 되돌림 (밀려서 튕겨나가는 것 방지)
        // 단, 조종 중인 생물은 다리(방 밖)를 건널 수 있어야 하니 제외
        if (!IsControlled && currentRoom != null && currentRoom.homeBound != null)
        {
            Bounds b = currentRoom.homeBound.bounds;
            Vector3 inside = b.ClosestPoint(pos);
            Vector3 toCenter = b.center - inside; toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.01f) inside += toCenter.normalized * 1.5f;   // 경계 살짝 안쪽
            rootTransform.position = inside;

            var rb = rootTransform.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;   // 계속 밀리는 속도 죽임
        }
    }

    // reparent 없이 이동 컴포넌트·물리만 정지/재개 (분해 중 타겟을 제자리에서 멈출 때)
    public void SetMovementEnabled(bool on)
    {
        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = on;
        }
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = !on;
    }

    public virtual void Release()
    {
        grabbedBy = null;
        intent = CreatureIntent.Wander;

        // kinematic 해제 + 잡을 때 껐던 Interpolation 원복
        if (_grabBodies != null)
        {
            for (int k = 0; k < _grabBodies.Length; k++)
            {
                if (_grabBodies[k] == null) continue;
                _grabBodies[k].isKinematic = false;
                if (_grabInterp != null && k < _grabInterp.Length)
                    _grabBodies[k].interpolation = _grabInterp[k];
            }
            _grabBodies = null;
            _grabInterp = null;
        }
        else
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = false;
        }

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
