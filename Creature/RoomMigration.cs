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

    [Header("열린 문으로 배회 이동 (A/H)")]
    [Tooltip("쫓을 대상이 없어도, 열린 문이 있으면 그쪽 방으로 흘러가려는 확률")]
    [Range(0f, 1f)] public float wanderMigrateChance = 0.7f;

    [System.NonSerialized] public float lastMigrateTime = -999f;
    [System.NonSerialized] public Vector3 migrateTargetPoint; //state쪽에서 사용
    [System.NonSerialized] public bool isMigrating = false;  // 문 통과 중(소속 갱신 전) → KeepCreaturesInBounds 예외
    private Room lastKnownRoom;
    private Room previousRoom;          // 직전에 있던 방 (되돌아가기 억제)
    private bool wanderDrifting = false; // 열린 문으로 흘러가기로 결정된 상태 (쿨다운당 1회 추첨 유지)

    public bool MigrateOnCooldown => Time.time - lastMigrateTime < migrateCooldown;

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
            previousRoom = lastKnownRoom;
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
        if (wanderDrifting) return true;            // 이미 흘러가기로 정함 — 유지
        if (MigrateOnCooldown) return false;        // 아직 추첨 타이밍 아님

        // 되돌아가기 제외하고 갈 수 있는 통로(문/부서진 벽)가 있나
        if (!CalculatePassage(out _, out _, out _, avoidBacktrack: true)) return false;

        if (Random.value < wanderMigrateChance)
        {
            wanderDrifting = true;
            return true;
        }

        lastMigrateTime = Time.time;   // 실패 → 잠시 뒤 다시 추첨
        return false;
    }

    public bool TickMigration()
    {
        if (self.currentRoom == null || MigrateOnCooldown) return false;

        // 배회 이동 중엔 방금 온 방으로 되돌아가지 않게
        if (!CalculatePassage(out Vector3 pt, out Room nextR, out float bestDist, avoidBacktrack: wanderDrifting))
            return false;

        if (bestDist > migrateDoorReachDist)
        {
            // 아직 통로(문/틈)까지 멀면 그 자리로 향함
            migrateTargetPoint = pt;
            isMigrating = false;
        }
        else
        {
            // 통로에 붙었으면 옆방 중심으로 관통 (소속 변경은 bounds 넘는 순간 Creature가 처리)
            migrateTargetPoint = nextR.transform.position;
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
                if (d == null || !d.isOpen) continue;
                Room o = d.GetOtherRoom(room);
                if (!PassOK(o, avoidBacktrack)) continue;
                Transform t = (d.self != null && d.self.rootTransform != null) ? d.self.rootTransform : d.transform;
                float dist = Vector3.Distance(myPos, t.position);
                if (dist < bestDist) { bestDist = dist; point = t.position; other = o; }
            }

        // 부서진 벽(틈) — 그 자리로 향해 옆방으로 관통
        if (room.brokenPassages != null)
            foreach (var w in room.brokenPassages)
            {
                if (w == null || !w.Broken) continue;
                Room o = w.GetOtherRoom(room);
                if (!PassOK(o, avoidBacktrack)) continue;
                float dist = Vector3.Distance(myPos, w.Position);
                if (dist < bestDist) { bestDist = dist; point = w.Position; other = o; }
            }

        return other != null;
    }

    // 그 방으로 넘어가도 되는지 (회피 대상/되돌아가기/과밀 체크)
    bool PassOK(Room o, bool avoidBacktrack)
    {
        if (o == null) return false;
        if (ShouldAvoidRoom(o)) return false;
        if (avoidBacktrack && o == previousRoom) return false;
        if (avoidBacktrack && IsCrowded(o)) return false;
        return true;
    }
}
