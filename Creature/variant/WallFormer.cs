using System;
using UnityEngine;
using CreatureTypes;

/// <summary>
/// A Kabsch swarm that can occupy a broken wall opening.
/// L rooms assemble it into the opening; A rooms release it back into its
/// saved roaming formation. Player control temporarily overrides automation.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Creature))]
[RequireComponent(typeof(Think2))]
public sealed class WallFormer : MonoBehaviour
{
    [Serializable]
    public sealed class KabschWallRig
    {
        public Kabsch kabsch;
        [Tooltip("BreakableWall.breakableObject 아래에 둔 벽 형태 참조점. Kabsch.refChild와 개수가 같아야 합니다.")]
        public Transform[] wallFormationPoints;
    }

    [Header("Wall formation")]
    [Tooltip("지정하면 이 벽만 사용합니다. 비우면 현재 방에서 가장 가까운 부서진 벽을 찾습니다.")]
    public BreakableWall targetWall;
    [Tooltip("Kabsch별 벽 형태 참조점. 각 Kabsch의 원래 refChild와 일대일 대응합니다.")]
    public KabschWallRig[] kabschRigs;
    [Tooltip("벽 형태일 때 켤 전용 BoxCollider. 락온용 Creature.mainCollider와 분리하세요.")]
    public BoxCollider wallCollider;
    [Tooltip("벽에 도착한 뒤 형태를 만들기 시작할 거리")]
    public float formRange = 2f;
    [Tooltip("벽 형태가 된 뒤 벽 콜라이더를 켜기 전 대기 시간")]
    public float colliderDelay = 0.5f;
    [Tooltip("방 상태 확인 간격")]
    public float stateCheckInterval = 0.15f;

    [Header("Room response")]
    [Tooltip("이 상태에서만 부서진 벽을 막습니다. 기본 L, A/None에서는 흩어집니다.")]
    public Room.RoomActivation formState = Room.RoomActivation.L;

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
    private Coroutine colliderRoutine;

    private Transform savedParent;
    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private Vector3 savedLocalScale;
    private Transform[][] originalRefChildren;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
        targetControl = GetComponent<TargetControl>();
        if (wallCollider != null) wallCollider.enabled = false;

        if (kabschRigs == null) return;
        originalRefChildren = new Transform[kabschRigs.Length][];
        for (int i = 0; i < kabschRigs.Length; i++)
        {
            var rig = kabschRigs[i];
            if (rig == null || rig.kabsch == null) continue;
            originalRefChildren[i] = rig.kabsch.refChild != null
                ? (Transform[])rig.kabsch.refChild.Clone()
                : Array.Empty<Transform>();
            if (rig.wallFormationPoints != null && rig.wallFormationPoints.Length != originalRefChildren[i].Length)
                Debug.LogError($"[WallFormer] {name}: Kabsch {rig.kabsch.name}의 벽 참조점 수가 원래 refChild 수와 다릅니다.", this);
        }
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
                CancelAutomaticForm(restoreHome: true);
            }
            return;
        }
        if (playerOverride)
        {
            playerOverride = false;
            observedRoom = null;
            observedState = (Room.RoomActivation)(-1);
        }

        if (Time.time < nextStateCheck) return;
        nextStateCheck = Time.time + Mathf.Max(0.05f, stateCheckInterval);

        Room room = self.currentRoom;
        Room.RoomActivation state = room != null ? room.Activation : Room.RoomActivation.None;
        if (room != observedRoom || state != observedState)
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
            if (Vector3.Distance(SelfPosition(), formingWall.Position) <= formRange)
                EnterWallFormation(formingWall);
        }
    }

    private void ApplyRoomState(Room room, Room.RoomActivation state)
    {
        if (state != formState)
        {
            CancelAutomaticForm(restoreHome: true);
            return;
        }

        BreakableWall wall = FindTargetWall(room);
        if (wall == null)
        {
            CancelAutomaticForm(restoreHome: true);
            return;
        }

        if (formed && formingWall == wall) return;
        if (forming && formingWall == wall) return;

        CancelAutomaticForm(restoreHome: true);
        forming = true;
        formingWall = wall;
        SetThinkManual(true);
        if (targetControl != null) targetControl.SetMovementTarget(wall.transform);
        else if (think != null) think.MoveProxy(wall.transform.position);
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
        if (!ValidateRig(wall)) return;

        SaveReturnPose();
        Transform body = transform;
        body.SetParent(wall.breakableObject.transform, false);
        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;

        for (int i = 0; i < kabschRigs.Length; i++)
        {
            var rig = kabschRigs[i];
            if (rig == null || rig.kabsch == null) continue;
            rig.kabsch.refChild = (Transform[])rig.wallFormationPoints.Clone();
        }

        formingWall = wall;
        forming = false;
        formed = true;
        if (think != null) think.MoveProxy(SelfPosition());
        if (targetControl != null && think != null) targetControl.SetMovementTarget(think.ProxyTarget);

        if (colliderRoutine != null) StopCoroutine(colliderRoutine);
        colliderRoutine = StartCoroutine(EnableWallColliderAfterDelay());
    }

    private System.Collections.IEnumerator EnableWallColliderAfterDelay()
    {
        if (colliderDelay > 0f) yield return new WaitForSeconds(colliderDelay);
        if (formed && !playerOverride && wallCollider != null) wallCollider.enabled = true;
        colliderRoutine = null;
    }

    private bool ValidateRig(BreakableWall wall)
    {
        if (kabschRigs == null || kabschRigs.Length == 0)
        {
            Debug.LogError($"[WallFormer] {name}: KabschWallRig이 지정되지 않았습니다.", this);
            return false;
        }

        for (int i = 0; i < kabschRigs.Length; i++)
        {
            var rig = kabschRigs[i];
            if (rig == null || rig.kabsch == null || rig.wallFormationPoints == null ||
                rig.kabsch.inChild == null || rig.kabsch.refChild == null ||
                rig.wallFormationPoints.Length != rig.kabsch.inChild.Length ||
                rig.wallFormationPoints.Length != rig.kabsch.refChild.Length)
            {
                Debug.LogError($"[WallFormer] {name}: Kabsch 벽 형태 참조점 설정을 확인하세요.", this);
                return false;
            }
            foreach (Transform point in rig.wallFormationPoints)
            {
                if (point == null || !point.IsChildOf(wall.breakableObject.transform))
                {
                    Debug.LogError($"[WallFormer] {name}: 벽 형태 참조점은 대상 Breakable Object의 자식이어야 합니다.", this);
                    return false;
                }
            }
        }
        return true;
    }

    private void CancelAutomaticForm(bool restoreHome)
    {
        if (colliderRoutine != null)
        {
            StopCoroutine(colliderRoutine);
            colliderRoutine = null;
        }
        if (wallCollider != null) wallCollider.enabled = false;

        if (formed && restoreHome)
        {
            RestoreOriginalKabschRefs();
            RestoreReturnPose();
        }
        formed = false;
        forming = false;
        formingWall = null;

        if (!playerOverride)
        {
            SetThinkManual(false);
            if (think != null && targetControl != null)
                targetControl.SetMovementTarget(think.ProxyTarget);
        }
    }

    private void RestoreOriginalKabschRefs()
    {
        if (kabschRigs == null || originalRefChildren == null) return;
        for (int i = 0; i < kabschRigs.Length; i++)
        {
            var rig = kabschRigs[i];
            if (rig == null || rig.kabsch == null || originalRefChildren[i] == null) continue;
            rig.kabsch.refChild = (Transform[])originalRefChildren[i].Clone();
        }
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

    private void SetThinkManual(bool value)
    {
        if (think == null) return;
        think.manualControl = value;
        if (value && think.ProxyTarget != null)
            think.MoveProxy(SelfPosition());
    }

    private Vector3 SelfPosition()
    {
        return self != null && self.rootTransform != null ? self.rootTransform.position : transform.position;
    }

    private void OnDisable()
    {
        if (wallCollider != null) wallCollider.enabled = false;
    }
}
