using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 생산기 (LL 프리팹에 부착): 지정한 위치들 중 랜덤한 곳에 L 입자를 생성해 holdTime만큼 들고 있다 놓음.
// 같은 방에 AA가 있으면 회피 이동. (합성 기능 없음 → Creature 상속)
public class Lcreature : Creature
{
    [Header("LBehavior")]
    [Tooltip("생산 간격(초)")]
    public float spawnInterval = 3f;
    [Tooltip("생성한 L을 소환 위치에 붙잡아 두는 시간(초). 지나면 놓아 자유롭게 흐름")]
    public float holdTime = 10f;
    public bool needToSpawn = false;

    [Tooltip("이 부모의 하위 자식들을 생성 위치로 사용 (지정 시 아래 리스트 대신 이걸 씀)")]
    public Transform spawnPointsParent;
    [Tooltip("L을 생성할 위치 후보들 (이 중 랜덤). 비우면 자기 위치)")]
    public List<Transform> spawnPoints = new();

    [Tooltip("생산하는 입자 (기본 L)")]
    public CreatureID currentSpawn = CreatureID.L;

    [Header("AA 회피")]
    [Tooltip("같은 방에 AA가 있으면 다른 방으로 이동")]
    public bool fleeFromAA = true;
    public float fleeCheckInterval = 0.4f;

    [Header("Runaway 시 회전 접기")]
    [Tooltip("produce: 저장된 초기 회전 유지 / runaway(도망): 회전 0 / 복귀: 저장값으로")]
    public Transform rotPartA;
    public Transform rotPartB;
    [Tooltip("회전 전환 속도 (클수록 빠르게 접힘/펴짐)")]
    public float rotateSpeed = 8f;
    private Quaternion _rotA = Quaternion.identity;
    private Quaternion _rotB = Quaternion.identity;

    // HUD: 이 개체가 생산기인지
    public bool IsProducer => needToSpawn;

    private void Start()
    {
        // parent 지정 시 그 하위 자식들을 생성 위치로 사용
        if (spawnPointsParent != null)
        {
            spawnPoints = new List<Transform>();
            foreach (Transform child in spawnPointsParent)
                spawnPoints.Add(child);
        }

        // 초기 회전 저장 (produce 상태에서 유지할 값)
        if (rotPartA != null) _rotA = rotPartA.localRotation;
        if (rotPartB != null) _rotB = rotPartB.localRotation;
        if (rotPartA != null || rotPartB != null) StartCoroutine(RotFoldLoop());

        if (fleeFromAA) StartCoroutine(FleeAALoop());
        if (needToSpawn) StartCoroutine(Lbehaviour());
    }

    // runaway(도망) 중엔 회전 0, 그 외엔 저장된 초기 회전으로. (Creature.Update를 가리지 않게 코루틴)
    private IEnumerator RotFoldLoop()
    {
        while (!IsDead)
        {
            bool runaway = intent == CreatureIntent.Flee;
            ApplyFold(rotPartA, _rotA, runaway);
            ApplyFold(rotPartB, _rotB, runaway);
            yield return null;
        }
    }

    private void ApplyFold(Transform t, Quaternion stored, bool runaway)
    {
        if (t == null) return;
        Quaternion target = runaway ? Quaternion.identity : stored;   // 도망=0, 아니면 저장값
        t.localRotation = Quaternion.Slerp(t.localRotation, target, Time.deltaTime * rotateSpeed);
    }

    // 같은 방에 AA가 있으면 인접(AA 없는) 방으로 이동 목표를 잡음
    private IEnumerator FleeAALoop()
    {
        var tc = GetComponent<TargetControl>();
        var wait = new WaitForSeconds(fleeCheckInterval);
        while (!IsDead)
        {
            // 플레이어가 조종 중이면 도망 로직이 이동을 덮어쓰지 않음
            if (!IsControlled && tc != null && currentRoom != null && currentRoom.HasSpecies(CreatureID.AA))
            {
                Transform t = PickExitTargetAwayFromAA();
                if (t != null) tc.SetMovementTarget(t);
            }
            yield return wait;
        }
    }

    // AA 없는 인접 방 우선, 없으면 아무 통로 너머 방으로.
    // 통로 = 열린 문 + 뚫린 벽(brokenPassages). 닫힌 문·안 뚫린 벽은 못 감.
    private Transform PickExitTargetAwayFromAA()
    {
        if (currentRoom == null) return null;

        Room best = null, fallback = null;

        if (currentRoom.doors != null)
            foreach (var d in currentRoom.doors)
            {
                if (d == null || !d.isOpen) continue;   // 닫힌 문으로는 못 감
                Room other = d.GetOtherRoom(currentRoom);
                if (other == null) continue;
                fallback = other;
                if (!other.HasSpecies(CreatureID.AA)) { best = other; break; }
            }

        if (best == null && currentRoom.brokenPassages != null)
            foreach (var w in currentRoom.brokenPassages)
            {
                if (w == null || !w.Broken) continue;   // 뚫린 벽만
                Room other = w.GetOtherRoom(currentRoom);
                if (other == null) continue;
                fallback = other;
                if (!other.HasSpecies(CreatureID.AA)) { best = other; break; }
            }

        Room target = best != null ? best : fallback;
        return target != null ? target.transform : null;
    }

    private IEnumerator Lbehaviour()
    {
        while (currentRoom == null) yield return null;

        while (!IsDead)
        {
            // 조종 중 / 비활성 방 / 합성 중이 아니면 생산
            if (!IsControlled
                && currentRoom != null && currentRoom.isActive
                && intent != CreatureIntent.Synthesizing)
            {
                SpawnAtRandomPoint();
            }
            yield return new WaitForSeconds(spawnInterval);   // 매번 현재 값 읽음
        }
    }

    // 지정 위치들 중 랜덤한 곳에 L 하나 생성 → holdTime만큼 그 자리에 붙잡았다 놓음
    private void SpawnAtRandomPoint()
    {
        GameObject spawnThis = WhichOneSpawn(currentSpawn);
        if (spawnThis == null)
        {
            Debug.LogWarning($"[Lcreature] {name}: creatureDB에서 {currentSpawn} prefab을 못 찾음 (DB 등록 확인)");
            return;
        }

        Transform point = PickSpawnPoint();
        Vector3 pos = point != null ? point.position
                    : (rootTransform != null ? rootTransform.position : transform.position);

        GameObject obj = Instantiate(spawnThis, pos, Quaternion.identity);
        Creature c = obj.GetComponent<Creature>();
        if (c == null) { Destroy(obj); return; }

        currentRoom.RegisterCreature(c);
        StartCoroutine(HoldThenRelease(c, point));
    }

    // spawnPoints 중 랜덤 (비어있으면 null → 자기 위치)
    private Transform PickSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        return null;
    }

    // 생성한 L을 holdTime 동안 소환 위치의 자식으로 붙였다가 놓음
    private IEnumerator HoldThenRelease(Creature c, Transform point)
    {
        if (c == null) yield break;

        c.SetMovementEnabled(false);            // 물리·이동 정지
        Transform ct = c.transform;
        if (point != null) ct.SetParent(point, true);   // 자식으로 (world 유지 → LL 움직여도 자연히 따라감)

        float t = 0f;
        while (t < holdTime)
        {
            if (c == null || c.IsDead) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (c == null || c.IsDead) yield break;
        ct.SetParent(null, true);               // 부모에서 떼어냄 (world 유지)
        c.SetMovementEnabled(true);             // 놓기 → 자유롭게 흐름
    }

    public GameObject WhichOneSpawn(CreatureID idx)
    {
        if (currentRoom == null || currentRoom.creatureDB == null) return null;
        return currentRoom.creatureDB.GetPrefab(idx);
    }

    public void SetLSpawnCreature(CreatureID idx)
    {
        currentSpawn = idx;
    }
}
