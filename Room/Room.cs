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

    public enum SignalRole { None, ReceiverRoom, TransmitterRoom }

    [Header("signal")]
    [Tooltip("이 방의 신호 역할. R방이면 R 오브젝트만, T방이면 T 오브젝트만 켬 (편집 시 RoomEditor에서 토글)")]
    public SignalRole signalRole = SignalRole.None;

    [Tooltip("이 방의 신호 수신기(R). 이 방의 SignalGate 문들이 자동으로 이걸 입력으로 씀. 비우면 문 각자 지정값 사용")]
    public SignalReceiver signalReceiver;
    [Tooltip("이 방의 신호 발신기(T)")]
    public SignalTransmitter signalTransmitter;

    // 역할에 맞춰 R/T 오브젝트를 켜고 끔 (R방 → R만, T방 → T만). 편집·런타임 공용.
    public void ApplySignalRole()
    {
        bool isR = signalRole == SignalRole.ReceiverRoom;
        bool isT = signalRole == SignalRole.TransmitterRoom;
        if (signalReceiver != null) signalReceiver.gameObject.SetActive(isR);
        if (signalTransmitter != null) signalTransmitter.gameObject.SetActive(isT);
    }

    // ── 방 활성화 상태 (L/A) ──────────────────────────────────────
    public enum RoomActivation { None, L, A }

    [Header("activation (L/A 상태)")]
    [Tooltip("방 안 L/A 입자 중 우세한 쪽으로 활성화. 문 열림·생물 각성 기준.")]
    public RoomActivation activation = RoomActivation.None;
    [Tooltip("활성화 갱신 주기(초)")]
    public float activationCheckInterval = 0.3f;
    [Tooltip("우세한 쪽 입자 수가 이 값 이상이어야 L/A로 활성화(켜짐 기준).")]
    public int activationMinCount = 1;
    [Tooltip("히스테리시스: 한번 활성화되면 그 종 입자가 이 수 이하로 떨어질 때까지 상태 유지(꺼짐 기준). " +
             "문 열리자마자 입자가 빠져 바로 닫히는 진동 방지. 기본 0 = 다 빠질 때까지 유지.")]
    public int activationHoldCount = 0;

    public RoomActivation Activation => activation;
    public event Action<RoomActivation> OnActivationChanged;

    // 색은 각 WallActivationTint가 관리. Room은 자식에서 모아 상태 바뀔 때 Apply 호출.
    private WallActivationTint[] activationTints;
    private void CollectWalls() => activationTints = GetComponentsInChildren<WallActivationTint>(true);
    private void RefreshWalls()
    {
        if (activationTints == null) return;
        for (int i = 0; i < activationTints.Length; i++)
            if (activationTints[i] != null) activationTints[i].Apply(activation);
    }

    // 방 안 L/A 우세로 활성화 계산 (동수는 현 상태 유지 → 깜빡임 방지)
    private RoomActivation ComputeActivation()
    {
        int nL = 0, nA = 0;
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c == null || c.IsDead || c.data == null) continue;
            var id = c.data.creatureID;
            if (id == CreatureID.L) nL++;
            else if (id == CreatureID.A) nA++;
        }
        // 히스테리시스: 이미 활성 상태면, 그 종이 hold 기준 초과로 남아있는 한 유지(우세만 지키면 됨).
        // → 문이 열려 입자가 빠지기 시작해도 무리가 다 통과할 때까지 상태 유지(진동 방지).
        if (activation == RoomActivation.L && nL > activationHoldCount && nL >= nA) return RoomActivation.L;
        if (activation == RoomActivation.A && nA > activationHoldCount && nA >= nL) return RoomActivation.A;

        // 새로 켜기: 우세 + activationMinCount 이상
        if (nA > nL) return nA >= activationMinCount ? RoomActivation.A : RoomActivation.None;
        if (nL > nA) return nL >= activationMinCount ? RoomActivation.L : RoomActivation.None;
        return nL == 0 ? RoomActivation.None : activation;   // 동수(0이면 None, 아니면 유지)
    }

    private void SetActivation(RoomActivation a)
    {
        if (a == activation) return;
        activation = a;
        RefreshWalls();                     // 벽 색 갱신
        OnActivationChanged?.Invoke(a);
        OnCreatureCountChanged?.Invoke();   // 문 조건 재평가
    }

    private IEnumerator ActivationLoop()
    {
        CollectWalls();
        RefreshWalls();   // 초기 색
        var wait = new WaitForSeconds(activationCheckInterval);
        while (true)
        {
            SetActivation(ComputeActivation());
            yield return wait;
        }
    }

    // 이 방에 접한 부술 수 있는 벽들 (S가 찾아 부숨). BreakableWall이 등록.
    [System.NonSerialized] public List<BreakableWall> breakableWalls = new();
    public void RegisterBreakable(BreakableWall w)
    {
        if (w != null && !breakableWalls.Contains(w)) breakableWalls.Add(w);
    }
    public void UnregisterBreakable(BreakableWall w) => breakableWalls.Remove(w);

    // 부서진 벽 = 통로. migration이 이 자리(틈)로 향해 옆방으로 넘어감.
    [System.NonSerialized] public List<BreakableWall> brokenPassages = new();
    public void RegisterPassage(BreakableWall w)
    {
        if (w != null && !brokenPassages.Contains(w)) brokenPassages.Add(w);
    }
    public void UnregisterPassage(BreakableWall w) => brokenPassages.Remove(w);

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

    [Header("heat (발열/과밀)")]
    [Tooltip("방 안 L/A 입자 수가 이 값을 넘으면 과밀 → D가 정리. 0 이하면 발열 없음")]
    public int heatThreshold = 0;

    // 과밀 계산: 방 안 살아있는 L/A 입자 수
    public int HeatLoad()
    {
        int n = 0;
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c == null || c.IsDead || c.data == null) continue;
            var id = c.data.creatureID;
            if (id == CreatureID.L || id == CreatureID.A) n++;
        }
        return n;
    }

    // 과밀 상태 — D가 이때만 정리
    public bool IsHot => heatThreshold > 0 && HeatLoad() > heatThreshold;

    private bool creaturesFrozen = false;

    // 방 활성/비활성 전환. 상시 시뮬이 꺼져 있으면 비활성 방 생물의 '움직임'까지 완전 정지.
    // (isActive만으로는 Think만 멈추고 다리·텐타클·물리는 관성으로 계속 움직임)
    public void SetActive(bool active)
    {
        isActive = active;

        bool sim = RoomManager.Instance != null && RoomManager.Instance.simulateInactiveRooms;
        // 튜토리얼 방은 상시 시뮬과 무관하게, 비활성이면 무조건 정지
        bool shouldFreeze = !active && (!sim || isTutorial);
        if (shouldFreeze != creaturesFrozen)
        {
            creaturesFrozen = shouldFreeze;
            SetCreaturesFrozen(shouldFreeze);
        }
    }

    private void SetCreaturesFrozen(bool frozen)
    {
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c == null || c.IsDead || c.data == null) continue;
            if (c.IsGrabbed) continue;   // 분해·합성 중 개체는 건드리지 않음
            var id = c.data.creatureID;
            if (id == CreatureID.Door || id == CreatureID.Player) continue;
            c.SetMovementEnabled(!frozen);
        }
    }

    [Header("creature")]
    public List<Creature> creatureList = new();
    public Dictionary<CreatureData, int> decomposedCounts = new();
    public event Action<Creature, CreatureID> OnCreatureDecomposed;
    public event Action<Creature, CreatureID> OnCreatureSynthesized;   // 합성 결과 생물, 합성한 종(L 등)
    public event Action OnCreatureCountChanged;   // 방 안 생물이 들어오거나 나갈 때 (문 조건 재평가용)

    // 문 카운트·UI 목록에서 제외할 종 (D/Door/Player, weed 계열)
    public static bool IsExcludedFromCount(CreatureID id)
    {
        return id == CreatureID.Door
            || id == CreatureID.Player
            || id == CreatureID.Weed
            || id == CreatureID.WalkingWeed
            || id == CreatureID.T      // 발신기 — 회로 부품, 우세 카운트 제외
            || id == CreatureID.R;     // 수신기 — 회로 부품, 우세 카운트 제외
    }

    // 방 안 살아있는 생물을 종별로 집계 (D/Door/Player 제외). 문 조건·UI 공용.
    public Dictionary<CreatureData, int> SpeciesCounts()
    {
        var counts = new Dictionary<CreatureData, int>();
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c == null || c.IsDead || c.data == null) continue;
            if (c.data.excludeFromRoomCount) continue;   // weed 등 CreatureData에서 제외 체크
            if (IsExcludedFromCount(c.data.creatureID)) continue;
            counts.TryGetValue(c.data, out int n);
            counts[c.data] = n + 1;
        }
        return counts;
    }

    // 방 안에 살아있는 생물 중 가장 수가 많은 종족(family). 문 열림 기준.
    // 같은 종족은 합산해서 하나로 취급. 반환값은 그 종족의 대표 CreatureData.
    public CreatureData MostNumerousSpecies()
    {
        // 종족별 합산 카운트 + 대표 asset (가장 수 많은 개별 asset을 대표로)
        var familyCount = new Dictionary<CreatureID, int>();
        var familyRep = new Dictionary<CreatureID, CreatureData>();
        var familyRepCount = new Dictionary<CreatureID, int>();

        foreach (var kv in SpeciesCounts())
        {
            CreatureID fam = CreatureFamily.Of(kv.Key.creatureID);
            familyCount.TryGetValue(fam, out int fc);
            familyCount[fam] = fc + kv.Value;

            // 대표는 그 종족 안에서 개체 수가 가장 많은 asset
            familyRepCount.TryGetValue(fam, out int rc);
            if (kv.Value > rc)
            {
                familyRepCount[fam] = kv.Value;
                familyRep[fam] = kv.Key;
            }
        }

        CreatureID bestFam = default;
        int bestCount = 0;
        bool found = false;
        foreach (var kv in familyCount)
            if (kv.Value > bestCount) { bestCount = kv.Value; bestFam = kv.Key; found = true; }

        return found ? familyRep[bestFam] : null;
    }

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

    public Wall GetWall(Direction dir)
    {
        // wallSlots에 지정돼 있으면 그것 우선
        var slot = GetWallSlot(dir);
        if (slot.HasValue && slot.Value.wall != null) return slot.Value.wall;
        // 없으면 자식 Wall 중 direction 일치하는 것 자동 탐색 (wallSlots 미설정/끊김 대비)
        foreach (var w in GetComponentsInChildren<Wall>(true))
            if (w != null && w.direction == dir) return w;
        return null;
    }
    // 문 참조는 Wall이 들고 있음 (slot.door가 아니라 slot.wall.door)
    public Door GetDoor(Direction dir) => GetWall(dir)?.door;

    public void SetDoorWall(Direction dir, bool hasDoor)
    {
        GetWall(dir)?.SetDoor(hasDoor);
    }

    // 이 방에 특정 종의 살아있는 생물이 있는가
    public bool HasSpecies(CreatureID id)
    {
        for (int i = 0; i < creatureList.Count; i++)
        {
            var c = creatureList[i];
            if (c != null && !c.IsDead && c.data != null && c.data.creatureID == id) return true;
        }
        return false;
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

    // 합성 결과 생물이 스폰됐을 때 SynthesizeState가 호출
    public void NotifySynthesized(Creature result, CreatureID synthesizerID)
    {
        OnCreatureSynthesized?.Invoke(result, synthesizerID);
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

        StartCoroutine(ActivationLoop());   // L/A 방 상태 갱신
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
            if (c.IsControlled) continue;                         // 조종 중인 생물은 플레이어가 몰고 다님(방 이동 허용)

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

        OnCreatureCountChanged?.Invoke();
    }

    public void UnregisterCreature(Creature c)
    {
        c.Died -= OnCreatureDied;
        if (creatureList.Contains(c))
        {
            creatureList.Remove(c);
            OnCreatureCountChanged?.Invoke();
        }
    }

    private void OnCreatureDied(Creature c, CreatureID who)
    {
        UnregisterCreature(c);
        c.Died -= OnCreatureDied;
    }
}
