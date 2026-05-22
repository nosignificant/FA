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
    [Tooltip("튜토리얼 방 — 문 열려도 자동 isActive 안 됨 (플레이어가 직접 들어와야)")]
    public bool isTutorial = false;

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
    [Tooltip("이 방에 D 생물을 항상 최소 1마리 유지")]
    public bool keepOneD = true;
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
        // 배열 크기 보장 (N/S/E/W/Up/Down = DirectionExt.All)
        Direction[] dirs = DirectionExt.All;
        if (wallSlots == null || wallSlots.Length != dirs.Length)
        {
            wallSlots = new WallSlot[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
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

        EnsureOneD();
        if (keepOneD) StartCoroutine(DKeepAliveLoop());
    }

    [Tooltip("D 존재 체크 주기(초)")]
    public float dCheckInterval = 2f;

    private System.Collections.IEnumerator DKeepAliveLoop()
    {
        var wait = new WaitForSeconds(dCheckInterval);
        while (true)
        {
            KillStrayD();   // 방 밖으로 나간 D는 즉시 사망 처리
            EnsureOneD();   // 없으면 새로 스폰
            yield return wait;
        }
    }

    // 방 collider(homeBound) 밖으로 벗어난 D를 사망 처리 + 리스트에서 제거
    private void KillStrayD()
    {
        if (homeBound == null) return;
        Bounds b = homeBound.bounds;

        for (int i = creatureList.Count - 1; i >= 0; i--)
        {
            var c = creatureList[i];
            if (c == null) { creatureList.RemoveAt(i); continue; }
            if (c.data == null || c.data.creatureID != CreatureID.D) continue;

            Vector3 p = c.rootTransform != null ? c.rootTransform.position : c.transform.position;
            if (!b.Contains(p))
            {
                UnregisterCreature(c);
                if (!c.IsDead) c.Die(CreatureID.D);
            }
        }
    }

    private bool HasD()
    {
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c != null && !c.IsDead && c.data != null && c.data.creatureID == CreatureID.D)
                return true;
        }
        return false;
    }

    public void EnsureOneD()
    {
        if (!keepOneD || creatureDB == null || homeBound == null) return;
        if (HasD()) return;

        CreatureData data = creatureDB.GetByID(CreatureID.D);
        GameObject prefab = data?.prefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[Room {roomID}] creatureDB에 D prefab이 없습니다.");
            return;
        }

        Bounds b = homeBound.bounds;
        Vector3 pos = new Vector3(
            UnityEngine.Random.Range(b.min.x, b.max.x),
            b.min.y + data.spawnYOffset,
            UnityEngine.Random.Range(b.min.z, b.max.z));

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
        Creature dc = obj.GetComponent<Creature>();
        if (dc != null) RegisterCreature(dc);
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
