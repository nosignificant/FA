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

    // 이 생물이 열린 문을 따라 흘러다니는 성향이 있는가 (A, H)
    private bool WandersThroughDoors()
    {
        if (self == null || self.data == null) return false;
        var id = self.data.creatureID;
        return id == CreatureID.A || id == CreatureID.H;
    }

    // 쫓을 대상이 없어도, 갈 수 있는 열린 문이 있으면 확률적으로 그쪽으로 흘러가기로 결정.
    // 쿨다운마다 1번만 추첨 → 매 틱 재추첨 방지. 실패하면 다음 추첨까지 쿨다운.
    public bool WantsWanderMigrate()
    {
        if (!WandersThroughDoors() || self.currentRoom == null) return false;
        if (wanderDrifting) return true;            // 이미 흘러가기로 정함 — 유지
        if (MigrateOnCooldown) return false;        // 아직 추첨 타이밍 아님

        // 되돌아가기 제외하고 갈 수 있는 열린 문이 있나
        if (CalculateDoor(out _, avoidBacktrack: true) == null) return false;

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
        Door bestDoor = CalculateDoor(out float bestDist, avoidBacktrack: wanderDrifting);
        if (bestDoor == null) return false;

        Room nextR = bestDoor.GetOtherRoom(self.currentRoom);

        if (bestDist > migrateDoorReachDist)
        {
            // 아직 문까지 거리가 멀면 문으로 향함
            Transform doorT = (bestDoor.self != null && bestDoor.self.rootTransform != null)
                ? bestDoor.self.rootTransform : bestDoor.transform;
            migrateTargetPoint = doorT.position;
            isMigrating = false;
        }
        else
        {
            // 문에 붙었으면 옆방 중심으로 관통 (소속 변경은 bounds 넘는 순간 Creature가 처리)
            migrateTargetPoint = nextR.transform.position;
            isMigrating = true;
        }

        return true;
    }

    //옆방에 다가가고싶은 대상이 있는지 
    public bool HasChaseTargetInAdjacentRoom()
    {
        if (self.currentRoom == null) return false;
        foreach (var door in self.currentRoom.doors)
        {
            if (!door.isOpen) continue;
            Room other = door.GetOtherRoom(self.currentRoom);
            if (other == null) continue;
            foreach (var c in other.creatureList)
            {
                if (c == null || c.data == null) continue;
                if (self.HasAction(c.data.creatureID, InteractionAction.Chase)) return true;
            }
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

    Door CalculateDoor(out float bestDist, bool avoidBacktrack = false)
    {
        Door bestDoor = null;
        bestDist = float.MaxValue;

        foreach (var d in self.currentRoom.doors)
        {
            if (!d.isOpen) continue;
            Room other = d.GetOtherRoom(self.currentRoom);
            if (other == null) continue;
            if (ShouldAvoidRoom(other)) continue;
            if (avoidBacktrack && other == previousRoom) continue;   // 방금 온 방으로 되돌아가지 않음

            // 문 위치: self/rootTransform 미설정이면 문 오브젝트 transform으로 대체 (NRE 방지)
            Transform doorT = (d.self != null && d.self.rootTransform != null) ? d.self.rootTransform : d.transform;
            Vector3 dp = doorT.position;
            float dist = Vector3.Distance(self.rootTransform.position, dp);
            if (dist < bestDist) { bestDist = dist; bestDoor = d; }
        }
        return bestDoor;
    }
}
