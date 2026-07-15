using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

//방에 필요한거
//콜라이더 설정 


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
    public bool isTutorial = false;

    [Header("room bounds")]
    public Collider homeBound;
    public Vector3 roomSize = new Vector3(60f, 6f, 60f);

    [Header("walls")]
    public WallSlot[] wallSlots = new WallSlot[4];

    [Header("Doors")]
    public List<Door> doors = new();

    public void RegisterDoor(Door d)
    {
        if (d == null) return;
        if (!doors.Contains(d)) doors.Add(d);
    }

    public void UnregisterDoor(Door d)
    {
        if (d == null) return;
        doors.Remove(d);
    }

    [Header("creature")]
    public List<Creature> creatureList = new();
    public int maxCreaturesInRoom = 5;
    public Dictionary<CreatureData, int> decomposedCounts = new();
    public event Action<Creature, CreatureID> OnCreatureDecomposed;

    [Header("initial spawn")]
    public CreatureDatabase creatureDB;
    [Min(0)] public int minDCount = 1;
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

    //방 처음 만들때 실행 
    private void InitWallSlots()
    {
        Direction[] dirs = DirectionExt.All;
        if (wallSlots == null || wallSlots.Length != dirs.Length)
        {
            wallSlots = new WallSlot[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
                wallSlots[i].direction = dirs[i];
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


    void Start()
    {
        roomID = gameObject.name;
        if (RoomManager.Instance != null) RoomManager.Instance.Register(this);

        doors.RemoveAll(d => d == null);

        // hierarchy 부모가 아니라 위치(bounds) 기준으로 소속 결정
        RegisterCreaturesInBounds();

        EnsureOneD();
        if (minDCount > 0) StartCoroutine(DKeepAliveLoop());
    }

    // bounds 안에 있는 모든 생물을 이 방 소속으로 등록 (Door의 self 포함)
    private void RegisterCreaturesInBounds()
    {
        if (homeBound == null) return;
        Bounds b = homeBound.bounds;
        foreach (Creature c in FindObjectsOfType<Creature>())
        {
            if (c == null || c.rootTransform == null) continue;
            if (c.data != null && c.data.creatureID == CreatureID.Player) continue;
            if (b.Contains(c.rootTransform.position)) RegisterCreature(c);
        }
    }

    public float dCheckInterval = 2f;

    private IEnumerator DKeepAliveLoop()
    {
        var wait = new WaitForSeconds(dCheckInterval);
        while (true)
        {
            KillStrayD();
            EnsureOneD();
            KeepCreaturesInBounds();
            yield return wait;
        }
    }

    [Tooltip("방 콜라이더 안쪽으로 이만큼 여유두고 밀어넣음")]
    public float boundsPushPadding = 2f;

    // 방 밖으로 벗어난 생물(D 제외)을 콜라이더 안쪽으로 즉시 이동
    private void KeepCreaturesInBounds()
    {
        if (homeBound == null) return;
        Bounds b = homeBound.bounds;

        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c == null || c.IsDead) continue;
            if (c.data == null) continue;
            if (c.data.creatureID == CreatureID.D) continue;      // D는 KillStrayD가 처리
            if (c.data.creatureID == CreatureID.Door) continue;   // 문은 경계에 있으니 제외
            if (c.data.creatureID == CreatureID.Player) continue; // 플레이어는 직접 이동하니 제외

            // 이주 중이면 건드리지 않음 (문 통과 중일 수 있음)
            var mig = c.GetComponent<RoomMigration>();
            if (mig != null && mig.isMigrating) continue;

            Transform t = c.rootTransform != null ? c.rootTransform : c.transform;
            Vector3 p = t.position;
            if (b.Contains(p)) continue;

            // 가장 가까운 안쪽 점으로 끌어들이기 + 중심 방향으로 살짝 더
            Vector3 inside = b.ClosestPoint(p);
            Vector3 toCenter = b.center - inside;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.001f)
                inside += toCenter.normalized * boundsPushPadding;

            t.position = new Vector3(inside.x, p.y, inside.z);
        }
    }

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

    private int CountD()
    {
        int count = 0;
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c != null && !c.IsDead && c.data != null && c.data.creatureID == CreatureID.D)
                count++;
        }
        return count;
    }

    public void EnsureOneD()
    {
        if (minDCount <= 0 || creatureDB == null || homeBound == null) return;
        int current = CountD();
        if (current >= minDCount) return;

        CreatureData data = creatureDB.GetByID(CreatureID.D);
        GameObject prefab = data?.prefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[Room {roomID}] creatureDB에 D prefab이 없습니다.");
            return;
        }

        Bounds b = homeBound.bounds;
        int toSpawn = minDCount - current;
        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(b.min.x, b.max.x),
                b.min.y + data.spawnYOffset,
                UnityEngine.Random.Range(b.min.z, b.max.z));

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, CreatureRoot.Container);
            Creature dc = obj.GetComponent<Creature>();
            if (dc != null) RegisterCreature(dc);
        }
    }

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

                Transform container = CreatureRoot.Container;
#if UNITY_EDITOR
                GameObject obj = !Application.isPlaying
                    ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, container)
                    : Instantiate(prefab, pos, Quaternion.identity, container);
#else
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, container);
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
}
