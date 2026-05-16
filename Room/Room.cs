using System;
using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

public class Room : MonoBehaviour
{
    [Serializable]
    public struct WallSlot
    {
        public Direction direction;
        public Wall wall;
        public Door door;
    }

    public string roomID;
    public bool isActive = false;

    [Header("room bounds")]
    public Collider homeBound;
    public Vector3 roomSize = new Vector3(60f, 6f, 60f);

    [Header("walls")]
    public WallSlot[] wallSlots = new WallSlot[4];

    [Header("room outside")]
    public List<Door> doors = new();

    [Header("room inside")]
    public List<Creature> creatureList = new();
    public int maxCreaturesInRoom = 5;
    public Dictionary<CreatureData, int> decomposedCounts = new();
    public event Action<Creature, CreatureID> OnCreatureDecomposed;

    [Header("initial spawn")]
    public CreatureDatabase creatureDB;
    public SpawnEntry[] spawnEntries;

    [Serializable]
    public struct SpawnEntry
    {
        public CreatureID id;
        [Min(0)] public int count;
    }

    void Awake()
    {
        if (homeBound == null) homeBound = GetComponent<Collider>();
        InitWallSlots();
    }

    private void InitWallSlots()
    {
        // 배열 크기 보장
        if (wallSlots == null || wallSlots.Length != 4)
        {
            wallSlots = new WallSlot[4];
            Direction[] dirs = DirectionExt.All;
            for (int i = 0; i < 4; i++)
                wallSlots[i].direction = dirs[i];
        }

        // door 리스트 동기화: WallSlot.door → doors
        doors.Clear();
        foreach (var ws in wallSlots)
        {
            if (ws.door != null && !doors.Contains(ws.door))
                doors.Add(ws.door);
        }
    }

    public WallSlot? GetWallSlot(Direction dir)
    {
        for (int i = 0; i < wallSlots.Length; i++)
            if (wallSlots[i].direction == dir) return wallSlots[i];
        return null;
    }

    public Wall GetWall(Direction dir) => GetWallSlot(dir)?.wall;
    public Door GetDoor(Direction dir) => GetWallSlot(dir)?.door;

    public void SetDoorWall(Direction dir, bool hasDoor)
    {
        GetWall(dir)?.SetDoor(hasDoor);
    }

    // ── decompose ────────────────────────────────────────────────────────

    public (CreatureData, int) MostDecomposedAndSecond()
    {
        CreatureData best = null, second = null;
        int bestCount = 0, secondCount = 0;

        foreach (var kv in decomposedCounts)
        {
            if (kv.Value > bestCount)
            {
                second = best; secondCount = bestCount;
                best = kv.Key; bestCount = kv.Value;
            }
            else if (kv.Value > secondCount)
            {
                second = kv.Key; secondCount = kv.Value;
            }
        }

        return (best, bestCount - secondCount);
    }

    public void NotifyDecomposed(Creature target, CreatureID decomposerID)
    {
        if (target?.data != null)
        {
            if (!decomposedCounts.ContainsKey(target.data)) decomposedCounts[target.data] = 0;
            decomposedCounts[target.data]++;
            Debug.Log($"[Room {roomID}] decomposed {target.data.name}, count={decomposedCounts[target.data]}");
        }
        OnCreatureDecomposed?.Invoke(target, decomposerID);
    }

    // ── lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        roomID = gameObject.name;
        if (RoomManager.Instance != null) RoomManager.Instance.Register(this);
        Player.Instance.roomChanged += ActiveRoom;
        // 이미 자식으로 배치된 생물들 자동 등록
        foreach (Creature c in GetComponentsInChildren<Creature>()) RegisterCreature(c);
    }

    /// <summary>spawnEntries대로 생물을 즉시 Room 자식으로 스폰 (에디터/런타임 공용)</summary>
    public void SpawnInitialCreatures()
    {
        if (spawnEntries == null || creatureDB == null || homeBound == null) return;

        Bounds b = homeBound.bounds;
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            var entry = spawnEntries[i];
            if (entry.count <= 0) continue;

            CreatureData data = creatureDB.GetByID(entry.id);
            GameObject prefab = data?.prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[Room {roomID}] creatureDB에 {entry.id}의 prefab이 없습니다.");
                continue;
            }

            float yOffset = data.spawnYOffset;

            for (int n = 0; n < entry.count; n++)
            {
                Vector3 pos = new Vector3(
                    UnityEngine.Random.Range(b.min.x, b.max.x),
                    b.min.y + yOffset,
                    UnityEngine.Random.Range(b.min.z, b.max.z));

#if UNITY_EDITOR
                GameObject obj = !Application.isPlaying
                    ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform)
                    : Instantiate(prefab, pos, Quaternion.identity, transform);
#else
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
#endif
                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.identity;

                // Play 모드 중 스폰된 거면 즉시 등록 (Start의 자동 등록은 이미 지났음)
                if (Application.isPlaying)
                {
                    Creature c = obj.GetComponent<Creature>();
                    if (c != null) RegisterCreature(c);
                }
            }
        }
    }

    /// <summary>방 벽 상태를 초기 상태(모든 벽 noDoor)로 되돌리고 doors 비움</summary>
    public void ResetWalls()
    {
        for (int i = 0; i < wallSlots.Length; i++)
        {
            Wall w = wallSlots[i].wall;
            if (w != null) w.SetDoor(false);
        }
        doors.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p != null) Player.Instance.SetRoom(this);
    }

    public void ActiveRoom(Room room)
    {
        if (room.roomID == this.roomID) isActive = true;
    }

    // ── creature register ────────────────────────────────────────────────

    public void RegisterCreature(Creature c)
    {
        c.currentRoom = this;
        if (creatureList.Contains(c)) return;

        creatureList.Add(c);

        if (c.data.creatureID != CreatureID.Door && c.data.creatureID != CreatureID.Player)
            c.Died += OnCreatureDied;
    }

    public void UnregisterCreature(Creature c)
    {
        c.Died -= OnCreatureDied;
        if (creatureList.Contains(c)) creatureList.Remove(c);
    }

    private void OnCreatureDied(Creature c, CreatureID who)
    {
        UnregisterCreature(c);
        c.Died -= OnCreatureDied;
    }

    // ── gizmo ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, roomSize);
    }
#endif
}
