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
    [Tooltip("같은 방에 AA가 있으면 인접(AA 없는) 방으로 도망")]
    public bool fleeFromAA = true;
    public float fleeCheckInterval = 0.4f;
    [Tooltip("도망 이동 진단 로그(0.5초 간격)")]
    public bool debugFlee = false;
    [Tooltip("같은 방에 AA가 있으면 생산도 멈춤")]
    public bool stopProduceWhenAA = true;

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

    // 같은 방에 AA가 있는지 (있으면 도망 + 생산 정지)
    private bool AAInRoom() => currentRoom != null && currentRoom.HasSpecies(CreatureID.AA);

    // LL 자체 이동은 Think/FleeState가 담당한다. 별도 exit Transform을
    // movementTarget에 넣으면 방 중앙/벽 쪽으로 향하거나 방 이동 후에도
    // proxy 대신 낡은 목표를 따라가는 문제가 생긴다.
    private IEnumerator FleeAALoop()
    {
        var think = GetComponent<Think2>();
        var wait = new WaitForSeconds(fleeCheckInterval);
        bool wasFleeing = false;
        while (!IsDead)
        {
            if (currentRoom != null)
            {
                bool aaPresent = currentRoom.HasSpecies(CreatureID.AA);
                bool fleeing = !IsControlled && intent == CreatureIntent.Flee;
                if (debugFlee)
                {
                    Vector3 selfP = rootTransform != null ? rootTransform.position : transform.position;
                    float moved = (selfP - _dbgLastPos).magnitude;
                    _dbgLastPos = selfP;
                    Vector3 proxyP = think != null && think.ProxyTarget != null ? think.ProxyTarget.position : Vector3.zero;
                    float proxyMoved = (proxyP - _dbgLastProxy).magnitude;
                    _dbgLastProxy = proxyP;
                    var mt = GetComponent<TargetControl>()?.movementTarget;
                    string mtName = mt != null ? mt.name : "null";
                    Debug.Log($"[LLflee] intent={intent} room={currentRoom.roomID} AA={aaPresent} fleeing={fleeing} " +
                              $"movementTarget={mtName} proxyTarget={(think != null ? think.ProxyTarget?.name : "null")} " +
                              $"self이동={moved:0.00} proxy이동={proxyMoved:0.00} dist(self→target)={(mt != null ? Vector3.Distance(selfP, mt.position) : -1):0.0}", this);
                }
                if (wasFleeing != fleeing)
                    Debug.Log($"[LLflee] {name}: fleeing {wasFleeing}→{fleeing}, room={currentRoom.roomID}, AA={aaPresent}", this);
                wasFleeing = fleeing;
            }
            yield return wait;
        }
    }

    private Vector3 _dbgLastPos, _dbgLastProxy;

    private IEnumerator Lbehaviour()
    {
        while (currentRoom == null) yield return null;

        while (!IsDead)
        {
            // 조종 중 / 비활성 방 / 합성 중 / 같은 방에 AA 있으면 생산 안 함
            if (!IsControlled
                && currentRoom != null && currentRoom.isActive
                && intent != CreatureIntent.Synthesizing
                && !(stopProduceWhenAA && AAInRoom()))
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

        c.intent = CreatureIntent.ProducedBy;                    // 생산 중 상태 (Think 정지, intent 유지)
        c.SetMovementEnabled(false);                             // 정지(kinematic) — reparent 전
        // 생산 중엔 LL(소환 위치)의 자식 → LL 따라감. 위치 지정이 있으면 그 point, 없으면 LL 본체.
        Transform holdParent = point != null ? point : (rootTransform != null ? rootTransform : transform);
        obj.transform.SetParent(holdParent, true);

        currentRoom.RegisterCreature(c);
        Popup.Instance?.BurstMessage(c, "produce");              // 생산 연출 (색은 Popup 메시지표에서)
        StartCoroutine(HoldThenRelease(c));
    }

    // spawnPoints 중 랜덤 (비어있으면 null → 자기 위치)
    private Transform PickSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        return null;
    }

    // 생산 중(ProducedBy): LL 자식으로 따라가며 holdTime 대기 → 끝나면 _Creatures로 옮기고 Wander로 놓음.
    private IEnumerator HoldThenRelease(Creature c)
    {
        if (c == null) yield break;
        // intent=ProducedBy, 정지, LL 자식 상태는 SpawnAtRandomPoint에서 이미 세팅됨.

        // 몸체(Rigidbody)들의 초기 로컬 위치/회전 기억 → hold 동안 강체처럼 spawnPoint에 고정.
        // (방 활성 전환 등으로 kinematic이 풀려도 매 프레임 복원해 이탈 방지)
        Rigidbody[] bodies = c.GetComponentsInChildren<Rigidbody>(true);
        var localPos = new Vector3[bodies.Length];
        var localRot = new Quaternion[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null) continue;
            localPos[i] = bodies[i].transform.localPosition;
            localRot[i] = bodies[i].transform.localRotation;
        }

        float t = 0f;
        while (t < holdTime)
        {
            if (c == null || c.IsDead) yield break;

            for (int i = 0; i < bodies.Length; i++)
            {
                var rb = bodies[i];
                if (rb == null) continue;
                if (!rb.isKinematic) rb.isKinematic = true;       // 풀렸으면 재고정
                rb.transform.localPosition = localPos[i];         // 이탈분 복원 (spawnPoint 기준 제자리)
                rb.transform.localRotation = localRot[i];
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (c == null || c.IsDead) yield break;

        c.transform.SetParent(CreatureRoot.Container, true);   // 놓을 때 _Creatures 자식으로 (LL 안 따라감)
        c.intent = CreatureIntent.Wander;                      // 생산 종료 → 배회 (Think이 이어받음)
        c.SetMovementEnabled(true);                            // 자유롭게 흐름
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
