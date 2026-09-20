using System.Collections;
using UnityEngine;
using CreatureTypes;

// 생산기 (LL 프리팹에 부착): 촉수 발로 L 입자를 계속 생산. 같은 방에 AA가 있으면 회피 이동.
public class Lcreature : TentacleCreature
{
    [Header("LBehavior")]
    public float releaseInterval = 13f;
    public float refillDelay = 3f;
    public bool needToSpawn = false;
    public int spawnCreatureAtTentacleIndex = 0;

    [Tooltip("생산하는 입자 (기본 L)")]
    public CreatureID currentSpawn = CreatureID.L;

    [Header("AA 회피")]
    [Tooltip("같은 방에 AA가 있으면 다른 방으로 이동")]
    public bool fleeFromAA = true;
    public float fleeCheckInterval = 0.4f;

    // HUD: 이 개체가 생산기인지
    public bool IsProducer => needToSpawn;

    private void Start()
    {
        if (fleeFromAA) StartCoroutine(FleeAALoop());

        if (tentacleGrab == null || tentacleGrab.tentacles == null) return;
        if (needToSpawn)
        {
            tentacleGrab.reservedForSpawn = spawnCreatureAtTentacleIndex;
            tentacleGrab.forcedTargetID = currentSpawn;
            StartCoroutine(Lbehaviour());
        }
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

    // AA 없는 인접 방 우선, 없으면 아무 문 너머 방으로
    private Transform PickExitTargetAwayFromAA()
    {
        if (currentRoom == null || currentRoom.doors == null) return null;

        Room best = null, fallback = null;
        foreach (var d in currentRoom.doors)
        {
            if (d == null || !d.isOpen) continue;   // 닫힌 문으로는 못 감
            Room other = d.GetOtherRoom(currentRoom);
            if (other == null) continue;
            fallback = other;
            if (!other.HasSpecies(CreatureID.AA)) { best = other; break; }
        }
        Room target = best != null ? best : fallback;
        return target != null ? target.transform : null;
    }

    private IEnumerator Lbehaviour()
    {
        if (spawnCreatureAtTentacleIndex >= tentacleGrab.tentacles.Length) yield break;

        while (currentRoom == null)
            yield return null;

        while (!IsDead)
        {
            // 조종 중엔 생산 정지 (촉수도 자유로워야 몸이 플레이어를 따라감)
            if (IsControlled)
            {
                yield return null;
                continue;
            }

            // 비활성 방은 생산 안 함
            if (currentRoom == null || !currentRoom.isActive)
            {
                yield return null;
                continue;
            }

            // Flee(도망) 중에도 L 생산은 계속 — 합성 중일 때만 정지
            if (intent == CreatureIntent.Synthesizing)
            {
                yield return null;
                continue;
            }

            Creature attached = SpawnAndAttach(spawnCreatureAtTentacleIndex);
            if (attached == null)
            {
                yield return new WaitForSeconds(refillDelay);
                continue;
            }

            float t = 0f;
            bool consumed = false;
            while (t < releaseInterval)
            {
                if (IsControlled) break;   // 조종 시작 → 들고 있던 스폰 놓고 촉수 해방 (아래 Release)
                t += Time.deltaTime;
                var slot = tentacleGrab.tentacles[spawnCreatureAtTentacleIndex];
                if (!slot.isGrabbing || slot.grabbedCreature == null || slot.grabbedCreature != attached)
                {
                    consumed = true;
                    break;
                }
                yield return null;
            }

            if (!consumed)
                ReleaseAttached(spawnCreatureAtTentacleIndex);

            yield return new WaitForSeconds(refillDelay);
        }
    }

    private Creature SpawnAndAttach(int idx)
    {
        GameObject spawnThis = WhichOneSpawn(currentSpawn);
        if (spawnThis == null)
        {
            Debug.LogWarning($"[Lcreature] {name}: creatureDB에서 {currentSpawn} prefab을 못 찾음 (DB 등록 확인)");
            return null;
        }
        if (idx < 0 || idx >= tentacleGrab.tentacles.Length) return null;

        ref var slot = ref tentacleGrab.tentacles[idx];
        if (slot.tentacle == null || slot.tentacle.foot == null) return null;

        // 생물이 죽어서 null이 됐으면 슬롯 정리
        if (slot.isGrabbing && slot.grabbedCreature == null)
        {
            slot.isGrabbing = false;
            slot.isPending = false;
        }

        if (slot.isGrabbing || slot.grabbedCreature != null) return null;

        Transform foot = slot.tentacle.foot;
        GameObject obj = Instantiate(spawnThis, foot.position, Quaternion.identity);
        Creature c = obj.GetComponent<Creature>();
        if (c == null) { Destroy(obj); return null; }

        currentRoom.RegisterCreature(c);

        tentacleGrab.AttachToSlot(idx, c);

        return c;
    }

    private void ReleaseAttached(int idx)
    {
        if (idx < 0 || idx >= tentacleGrab.tentacles.Length) return;

        ref var slot = ref tentacleGrab.tentacles[idx];
        Creature c = slot.grabbedCreature;

        slot.isGrabbing = false;
        slot.grabbedCreature = null;
        if (slot.tentacle != null) slot.tentacle.target = slot.oldTarget;

        if (c == null || c.IsDead) return;

        // AttachedTo의 정확한 역동작 (계층/컴포넌트/kinematic 복구)
        c.Release();

        // 풀리자마자 다시 잡히는 것 방지 — grab 면역
        c.SetGrabImmunity(2f);
    }

    public GameObject WhichOneSpawn(CreatureID idx)
    {
        if (currentRoom == null || currentRoom.creatureDB == null) return null;
        return currentRoom.creatureDB.GetPrefab(idx);
    }

    public void SetLSpawnCreature(CreatureID idx)
    {
        currentSpawn = idx;
        if (tentacleGrab != null) tentacleGrab.forcedTargetID = idx;
    }
}
