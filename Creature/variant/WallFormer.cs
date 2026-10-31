using UnityEngine;
using CreatureTypes;

/// <summary>
/// A four-legged creature that occupies and blocks a broken wall opening.
/// It takes over the creature's regular Think behavior while enabled.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Creature))]
[RequireComponent(typeof(Think2))]
public sealed class WallFormer : MonoBehaviour
{
    private struct SavedLegPose
    {
        public Vector3 tipLocalPosition;
        public Vector3 footLocalPosition;
        public bool footTargetLocked;
    }

    [Header("Wall formation")]
    [Tooltip("지정하면 이 벽만 사용합니다. 비우면 현재 방에서 가장 가까운 부서진 벽을 찾습니다.")]
    public BreakableWall targetWall;
    [Tooltip("네 다리를 BreakableWall 모서리 순서에 맞춰 지정합니다. 비우면 하위 Leg 4개를 자동으로 찾습니다.")]
    public Leg[] legs;
    [Tooltip("벽에 이 거리로 도착하면 형태를 만들기 시작합니다.")]
    public float formRange = 2f;
    [Tooltip("방 상태 확인 간격")]
    public float stateCheckInterval = 0.15f;

    [Header("Room response")]
    [Tooltip("켜면 지정한 방 상태일 때만 벽으로 이동/형성합니다. 끄면 L/A/None 모두에서 작동합니다.")]
    public bool limitByRoomState = false;
    [Tooltip("방 상태 제한을 켰을 때 요구할 상태")]
    public Room.RoomActivation formState = Room.RoomActivation.L;

    [Header("Debug")]
    [Tooltip("벽 이동·형성 과정을 콘솔에 찍음(0.3초 간격)")]
    public bool debugLog = false;
    private float _dbgNext;
    private void Dbg(string msg)
    {
        if (!debugLog) return;
        if (Time.time < _dbgNext) return;
        _dbgNext = Time.time + 0.3f;
        Debug.Log($"[WallFormer:{name}] {msg}", this);
    }

    private Creature self;
    private Think2 think;
    private TargetControl targetControl;
    private Room observedRoom;
    private Room.RoomActivation observedState = (Room.RoomActivation)(-1);
    private float nextStateCheck;
    private bool forming;
    private bool formed;
    private bool playerOverride;
    private BreakableWall formingWall;
    private Room formationRoom;
    private LegHead legHead;

    private Transform savedParent;
    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private Vector3 savedLocalScale;
    private SavedLegPose[] savedLegPoses;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
        targetControl = GetComponent<TargetControl>();

        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>(true);

        legHead = GetComponentInChildren<LegHead>(true);   // 형성 중엔 꺼서 발 앵커 충돌 방지

        // 벽 형태는 발 앵커가 만들므로 예전 Kabsch rig는 더 이상 구동하지 않는다.
        Kabsch[] oldRigs = GetComponentsInChildren<Kabsch>(true);
        foreach (Kabsch rig in oldRigs)
            if (rig != null) rig.enabled = false;

        SetThinkManual();
    }

    private void Update()
    {
        if (self == null || self.IsDead) return;

        // Possess wins over the automatic room response. Re-evaluate after release.
        if (self.IsControlled)
        {
            if (!playerOverride)
            {
                playerOverride = true;
                // 조종 시작: 형성 전 위치로 되돌리지 않고(순간이동 방지) 현재(block) 위치에서 따라오게.
                // 발 잠금만 풀어 바로 걸을 수 있게 한다.
                CancelAutomaticForm(restoreHome: false);
                UnlockFeetKeepPosition();
            }
            return;
        }
        if (playerOverride)
        {
            playerOverride = false;
            observedRoom = null;
            observedState = (Room.RoomActivation)(-1);
            SetThinkManual();
        }

        if (formed)
        {
            bool stateNoLongerAllowsFormation = limitByRoomState &&
                (formationRoom == null || formationRoom.Activation != formState);
            if (formingWall == null || !formingWall.Broken || stateNoLongerAllowsFormation)
                CancelAutomaticForm(restoreHome: true);
            else
                return;
        }

        if (Time.time < nextStateCheck) return;
        nextStateCheck = Time.time + Mathf.Max(0.05f, stateCheckInterval);

        Room room = self.currentRoom;
        Room.RoomActivation state = room != null ? room.Activation : Room.RoomActivation.None;
        // 방/상태가 바뀌었거나, 유휴(배회) 중이면 매 체크마다 부서진 벽을 다시 탐색한다.
        // → 상태 변화 없이 벽이 새로 부서져도 바로 가서 막음.
        if (room != observedRoom || state != observedState || (!forming && !formed))
        {
            observedRoom = room;
            observedState = state;
            ApplyRoomState(room, state);
        }

        if (forming && !formed && formingWall != null)
        {
            if (!formingWall.Broken)
            {
                // The opening was repaired while the creature was on its way.
                CancelAutomaticForm(restoreHome: true);
                return;
            }
            // 가장 가까운 모서리로 등반 목표를 갱신 → 그 모서리로 올라가고, reach 안에 들면 형성.
            if (NearestCorner(formingWall, out Vector3 cornerPos, out float dCorner))
            {
                if (think != null) think.MoveProxy(cornerPos);
                if (targetControl != null && think != null) targetControl.SetMovementTarget(think.ProxyTarget);

                float reach = formRange + (legHead != null ? legHead.wallStandoff : 0f);
                Dbg($"이동중 → wall={formingWall.name} 최근접 모서리거리={dCorner:0.00}(reach={reach:0.0}) corner={cornerPos} self={SelfPosition()}");
                if (dCorner <= reach)
                {
                    Dbg($"형성 시작 (모서리거리 {dCorner:0.00} ≤ {reach:0.0})");
                    EnterWallFormation(formingWall);
                }
            }
        }
    }

    private void ApplyRoomState(Room room, Room.RoomActivation state)
    {
        if (limitByRoomState && state != formState)
        {
            CancelAutomaticForm(restoreHome: true);
            return;
        }

        BreakableWall wall = FindTargetWall(room);
        if (wall == null)
        {
            Dbg($"방={(room != null ? room.roomID : "null")} 부서진 벽 없음 → 형성 대상 없음");
            CancelAutomaticForm(restoreHome: true);
            return;
        }

        if (formed && formingWall == wall) return;
        if (forming && formingWall == wall) return;

        CancelAutomaticForm(restoreHome: true);
        forming = true;
        formingWall = wall;
        formationRoom = room;
        Dbg($"대상 벽 지정 {wall.name} @ mount={MountPoint(wall)} → proxy 이동 시작");
        SetThinkManual(MountPoint(wall));
    }

    private BreakableWall FindTargetWall(Room room)
    {
        if (targetWall != null)
            return targetWall.Broken ? targetWall : null;
        if (room == null || room.brokenPassages == null) return null;

        BreakableWall best = null;
        float bestDistance = float.MaxValue;
        Vector3 p = SelfPosition();
        foreach (BreakableWall wall in room.brokenPassages)
        {
            if (wall == null || !wall.Broken || wall.breakableObject == null) continue;
            float d = (wall.Position - p).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = wall; }
        }
        return best;
    }

    private void EnterWallFormation(BreakableWall wall)
    {
        if (wall == null || !wall.Broken || wall.breakableObject == null) return;
        if (!ValidateFootAnchors(wall, out Transform[] corners))
        {
            // 설정 오류를 매 프레임 반복하지 않고 자동 이동 상태를 정리한다.
            CancelAutomaticForm(restoreHome: false);
            return;
        }

        SaveReturnPose();
        SaveLegPoses();
        Transform body = transform;
        // 예전엔 breakableObject의 자식으로 넣어 (0,0,0)에 뒀으나, 벽의 비균등 스케일이
        // H·다리에 상속돼 뒤틀렸다. → 부모를 바꾸지 않고 월드 좌표로만 벽 중심에 맞춤(스케일 상속 없음).
        body.position = wall.breakableObject.transform.position;   // 월드 중심
        body.rotation = wall.breakableObject.transform.rotation;   // 벽과 같은 방향(기존 local-identity와 동일한 외형)

        // 다리는 LegHead를 향하므로, LegHead를 벽 중심에 둬야 몸이 중심에 모인다 (끄면 EQS 추종이 멈추니 직접 배치)
        if (legHead != null) legHead.transform.position = wall.breakableObject.transform.position;

        // 각 모서리에 '가장 가까운 다리'를 하나씩 1:1 배정 (다리 꼬임 방지)
        bool[] used = new bool[legs.Length];
        for (int c = 0; c < 4; c++)
        {
            int best = -1; float bestSqr = float.MaxValue;
            for (int l = 0; l < legs.Length; l++)
            {
                if (used[l] || legs[l] == null || legs[l].foot == null) continue;
                float sqr = (legs[l].foot.position - corners[c].position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = l; }
            }
            if (best >= 0) { used[best] = true; legs[best].SetFootAnchor(corners[c].position); }
        }

        formingWall = wall;
        forming = false;
        formed = true;
        if (legHead != null) legHead.enabled = false;   // 형성됨 → LegHead 정지(발은 모서리 고정 유지)
        formingWall.SetPassageBlocked(true);
        if (self != null) self.intent = CreatureIntent.Block;   // 통로 차단 중 상태
        // EQS(proxy)를 breakable wall 중심(breakableObject (0,0,0))에 고정
        if (think != null) think.MoveProxy(wall.breakableObject.transform.position);
        if (targetControl != null && think != null) targetControl.SetMovementTarget(think.ProxyTarget);
        Dbg($"형성 완료 ✓ wall={wall.name} — 몸을 벽에 부착, 발 4개 모서리 고정, 통로 차단");
    }

    private bool ValidateFootAnchors(BreakableWall wall, out Transform[] corners)
    {
        corners = null;
        if (wall.breakableObject == null || !wall.breakableObject.activeInHierarchy)
        {
            Debug.LogError($"[WallFormer] {name}: breakableObject가 비어있거나 비활성입니다.", this);
            return false;
        }

        if (wall.GetWallFormerBlockerCollider() == null)
        {
            Debug.LogError($"[WallFormer] {name}: BreakableWall에 통로 차단용 BoxCollider를 지정하세요.", this);
            return false;
        }

        if (!wall.TryGetWallFormerCorners(out corners))
        {
            Debug.LogError($"[WallFormer] {name}: BreakableWall에 네 모서리 Transform을 지정하세요.", this);
            return false;
        }

        if (legs == null || legs.Length != 4)
        {
            Debug.LogError($"[WallFormer] {name}: 모서리 고정에는 Leg 4개가 필요합니다.", this);
            return false;
        }

        for (int i = 0; i < 4; i++)
        {
            if (legs[i] == null || legs[i].tipTarget == null || legs[i].foot == null)
            {
                Debug.LogError($"[WallFormer] {name}: Leg {i}의 foot과 tipTarget을 지정하세요.", this);
                return false;
            }
            if (!corners[i].IsChildOf(wall.breakableObject.transform))
            {
                Debug.LogError($"[WallFormer] {name}: 모서리점은 Breakable Object의 자식이어야 합니다.", this);
                return false;
            }
        }
        return true;
    }

    private void CancelAutomaticForm(bool restoreHome)
    {
        if (formed)
        {
            BreakableWall wallToRelease = formingWall;
            if (self != null && self.intent == CreatureIntent.Block) self.intent = CreatureIntent.Wander;   // 차단 해제
            if (legHead != null) legHead.enabled = true;   // 형성 해제 → LegHead 재가동
            if (restoreHome)
            {
                RestoreReturnPose();
                RestoreLegPoses();
            }
            if (wallToRelease != null) wallToRelease.SetPassageBlocked(false);
        }
        formed = false;
        forming = false;
        formingWall = null;
        formationRoom = null;

        if (!playerOverride)
        {
            WanderFreely();   // 수복할 벽 없음 → 제자리 고정 대신 Think2 기본 배회
        }
    }

    // 수복할 부서진 벽이 없을 때: 위치 고정(SetThinkManual) 대신 Think2의 기본 배회에 맡긴다.
    private void WanderFreely()
    {
        if (think == null) return;
        think.manualControl = false;   // 수동 제어 해제 → Think2 EQS 배회로 복귀
        think.dormant = false;
        think.LockState(false);
    }

    private void SaveLegPoses()
    {
        if (legs == null) return;
        savedLegPoses = new SavedLegPose[legs.Length];
        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];
            if (leg == null) continue;
            savedLegPoses[i] = new SavedLegPose
            {
                tipLocalPosition = leg.tipTarget.localPosition,
                footLocalPosition = leg.foot.localPosition,
                footTargetLocked = leg.FootTargetLocked
            };
        }
    }

    // 몸 위치는 그대로 두고 발 고정만 해제 (조종 시작 시 현재 위치에서 자연스럽게 걷게)
    private void UnlockFeetKeepPosition()
    {
        if (legs == null) return;
        foreach (var leg in legs)
            if (leg != null) leg.SetFootTargetLock(false);
        savedLegPoses = null;
    }

    private void RestoreLegPoses()
    {
        if (legs == null || savedLegPoses == null) return;
        int count = Mathf.Min(legs.Length, savedLegPoses.Length);
        for (int i = 0; i < count; i++)
        {
            Leg leg = legs[i];
            if (leg == null) continue;
            leg.tipTarget.localPosition = savedLegPoses[i].tipLocalPosition;
            leg.foot.localPosition = savedLegPoses[i].footLocalPosition;
            leg.SetFootTargetLock(false);   // 복귀 시엔 발 잠금 해제 → 걷기 재개 (등반 중 잠긴 상태가 남지 않게)
        }
        savedLegPoses = null;
    }

    private void SaveReturnPose()
    {
        savedParent = transform.parent;
        savedLocalPosition = transform.localPosition;
        savedLocalRotation = transform.localRotation;
        savedLocalScale = transform.localScale;
    }

    private void RestoreReturnPose()
    {
        transform.SetParent(savedParent, false);
        transform.localPosition = savedLocalPosition;
        transform.localRotation = savedLocalRotation;
        transform.localScale = savedLocalScale;
    }

    private void SetThinkManual()
    {
        SetThinkManual(SelfPosition());
    }

    private void SetThinkManual(Vector3 targetPosition)
    {
        if (think == null) return;
        think.manualControl = true;
        think.dormant = false;
        think.LockState(false);
        think.currentTarget = new ThinkTarget { creature = null, point = targetPosition };
        think.MoveProxy(targetPosition);
        if (think.ProxyTarget != null)
        {
            if (targetControl != null) targetControl.SetMovementTarget(think.ProxyTarget);
        }
    }

    private Vector3 SelfPosition()
    {
        return self != null && self.rootTransform != null ? self.rootTransform.position : transform.position;
    }

    // 실제로 벽을 타고 올라가는 몸 = LegHead. 형성 거리 판정은 이 위치로 재야 등반에 맞춰 줄어든다.
    private Vector3 BodyPosition()
    {
        return legHead != null ? legHead.transform.position : SelfPosition();
    }

    // H가 실제로 붙는 지점 = breakableObject 중심. (없으면 벽 Transform 원점 폴백) — 등반(proxy) 목표용.
    private Vector3 MountPoint(BreakableWall wall)
    {
        if (wall == null) return SelfPosition();
        return wall.breakableObject != null ? wall.breakableObject.transform.position : wall.Position;
    }

    // 네 모서리 중 H와 가장 가까운 것의 위치·거리 (등반 목표 + 형성 발동 판정용)
    private bool NearestCorner(BreakableWall wall, out Vector3 pos, out float dist)
    {
        pos = default; dist = float.MaxValue;
        if (wall == null || !wall.TryGetWallFormerCorners(out Transform[] corners)) return false;
        Vector3 p = BodyPosition();   // 등반하는 LegHead 기준
        float bestSqr = float.MaxValue; Vector3 bestPos = default;
        for (int i = 0; i < corners.Length; i++)
        {
            if (corners[i] == null) continue;
            float sqr = (corners[i].position - p).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; bestPos = corners[i].position; }
        }
        if (bestSqr == float.MaxValue) return false;
        pos = bestPos; dist = Mathf.Sqrt(bestSqr);
        return true;
    }

}
