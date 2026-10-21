using UnityEngine;
using CreatureTypes;

[RequireComponent(typeof(Creature))]
public class RoomMigration : MonoBehaviour
{
    [Header("References")]
    public Creature self;
    public Think2 think;

    [Header("Config")]
    public float migrateCooldown = 8f;

    [Tooltip("문에 이 거리 이내로 붙으면 반대쪽 방으로 전환")]
    public float migrateDoorReachDist = 2f;

    [Tooltip("문 통과 시, 문 opening을 똑바로 지나도록 문 너머로 밀어줄 거리(m). 벽에 박히는 것 방지")]
    public float doorThroughPush = 6f;

    [Header("열린 문으로 배회 이동 (A/H)")]
    [Tooltip("쫓을 대상이 없어도, 열린 문이 있으면 그쪽 방으로 흘러가려는 확률")]
    [Range(0f, 1f)] public float wanderMigrateChance = 0.7f;
    [Tooltip("이동 시도 후 이 시간(초) 안에 못 넘어가면 포기하고 다시 wander")]
    public float migrateTimeout = 5f;

    [System.NonSerialized] public float lastMigrateTime = -999f;
    [System.NonSerialized] public Vector3 migrateTargetPoint; //state쪽에서 사용
    [System.NonSerialized] public bool isMigrating = false;  // 문 통과 중(소속 갱신 전) → KeepCreaturesInBounds 예외
    private Room lastKnownRoom;
    private Room _prevRoom;              // 방금 떠나온 방 (배회 이동 시 되돌아가기 회피)
    private bool wanderDrifting = false; // 열린 문으로 흘러가기로 결정된 상태 (쿨다운당 1회 추첨 유지)
    private float _driftStart;           // wanderDrifting 시작 시각 (타임아웃 포기용)

    public bool MigrateOnCooldown => Time.time - lastMigrateTime < migrateCooldown;

    [Header("디버그")]
    [Tooltip("켜면 이 생물의 이주 판단 과정을 콘솔에 찍음(1초 간격)")]
    public bool debugLog = false;
    private float _dbgNext, _dbgWanderNext;
    private string DbgName => self != null && self.data != null ? self.data.creatureID.ToString() : "?";
    // QuadLegs(LL)는 XZ 평면에서만 이동하므로 문까지의 거리에서도 높이 차를 빼야 한다.
    private bool UsesPlanarMovement => self != null
        && self.GetComponent<TargetControl>() != null
        && self.GetComponent<TargetControl>().moveMode == TargetControl.MoveMode.QuadLeg;
    private float MigrationDistance(Vector3 a, Vector3 b)
    {
        if (UsesPlanarMovement) { a.y = 0f; b.y = 0f; }
        return Vector3.Distance(a, b);
    }

    private void Dbg(string msg)
    {
        if (!debugLog) return;
        if (Time.time < _dbgNext) return;
        _dbgNext = Time.time + 1f;
        Debug.Log($"[Migrate:{DbgName}] {msg}", this);
    }

    // WanderState가 실제로 돌고 있는지 + 게이트 상태 (별도 throttle)
    public void LogWanderEntry(bool canMig)
    {
        if (!debugLog) return;
        if (Time.time < _dbgWanderNext) return;
        _dbgWanderNext = Time.time + 1f;
        Debug.Log($"[Migrate:{DbgName}] WanderState Refresh 중 · canMigrate={canMig} · room={(self != null && self.currentRoom != null ? self.currentRoom.roomID : "null")}", this);
    }

    private void Awake()
    {
        if (self == null) self = GetComponent<Creature>();
        if (think == null) think = GetComponent<Think2>();
    }

    private void Update()
    {
        // 최초 방 배정은 쿨다운으로 치지 않음
        if (lastKnownRoom == null) { lastKnownRoom = self.currentRoom; return; }

        // bounds 기반 소속이 실제로 바뀌면 → 이주 완료 처리 + 쿨다운 시작
        if (self.currentRoom != lastKnownRoom)
        {
            if (debugLog)
            {
                string from = lastKnownRoom != null ? lastKnownRoom.roomID : "null";
                string to = self.currentRoom != null ? self.currentRoom.roomID : "null";
                bool connected = lastKnownRoom != null && self.currentRoom != null && IsConnected(lastKnownRoom, self.currentRoom);
                Debug.Log($"[Migrate:{DbgName}] 방 변경 {from} → {to} · 열린통로로연결={connected} · isMigrating={isMigrating}" +
                          (connected ? "" : "  ⚠ 연결 안 된 방으로 넘어감(벽 통과)"), this);
            }
            _prevRoom = lastKnownRoom;                          // 방금 떠나온 방 → 배회로는 다시 안 돌아감
            lastKnownRoom = self.currentRoom;
            lastMigrateTime = Time.time;
            isMigrating = false;
            wanderDrifting = false;   // 새 방 도착 → 다음 배회 이동은 새로 추첨
        }
    }

    // 이 생물이 열린 문을 따라 흘러다니는 입자인가 (L, A)
    private bool WandersThroughDoors()
    {
        // 이동 가능한 생물(canMigrate)이면 열린 문/뚫린 벽으로 흘러다님 (도구=canMigrate false는 안 함)
        return self != null && self.canMigrate;
    }

    // 쫓을 대상이 없어도, 갈 수 있는 열린 문이 있으면 확률적으로 그쪽으로 흘러가기로 결정.
    // 쿨다운마다 1번만 추첨 → 매 틱 재추첨 방지. 실패하면 다음 추첨까지 쿨다운.
    public bool WantsWanderMigrate()
    {
        if (!WandersThroughDoors() || self.currentRoom == null) return false;
        if (wanderDrifting)
        {
            // 제한시간 내 못 넘어가면 포기 → wander 복귀, 쿨다운 후 재시도
            if (Time.time - _driftStart > migrateTimeout)
            {
                wanderDrifting = false;
                isMigrating = false;
                lastMigrateTime = Time.time;
                return false;
            }
            return true;            // 아직 흘러가는 중 — 유지
        }
        if (MigrateOnCooldown) return false;        // 아직 추첨 타이밍 아님

        // 되돌아가기 제외하고 갈 수 있는 통로(문/부서진 벽)가 있나
        if (!CalculatePassage(out _, out _, out _, avoidBacktrack: true)) return false;

        if (Random.value < wanderMigrateChance)
        {
            wanderDrifting = true;
            _driftStart = Time.time;    // 이동 시도 타이머 시작
            return true;
        }

        lastMigrateTime = Time.time;   // 실패 → 잠시 뒤 다시 추첨
        return false;
    }

    public bool TickMigration()
    {
        if (self.currentRoom == null) { Dbg("[Tick] currentRoom=null"); return false; }
        if (MigrateOnCooldown) { Dbg($"[Tick] 쿨다운 {migrateCooldown - (Time.time - lastMigrateTime):0.0}s"); return false; }

        // 배회 이동 중엔 방금 온 방으로 되돌아가지 않게
        if (!CalculatePassage(out Vector3 pt, out Room nextR, out float bestDist, avoidBacktrack: wanderDrifting))
        {
            isMigrating = false;   // 갈 통로 없음 → bounds 무시 해제(벽 통과 방지)
            return false;
        }

        if (bestDist > migrateDoorReachDist)
        {
            // 아직 통로(문/틈)까지 멀면 그 자리로 향함
            migrateTargetPoint = pt;
            isMigrating = false;
            Dbg($"[Tick] {nextR.roomID}로 접근 중 dist={bestDist:0.0} (아직 문 밖, isMigrating=false)");
        }
        else
        {
            // 방 중심은 출입구와 어긋날 수 있어 벽에 걸린다. 통로를 기준으로
            // 방 바깥 방향으로 밀어 출입구를 실제로 통과하게 한다.
            Vector3 roomCenter = self.currentRoom.homeBound != null
                ? self.currentRoom.homeBound.bounds.center : self.currentRoom.transform.position;
            Vector3 through = pt - roomCenter; through.y = 0f;
            if (through.sqrMagnitude < 0.0001f)
                through = (nextR.transform.position - pt).normalized;
            else through.Normalize();
            migrateTargetPoint = pt + through * doorThroughPush;
            isMigrating = true;
            Dbg($"[Tick] {nextR.roomID} 통로 관통 목표={migrateTargetPoint} (passage={pt}, isMigrating=true)");
        }

        return true;
    }

    // 두 방이 '열린 문' 또는 '부서진 벽'으로 실제 연결돼 있나 (로그/진단용)
    private bool IsConnected(Room a, Room b)
    {
        if (a == null || b == null) return false;
        if (a.doors != null)
            foreach (var d in a.doors)
                if (d != null && d.isOpen && d.GetOtherRoom(a) == b) return true;
        if (a.brokenPassages != null)
            foreach (var w in a.brokenPassages)
                if (w != null && w.Broken && w.GetOtherRoom(a) == b) return true;
        return false;
    }

    // 옆방(열린 문/부서진 벽 너머)에 쫓을 대상이 있으면, 그 '대상이 있는 방'으로 가는 통로로 이주.
    // (HasChaseTargetInAdjacentRoom + TickMigration 조합은 가장 가까운 통로로 가버려 엉뚱한 방에 갈 수 있어 분리 대신 이걸 씀)
    public bool TickChaseMigration()
    {
        if (self.currentRoom == null) { Dbg("currentRoom=null → 이주 불가"); return false; }
        if (MigrateOnCooldown) { Dbg($"쿨다운 중 ({migrateCooldown - (Time.time - lastMigrateTime):0.0}s 남음)"); return false; }

        var room = self.currentRoom;
        Vector3 myPos = self.rootTransform.position;
        Vector3 bestPoint = default; Room bestRoom = null; float bestDist = float.MaxValue;
        int openDoors = 0, candidates = 0;

        if (room.doors != null)
            foreach (var d in room.doors)
            {
                if (d == null) continue;
                if (!d.isOpen) continue;
                openDoors++;
                Room o = d.GetOtherRoom(room);
                bool avoid = o != null && ShouldAvoidRoom(o);
                bool chase = o != null && HasChaseIn(o);
                Dbg($"문→{(o != null ? o.roomID : "null")} open=Y avoid={avoid} 쫓을대상={chase}");
                if (o == null || avoid || !chase) continue;
                candidates++;
                Transform t = (d.self != null && d.self.rootTransform != null) ? d.self.rootTransform : d.transform;
                float dist = MigrationDistance(myPos, t.position);
                if (dist < bestDist) { bestDist = dist; bestPoint = t.position; bestRoom = o; }
            }

        if (room.brokenPassages != null)
            foreach (var w in room.brokenPassages)
            {
                if (w == null || !w.Broken) continue;
                Room o = w.GetOtherRoom(room);
                if (o == null || ShouldAvoidRoom(o) || !HasChaseIn(o)) continue;
                candidates++;
                float dist = MigrationDistance(myPos, w.Position);
                if (dist < bestDist) { bestDist = dist; bestPoint = w.Position; bestRoom = o; }
            }

        if (bestRoom == null)
        {
            isMigrating = false;   // 갈 통로 없음 → bounds 무시 해제
            return false;
        }

        Dbg($"→ {bestRoom.roomID}로 이주 (dist={bestDist:0.0}, reach={migrateDoorReachDist})");

        if (bestDist > migrateDoorReachDist)
        {
            migrateTargetPoint = bestPoint;              // 통로까지 멀면 통로로 향함
            isMigrating = false;
        }
        else
        {
            // 통로에 붙음 → 방 중심(대각선)이 아니라 'opening을 똑바로 관통'하는 지점으로.
            Vector3 roomCenter = room.homeBound != null ? room.homeBound.bounds.center : room.transform.position;
            Vector3 through = bestPoint - roomCenter; through.y = 0f;
            through = through.sqrMagnitude > 0.0001f
                ? through.normalized
                : (bestRoom.transform.position - bestPoint).normalized;   // 폴백: 옆방 쪽
            migrateTargetPoint = bestPoint + through * doorThroughPush;
            isMigrating = true;
        }
        return true;
    }

    //옆방에 다가가고싶은 대상이 있는지 (문·부서진 벽 너머)
    public bool HasChaseTargetInAdjacentRoom()
    {
        var room = self.currentRoom;
        if (room == null) return false;

        if (room.doors != null)
            foreach (var d in room.doors)
                if (d != null && d.isOpen && HasChaseIn(d.GetOtherRoom(room))) return true;

        if (room.brokenPassages != null)
            foreach (var w in room.brokenPassages)
                if (w != null && w.Broken && HasChaseIn(w.GetOtherRoom(room))) return true;

        return false;
    }

    private bool HasChaseIn(Room other)
    {
        if (other == null) return false;
        foreach (var c in other.creatureList)
        {
            if (c == null || c.data == null) continue;
            if (self.HasAction(c.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }

    public bool ShouldAvoidRoom(Room room)
    {
        if (room == null || self == null) return false;
        foreach (var c in room.creatureList)
        {
            if (c == null || c.data == null) continue;
            if (self.HasAction(c.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }

    // 꽉 찬 방: L/A 수가 D 정리 기준(heatThreshold) 이상 → 배회로는 잘 안 감 (과밀·불필요한 정리 방지)
    private bool IsCrowded(Room room)
    {
        return room != null && room.heatThreshold > 0 && room.HeatLoad() >= room.heatThreshold;
    }

    // 갈 수 있는 통로(열린 문 + 부서진 벽) 중 가장 가까운 것. 위치·옆방·거리를 돌려줌.
    bool CalculatePassage(out Vector3 point, out Room other, out float bestDist, bool avoidBacktrack = false)
    {
        point = default; other = null; bestDist = float.MaxValue;
        var room = self.currentRoom;
        if (room == null) return false;
        Vector3 myPos = self.rootTransform.position;

        // 열린 문
        if (room.doors != null)
            foreach (var d in room.doors)
            {
                if (d == null) continue;
                if (!d.isOpen) { Dbg($"[Passage] 문 {d.name}: 닫힘"); continue; }
                Room o = d.GetOtherRoom(room);
                if (!PassOK(o, avoidBacktrack))
                {
                    Dbg($"[Passage] 문 {d.name}→{(o != null ? o.roomID : "null")}: 제외 (avoid={o != null && ShouldAvoidRoom(o)}, backtrack={avoidBacktrack && o == _prevRoom}, crowded={avoidBacktrack && IsCrowded(o)})");
                    continue;
                }
                Transform t = (d.self != null && d.self.rootTransform != null) ? d.self.rootTransform : d.transform;
                float dist = MigrationDistance(myPos, t.position);
                if (dist < bestDist) { bestDist = dist; point = t.position; other = o; }
            }

        // 부서진 벽(틈) — 그 자리로 향해 옆방으로 관통
        if (room.brokenPassages != null)
            foreach (var w in room.brokenPassages)
            {
                if (w == null || !w.Broken) continue;
                Room o = w.GetOtherRoom(room);
                if (!PassOK(o, avoidBacktrack))
                {
                    Dbg($"[Passage] 틈 {w.name}→{(o != null ? o.roomID : "null")}: 제외 (avoid={o != null && ShouldAvoidRoom(o)}, backtrack={avoidBacktrack && o == _prevRoom}, crowded={avoidBacktrack && IsCrowded(o)})");
                    continue;
                }
                float dist = MigrationDistance(myPos, w.Position);
                if (dist < bestDist) { bestDist = dist; point = w.Position; other = o; }
            }

        return other != null;
    }

    // 그 방으로 넘어가도 되는지 (회피 대상/되돌아가기/과밀 체크)
    bool PassOK(Room o, bool avoidBacktrack)
    {
        if (o == null) return false;
        if (ShouldAvoidRoom(o)) return false;
        // 배회 이동 땐 방금 떠나온 방으로는 다시 안 감 (쿨다운 끝난 뒤에도 유지 — 앞으로 흘러가게)
        if (avoidBacktrack && o == _prevRoom) return false;
        if (avoidBacktrack && IsCrowded(o)) return false;
        return true;
    }
}
