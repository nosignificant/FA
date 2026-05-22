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
    public float migrationRange = 2f;
    protected Transform proxyTarget;


    private void SyncRoomFromPosition() { }

    protected virtual void Awake()
    {
        if (scanner == null) scanner = GetComponent<CreatureScanner>();
        if (self == null) self = GetComponent<Creature>();
        if (targetControl == null) targetControl = GetComponent<TargetControl>();

        initStates();
        EnsureProxyTarget();

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

    private IEnumerator ThinkLoop()
    {
        // 생물마다 시작 타이밍 분산 → 같은 프레임에 몰리는 부하 스파이크 방지
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, waitInterval));

        while (true)
        {
            if (self != null) SyncRoomFromPosition();
            LetsThink();
            yield return new WaitForSeconds(waitInterval);
        }
    }

    private void Update()
    {
        if (currentLockTime >= lockTime) LockThink(false);
    }

    public void LockThink(bool setLock)
    {
        if (setLock == false) currentLockTime = 0f;
        isLocked = setLock;
    }

    private void LetsThink()
    {
        if (self.intent == CreatureIntent.Grabbed) return;
        detected = scanner.Results;
        var newIntent = DetermineIntent();
        var newState = GetThinkState(newIntent);

        if (newState != currentState)
        {
            ThinkTarget carry = currentState?.Exit() ?? default;
            newState.Enter(carry);
            currentState = newState;
            self.intent = newIntent;
        }

        currentState.Refresh(BuildQueryPoints());
        currentTarget = currentState.newTarget; // ← Refresh 후에 업데이트
        MoveProxy(currentTarget.point, 10f);


        if (targetControl != null)
            targetControl.SetMovementTarget(proxyTarget);
    }

    private void UpdateCurrentTarget()
    {
        if (currentState != null)
            currentTarget = currentState.newTarget;
    }

    protected virtual CreatureIntent DetermineIntent()
    {
        // 회피 대상(avoidCreatureIDs)이 같은 방에 있으면 하던 일 멈추고 이주(Wander→GetMigrateChance=1)
        if (self != null && self.AvoidedKindInRoom()) return CreatureIntent.Wander;

        if (DoesNeedToFlee()) return CreatureIntent.Flee;
        else if (DoesNeedToChase()) return CreatureIntent.Chase;
        return CreatureIntent.Wander;
    }
    protected virtual bool DoesNeedToFlee()
    {
        if (detected == null) return false;
        if (self.intent == CreatureIntent.Flee && isLocked) return true;
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
        if (self.intent == CreatureIntent.Chase && isLocked) return true;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

    public bool IsValidTarget(Creature target)
    {
        return target != null && target != self && !target.IsDead && target.data != null;
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



    private Door FindOpenDoorTo(Room targetRoom)
    {
        if (self.currentRoom == null || targetRoom == null) return null;

        Door bestDoor = null;
        float bestDist = float.MaxValue;
        foreach (var d in self.currentRoom.doors)
        {
            //내 방의 문이 targetRoom으로 이어져야함
            if (d == null || !d.isOpen) continue;
            if (d.GetOtherRoom(self.currentRoom) != targetRoom) continue;

            float dist = Vector3.Distance(self.rootTransform.position, d.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDoor = d;
            }
        }
        return bestDoor;
    }
    protected virtual bool ShouldAvoidRoom(Room room) => false;

    // 이주 목표(문) 위치 — WanderState가 이걸 EQS 대신 추적함
    [System.NonSerialized] public bool hasMigrateTarget = false;
    [System.NonSerialized] public Vector3 migrateTargetPoint;
    [Tooltip("이주 시 문 너머 반대쪽 방 안쪽으로 얼마나 들어간 지점을 목표로 할지")]
    public float migrateThroughDepth = 5f;
    [Tooltip("문에 이 거리 이내로 붙으면 반대쪽 방으로 전환 + 방 교체 (생물 몸집 고려해 너무 작게 두면 영영 도달 못함)")]
    public float migrateDoorReachDist = 2f;

    public bool TryMigrateRoom()
    {
        hasMigrateTarget = false;
        if (self.currentRoom == null) return false;

        Door bestDoor = null;
        float bestDist = float.MaxValue;

        foreach (var d in self.currentRoom.doors)
        {
            if (!d.isOpen) continue;
            Room other = d.GetOtherRoom(self.currentRoom);
            if (other == null) continue;
            if (ShouldAvoidRoom(other)) continue;

            Vector3 dp = (d.self != null && d.self.rootTransform != null)
                ? d.self.rootTransform.position : d.transform.position;
            float dist = Vector3.Distance(self.rootTransform.position, dp);
            if (dist < bestDist) { bestDist = dist; bestDoor = d; }
        }

        if (bestDoor == null)
        {
            // 지금 갈 문이 없을 뿐 — 의향은 MigrateLoop이 관리. 여기서 끄지 않음
            hasMigrateTarget = false;
            return false;
        }

        Room nextR = bestDoor.GetOtherRoom(self.currentRoom);
        Vector3 doorPos = (bestDoor.self != null && bestDoor.self.rootTransform != null)
            ? bestDoor.self.rootTransform.position
            : bestDoor.transform.position;

        hasMigrateTarget = true;

        if (bestDist > migrateDoorReachDist)
        {
            // 1단계: 아직 문에서 멀다 → 문 자체를 목표
            migrateTargetPoint = doorPos;
        }
        else
        {
            // 2단계: 문 똑바로 관통 → 두 방 중심을 잇는 축 방향으로
            Vector3 intoNext = (nextR.transform.position - self.currentRoom.transform.position);
            intoNext.y = 0f;
            intoNext = intoNext.sqrMagnitude > 0.001f ? intoNext.normalized : Vector3.zero;
            migrateTargetPoint = doorPos + intoNext * migrateThroughDepth;

            self.currentRoom.UnregisterCreature(self);
            nextR.RegisterCreature(self);
            self.transform.SetParent(nextR.transform, true);   // 새 방 자식으로
            self.lastMigrateTime = Time.time;                   // 재이동 쿨타임 시작
            Debug.Log("move to room: " + nextR);
            self.wantToMigrate = false;
            hasMigrateTarget = false;
        }

        return true;
    }
    /////////////
    //build proxy codes
    ////////////
    /// 
    private List<Vector3> BuildQueryPoints()
    {
        List<Vector3> points = new();
        float r = Mathf.Max(0.1f, scanner.scanRadius * 1.5f);
        float s = Mathf.Max(0.5f, pointSpacing);
        Vector3 center = self.rootTransform.position;
        for (int i = 0; i < maxPoints; i++)
        {
            Vector3 p = center + Random.insideUnitSphere * r;
            if (!IsInsideBounds(p)) continue;
            points.Add(p);
        }
        return points;
    }

    private bool IsInsideBounds(Vector3 p, float shrink = 0f)
    {
        if (self.currentRoom == null) return false;
        Bounds b = self.currentRoom.homeBound.bounds;

        // bounds를 shrink만큼 줄이기
        Vector3 min = b.min + Vector3.one * shrink;
        Vector3 max = b.max - Vector3.one * shrink;

        return p.x > min.x && p.x < max.x &&
               p.y > min.y && p.y < max.y &&
               p.z > min.z && p.z < max.z;
    }
    public void MoveProxy(Vector3 p, float speed)
    {
        // ThinkLoop는 waitInterval마다 호출됨. deltaTime이 아니라 waitInterval 기준으로 보간
        float t = 1f - Mathf.Exp(-speed * waitInterval);
        proxyTarget.position = Vector3.Lerp(proxyTarget.position, p, t);
    }
    private void OnDestroy()
    {
        // 독립 루트 오브젝트라 생물 죽어도 안 지워짐 → 직접 정리
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

        Gizmos.color = (self.intent == CreatureIntent.Flee) ? Color.red : Color.green;
        Gizmos.DrawLine(self.rootTransform.position, proxyTarget.position);
        Gizmos.DrawSphere(proxyTarget.position + Vector3.up * 2, 0.5f);

    }
}
