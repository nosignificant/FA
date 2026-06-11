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

    [System.NonSerialized] public float lastMigrateTime = -999f;
    [System.NonSerialized] public Vector3 migrateTargetPoint; //state쪽에서 사용
    [System.NonSerialized] public bool isMigrating = false;  // 문 통과 중(소속 갱신 전) → KeepCreaturesInBounds 예외
    private Room lastKnownRoom;

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
            lastKnownRoom = self.currentRoom;
            lastMigrateTime = Time.time;
            isMigrating = false;
        }
    }

    public bool TickMigration()
    {
        if (self.currentRoom == null || MigrateOnCooldown) return false;

        Door bestDoor = CalculateDoor(out float bestDist);
        if (bestDoor == null) return false;

        Room nextR = bestDoor.GetOtherRoom(self.currentRoom);

        if (bestDist > migrateDoorReachDist)
        {
            // 아직 문까지 거리가 멀면 문으로 향함
            migrateTargetPoint = bestDoor.self.rootTransform.position;
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

    Door CalculateDoor(out float bestDist)
    {
        Door bestDoor = null;
        bestDist = float.MaxValue;

        foreach (var d in self.currentRoom.doors)
        {
            if (!d.isOpen) continue;
            Room other = d.GetOtherRoom(self.currentRoom);
            if (other == null) continue;
            if (ShouldAvoidRoom(other)) continue;

            //door에 roottransform 있었던가?
            Vector3 dp = d.self.rootTransform.position;
            float dist = Vector3.Distance(self.rootTransform.position, dp);
            if (dist < bestDist) { bestDist = dist; bestDoor = d; }
        }
        return bestDoor;
    }
}
