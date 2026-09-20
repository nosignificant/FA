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
            if (!_activated && room != null && AwakeInRoom(room)) _activated = true;   // 한번 켜지면 영구 유지
            bool awake = _activated;

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
                    SetMode(Mode.Frozen);      // 닿음 → 제자리서 타격
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

    private enum Mode { Frozen, GotoWall, Wander }
    private Mode _mode = (Mode)(-1);
    private bool _activated;   // 한번 각성하면 영구 유지

    // 각성 조건: CreatureData의 방상태 게이트 사용 (하드코딩 대신). 게이트 없으면 항상 각성.
    private bool AwakeInRoom(Room room)
    {
        if (self.data != null && self.data.gateByRoomState)
            return room.Activation == self.data.requiredRoomState;
        return true;
    }

    // Frozen: dormant(제자리 정지) / GotoWall: manualControl(Think 손 뗌, 벽으로 직진) / Wander: Think가 EQS로 몲
    private void SetMode(Mode m)
    {
        if (m == _mode) { if (m == Mode.Frozen) FollowProxy(); return; }
        _mode = m;
        if (think != null)
        {
            think.dormant       = (m == Mode.Frozen);
            think.manualControl = (m == Mode.GotoWall);
        }
        if (m == Mode.Frozen) FollowProxy();   // 얼린 proxy(=자기 위치)를 따라 → 정지
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
        if (!_activated && (room == null || !AwakeInRoom(room))) return "dormant";
        BreakableWall w = NearestBreakableInRoom();
        if (w == null) return "wander";
        return w.DistanceFrom(SelfPos()) <= breakRange ? "breaking" : "goto";
    }
}
