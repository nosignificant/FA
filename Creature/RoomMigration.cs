using System.Linq;
using UnityEngine;
using CreatureTypes;

[RequireComponent(typeof(Creature))]
public class RoomMigration : MonoBehaviour
{
    [Header("References")]
    public Creature self;
    public Think2 think;

    [Header("Config")]
    public bool canMigrate = false;
    public float migrateCooldown = 8f;

    [Tooltip("문에 이 거리 이내로 붙으면 반대쪽 방으로 전환")]
    public float migrateDoorReachDist = 2f;

    [System.NonSerialized] public float lastMigrateTime = -999f;
    [System.NonSerialized] public Vector3 migrateTargetPoint; //state쪽에서 사용
    [System.NonSerialized] public bool isApproachingCenter = false;  // 문 관통~새 방 진입 중(아직 옛 방 소속)
    [System.NonSerialized] public Room migrateTargetRoom;            // 진입 목표 방(경계 진입 시 소속 변경)

    public bool MigrateOnCooldown => Time.time - lastMigrateTime < migrateCooldown;

    private void Awake()
    {
        if (self == null) self = GetComponent<Creature>();
        if (think == null) think = GetComponent<Think2>();
    }

    public bool TickMigration()
    {
        if (isApproachingCenter)
        {
            if (migrateTargetRoom == null)
            {
                isApproachingCenter = false;
                return false;
            }
            Debug.Log(self.data.name + " : Approaching center");
            Vector3 pos = self.rootTransform.position;

            if (think.IsInsideBounds(pos, migrateTargetRoom.homeBound.bounds)) RegisterToOtherRoom();

            return true;
        }

        if (self.currentRoom == null) return false;
        if (MigrateOnCooldown) return false;

        Door bestDoor = CalculateDoor(out float bestDist);

        if (bestDoor == null) return false;

        Room nextR = bestDoor.GetOtherRoom(self.currentRoom);
        Vector3 doorPos = bestDoor.self.rootTransform.position;


        if (bestDist > migrateDoorReachDist)//문쪽까지 가기
        {
            migrateTargetPoint = doorPos;
        }
        else //문까지갔으면 다음 방으로 가기
        {
            migrateTargetRoom = nextR;
            migrateTargetPoint = nextR.transform.position;
            isApproachingCenter = true;
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

    void RegisterToOtherRoom()
    {
        self.currentRoom.UnregisterCreature(self);
        migrateTargetRoom.RegisterCreature(self);
        self.transform.SetParent(migrateTargetRoom.transform, true);
        lastMigrateTime = Time.time;
        Debug.Log($"[RoomMigration] {self.name} → {migrateTargetRoom.name}");

        migrateTargetRoom = null;
        isApproachingCenter = false;
    }
}
