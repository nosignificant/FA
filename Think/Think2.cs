using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;

//state 결정에 대해서 서술
//대상을 쫓아가다 다른 flee해야하는 대상을 만나면 flee로 상태 전환

[System.Serializable]
public struct ThinkTarget
{
    public Vector3 point;
    public Creature creature;
}

public class Think2 : MonoBehaviour
{
    [Header("References")]
    public Creature self;
    public CreatureScanner scanner;
    public TargetControl targetControl;
    public RoomMigration migration;
    protected IReadOnlyList<Creature> detected;

    [Header("Vision")]
    public LayerMask obstacleMask;

    public bool isLocked = false;
    public float lockTime = 5f;
    public float currentLockTime = 0f;
    public float waitInterval = 0.2f;

    [Header("currentTarget")]
    public ThinkTarget currentTarget;

    [Header("State")]
    private ThinkState oldState;
    private ThinkState currentState;
    private FleeState fleeState;
    private ChaseState chaseState;
    private WanderState wanderState;

    [Header("EQS (Point Query)")]
    public float pointSpacing = 2f;
    public int maxPoints = 35;

    [Header("Wander")]
    [Tooltip("점에 못 닿아도 이 시간마다 새 wander 점")]
    public float wanderChangeInterval = 3f;
    [Tooltip("점에 이만큼 가까워지면 새 wander 점")]
    public float wanderReachThreshold = 5f;


    [Header("Debug")]
    public bool drawEqsPoints = true;
    protected Transform proxyTarget;

    protected virtual void Awake()
    {
        if (scanner == null) scanner = GetComponent<CreatureScanner>();
        if (self == null) self = GetComponent<Creature>();
        if (targetControl == null) targetControl = GetComponent<TargetControl>();
        if (migration == null) migration = GetComponent<RoomMigration>();

        initStates();
        EnsureProxyTarget();

        if (targetControl != null) targetControl.SetMovementTarget(proxyTarget);

        currentTarget = new ThinkTarget
        {
            creature = self,
            point = self.rootTransform.position
        };

        currentState = wanderState;
        oldState = wanderState;
    }

    private void Start()
    {
        StartCoroutine(ThinkLoop());
    }

    // 상시 시뮬 설정 (RoomManager 전역)
    private bool SimInactive => RoomManager.Instance != null && RoomManager.Instance.simulateInactiveRooms;
    private float InactiveScale => RoomManager.Instance != null ? Mathf.Max(1f, RoomManager.Instance.inactiveTickScale) : 1f;

    private IEnumerator ThinkLoop()
    {
        while (self.currentRoom == null) yield return null;
        while (true)
        {
            LetsThink();

            // 비활성 방은 (상시 시뮬 시) 느린 틱으로 계속 연산
            bool inactive = self.currentRoom != null && !self.currentRoom.isActive;
            float interval = (inactive && SimInactive) ? waitInterval * InactiveScale : waitInterval;
            yield return new WaitForSeconds(interval);
        }
    }

    private void Update()
    {
        if (isLocked)
        {
            currentLockTime += Time.deltaTime;
            if (currentLockTime >= lockTime) LockState(false);
        }

    }

    //상태 잠금 
    public void LockState(bool setLock)
    {
        if (setLock == false) currentLockTime = 0f;
        isLocked = setLock;
    }

    [System.NonSerialized] public bool manualControl = false;
    private bool _frozen = false;   // dormant/bind 진입 시 proxy 1회 고정 후 완전 정지
    // 휴면 상태면 제자리 정지. HBinder/SBinder가 이 필드를 설정(H는 자기·AA 정지, S는 자기 정지).
    [System.NonSerialized] public bool dormant = false;
    protected virtual bool IsDormant() => dormant;
    public Transform ProxyTarget => proxyTarget;

    //플레이어가 조종하는 거 어떤 타겟을 가리키고 있든 프록시 타겟을 따라가게 만듦
    public void SetManualControl(bool on)
    {
        manualControl = on;
        if (on && targetControl != null && proxyTarget != null)
            targetControl.SetMovementTarget(proxyTarget);
    }

    private void LetsThink()
    {
        // weed 등 새 프리팹에서 컴포넌트 누락 시 어디가 빈지 알려주고 크래시 방지
        if (self == null || scanner == null || currentState == null)
        {
            Debug.LogWarning($"[Think2] {name}: self={self != null} scanner={scanner != null} " +
                             $"currentState={currentState != null} — 컴포넌트 누락 확인");
            return;
        }

        if (manualControl) return;
        if (self.IsGrabbed) return;
        // 정지: bind/dormant면 진입 시 proxy를 자기 위치로 한 번만 고정 → 이후 think 완전 정지(EQS 안 움직임)
        if (self.isBound || IsDormant())
        {
            if (!_frozen) { MoveProxy(self.rootTransform.position); _frozen = true; }
            return;
        }
        _frozen = false;
        // 비활성 방은 상시 시뮬이 꺼져 있을 때만 완전 정지
        if (self.currentRoom != null && !self.currentRoom.isActive && !SimInactive) return;
        //방 기준 아니고 주변 전체 기준
        detected = scanner.Results;
        var newIntent = DetermineIntent();
        var newState = GetThinkState(newIntent);

        //상태 다르면 이전 상태값 넘겨줌
        if (newState != currentState)
        {
            ThinkTarget carry = currentState?.Exit() ?? default;
            newState.Enter(carry);
            currentState = newState;
            self.intent = newIntent;
        }
        //상태 같으면 다시 생각함
        currentState.Refresh(BuildQueryPoints());
        //상태마다 타겟 가짐
        currentTarget = currentState.newTarget;
        MoveProxy(currentTarget.point);
    }

    protected virtual CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        else if (DoesNeedToChase()) return CreatureIntent.Chase;
        return CreatureIntent.Wander;
    }
    protected virtual bool DoesNeedToFlee()
    {
        if (detected == null) return false;
        if (self.intent == CreatureIntent.Flee && isLocked && currentTarget.creature != null) return true;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }
    protected virtual bool DoesNeedToChase()
    {
        if (detected == null) return false;
        if (self.intent == CreatureIntent.Chase && isLocked && currentTarget.creature != null) return true;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (t.IsGrabbed) continue;   // 이미 잡힌 건 안 쫓음
            if (t.currentRoom != self.currentRoom) continue;    // 다른 방 생물은 안 쫓음
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

    public virtual bool IsValidTarget(Creature target)
    {
        if (target == null || target == self || target.IsDead || target.data == null) return false;
        // 상호작용 관계가 전혀 없는 생물은 대상으로 안 삼음 (walkingWeed 등)
        if (self != null && self.data != null && !self.HasAnyAction(target.data.creatureID)) return false;
        return true;
    }

    //state//
    protected virtual ThinkState GetThinkState(CreatureIntent intent)
    {
        switch (intent)
        {
            case CreatureIntent.Wander: return wanderState;
            case CreatureIntent.Flee: return fleeState;
            case CreatureIntent.Chase: return chaseState;
            default: return wanderState;
        }
    }

    private void initStates()
    {
        fleeState = new FleeState(this);
        chaseState = new ChaseState(this);
        wanderState = new WanderState(this)
        {
            maxStayTime = wanderChangeInterval,
            changeTargetThreshold = wanderReachThreshold
        };
    }

    /////////////
    //build proxy codes
    ////////////
    /// 
    private List<Vector3> BuildQueryPoints()
    {
        List<Vector3> points = new();
        //스캐너 반경만큼
        float r = Mathf.Max(0.1f, scanner.scanRadius);
        // 이만큼의 간격에 
        float s = Mathf.Max(0.5f, pointSpacing);
        //내 rootTransform을 중심으로 해서
        Vector3 center = self.rootTransform.position;
        // 내 방 있는지 확인
        bool hasBounds = self.currentRoom != null;

        //이 개수만큼 점을 찍음 
        for (int i = 0; i < maxPoints; i++)
        {
            Vector3 p = center + Random.insideUnitSphere * r;
            //내 방이 있을 때 벗어나면 제외, 내 방 없으면 그냥 돌아다니게 
            if (hasBounds && !IsInsideBounds(p, self.currentRoom.homeBound.bounds, 0f)) continue;
            //걷는 생물이면 y높이 제한 
            if (self.data != null && self.data.walkingCreature) { if (p.y > center.y + 15 || p.y < center.y - 15) continue; }
            points.Add(p);
        }
        return points;
    }

    public bool IsInsideBounds(Vector3 p, Bounds b, float shrink = 3f)
    {
        if (self.currentRoom == null) return false;

        // bounds를 shrink만큼 줄이기
        Vector3 min = b.min + Vector3.one * shrink;
        Vector3 max = b.max - Vector3.one * shrink;

        return p.x > min.x && p.x < max.x &&
               p.y > min.y && p.y < max.y &&
               p.z > min.z && p.z < max.z;
    }
    public void MoveProxy(Vector3 p)
    {
        proxyTarget.position = p;
    }
    private void OnDestroy()
    {
        if (proxyTarget != null) Destroy(proxyTarget.gameObject);
    }

    private void EnsureProxyTarget()
    {
        if (proxyTarget != null) return;

        var go = new GameObject($"{name}_EQS");
        go.hideFlags = HideFlags.DontSave;

        proxyTarget = go.transform;
        proxyTarget.position = transform.position;
    }
    private void OnDrawGizmos()
    {
        if (proxyTarget == null || drawEqsPoints == false) return;
        if (self != null && (self.isBound || IsDormant())) return;   // 정지 중엔 EQS gizmo 숨김

        Gizmos.color = (self.intent == CreatureIntent.Flee) ? Color.red : Color.green;
        Gizmos.DrawLine(self.rootTransform.position, proxyTarget.position);
        Gizmos.DrawSphere(proxyTarget.position + Vector3.up * 2, 0.5f);

    }
}
