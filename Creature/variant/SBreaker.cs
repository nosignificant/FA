using System.Collections;
using UnityEngine;
using CreatureTypes;

// S: 방이 A활성이면 깨어나 방 안의 BreakableWall을 찾아가 부순다. 아니면 휴면(정지).
//   - 벽은 생물이 아니라 Think의 chase 대상이 아님 → TargetControl을 직접 벽으로 향하게 이동.
//   - 벽에 닿으면 hitInterval마다 Hit(). 부술 벽 없으면 배회.
//   - 도구 생물 → 방 밖으로 이주하지 않음(canMigrate=false).
[RequireComponent(typeof(Creature))]
public class SBreaker : MonoBehaviour
{
    [Tooltip("이 거리 안이면 벽에 닿은 것으로 보고 타격. S 몸통이 벽 앞에서 멈추는 거리보다 커야 함")]
    public float breakRange = 5f;
    [Tooltip("벽에 닿아있을 때 타격 간격(초)")]
    public float hitInterval = 1f;
    public float checkInterval = 0.2f;

    private Creature self;
    private Think2 think;
    private TargetControl tc;
    private float nextHitTime;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
        tc = GetComponent<TargetControl>();
        self.canMigrate = false;   // 도구 생물 — 방에 묶임
    }

    private void Start() => StartCoroutine(Loop());

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (self != null && !self.IsDead)
        {
            var room = self.currentRoom;
            if (!_activated && room != null && AwakeInRoom(room))
            {
                _activated = true;   // 한번 켜지면 영구 유지
            }
            // 최초 각성은 유지하되, 생물 데이터에 지정된 방 상태(A 등)가 아니면 다시 휴면한다.
            bool awake = IsCurrentlyAwake(room);

            if (!awake)
            {
                SetMode(Mode.Frozen);          // 휴면 정지
            }
            else
            {
                BreakableWall w = NearestBreakableInRoom();
                if (w == null)
                {
                    SetMode(Mode.Wander);      // 부술 벽 없음 → 배회(Think가 몰기)
                }
                else if (w.DistanceFrom(SelfPos()) <= breakRange)
                {
                    SetMode(Mode.Breaking);    // 닿음 → 활성 상태로 제자리서 타격
                    if (Time.time >= nextHitTime)
                    {
                        w.Hit();
                        nextHitTime = Time.time + hitInterval;
                    }
                }
                else
                {
                    SetMode(Mode.GotoWall);    // 벽으로 직진 (Think 손 떼고 벽만 조준)
                    if (tc != null) tc.SetMovementTarget(w.transform);
                }
            }

            yield return wait;
        }
    }

    private enum Mode { Frozen, GotoWall, Breaking, Wander }
    private Mode _mode = (Mode)(-1);
    private bool _activated;   // 한번 각성하면 영구 유지

    // 각성 조건: CreatureData의 방상태 게이트 사용 (하드코딩 대신). 게이트 없으면 항상 각성.
    private bool AwakeInRoom(Room room)
    {
        if (self.data != null && self.data.gateByRoomState)
            return room.Activation == self.data.requiredRoomState;
        return true;
    }

    private bool IsCurrentlyAwake(Room room) => _activated && room != null && AwakeInRoom(room);

    // Frozen: dormant 정지 / GotoWall: 벽으로 직진 / Breaking: 활성 상태로 제자리 타격 / Wander: Think가 EQS로 몲
    private void SetMode(Mode m)
    {
        bool changed = m != _mode;
        _mode = m;
        if (think != null)
        {
            think.dormant       = (m == Mode.Frozen);
            think.manualControl = (m == Mode.GotoWall || m == Mode.Breaking);
        }

        if (m == Mode.Frozen)
        {
            // 이전에 조준하던 벽 목표를 즉시 취소한다. Think의 다음 업데이트를 기다리면
            // 휴면 전 목표를 잠깐 더 따라가거나 proxy가 낡은 위치에 남을 수 있다.
            if (think != null) think.MoveProxy(SelfPos());
            if (tc != null && think != null && tc.movementTarget != think.ProxyTarget)
                tc.SetMovementTarget(think.ProxyTarget);
            return;
        }

        if (!changed) return;

        if (m == Mode.Breaking && think != null)
        {
            // Think는 잠시 양보하되 dormant로 만들지 않고, 현재 위치를 목표로 고정한다.
            think.MoveProxy(SelfPos());
            if (tc != null) tc.SetMovementTarget(think.ProxyTarget);
        }
        else if (m != Mode.GotoWall)
        {
            FollowProxy(); // Frozen은 정지, Wander는 EQS proxy 복귀
        }
    }

    private void FollowProxy()
    {
        if (tc != null && think != null) tc.SetMovementTarget(think.ProxyTarget);
    }

    private BreakableWall NearestBreakableInRoom()
    {
        var room = self.currentRoom;
        if (room == null) return null;
        BreakableWall best = null;
        float bd = float.MaxValue;
        for (int i = 0; i < room.breakableWalls.Count; i++)
        {
            var w = room.breakableWalls[i];
            if (w == null || w.Broken) continue;
            float d = w.DistanceFrom(SelfPos());
            if (d < bd) { bd = d; best = w; }
        }
        return best;
    }

    private Vector3 SelfPos() => self.rootTransform != null ? self.rootTransform.position : transform.position;

    // HUD용
    public string HudStatus()
    {
        var room = self.currentRoom;
        // Loop와 동일한 게이트를 매번 확인한다. 한 번 각성했어도 방 상태가 바뀌면
        // HUD에서 wander/벽 목표를 표시하지 않고 바로 dormant로 보인다.
        if (!IsCurrentlyAwake(room)) return "dormant";
        BreakableWall w = NearestBreakableInRoom();
        if (w == null) return "wander";
        return w.DistanceFrom(SelfPos()) <= breakRange ? "breaking" : "goto";
    }
}
