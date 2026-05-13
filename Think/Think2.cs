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

public abstract class Think2 : MonoBehaviour
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
    public int maxPoints = 80;

    [Header("Debug")]
    public bool drawEqsPoints = true;
    private float proxyMoveSpeed = 1f;
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
        MoveProxy(currentTarget.point, 1f);


        if (targetControl != null)
            targetControl.SetMovementTarget(currentTarget.creature.rootTransform);
    }

    private void UpdateCurrentTarget()
    {
        if (currentState != null)
            currentTarget = currentState.newTarget;
    }

    protected abstract CreatureIntent DetermineIntent();
    protected abstract bool DoesNeedToFlee();
    protected abstract bool DoesNeedToChase();


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
        wanderState = new WanderState(this);
    }



    private Door FindOpenDoorTo(Room targetRoom)
    {
        if (self.currentRoom == null || targetRoom == null) return null;

        Door bestDoor = null;
        float bestDist = float.MaxValue;
        foreach (var d in self.currentRoom.doors)
        {
            //내 방의 문이 targetRoom으로 이어져야함 내가 있는 방에 nextRoom을 설정해놔야함 
            if (d == null || !d.isOpen || d.nextRoom != targetRoom) continue;

            float dist = Vector3.Distance(self.rootTransform.position, d.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDoor = d;
            }
        }
        return bestDoor;
    }


    private bool TryMigrateRoom()
    {
        if (self.currentRoom == null) return false;

        Door bestDoor = null;
        float bestDist = float.MaxValue;

        foreach (var d in self.currentRoom.doors)
        {
            if (!d.isOpen || d.nextRoom == null) continue;
            float dist = Vector3.Distance(self.transform.position, d.transform.position);
            if (dist < bestDist) { bestDist = dist; bestDoor = d; }
        }

        if (bestDoor == null) return false;

        // 열린 문 방향으로 유도
        MoveProxy(bestDoor.transform.position, 2f);

        // 문에 충분히 가까우면 방 교체
        if (bestDist <= migrationRange)
        {
            self.currentRoom.UnregisterCreature(self);
            bestDoor.nextRoom.RegisterCreature(self);
            Debug.Log("move to room: " + bestDoor.nextRoom);
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

    private bool IsInsideBounds(Vector3 p, float shrink = 1f)
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
        proxyTarget.position = Vector3.Lerp(
                                proxyTarget.position,
                                p,
                                1f - Mathf.Exp(-proxyMoveSpeed * Time.deltaTime));
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
