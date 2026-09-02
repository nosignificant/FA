using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using CreatureTypes;

public class Lcreature : TentacleCreature
{
    [Header("LBehavior")]

    public float releaseInterval = 13f;
    public float refillDelay = 3f;
    public bool needToSpawn = false;
    public int spawnCreatureAtTentacleIndex = 0;

    public CreatureID currentSpawn;

    [Header("AA 회피")]
    [Tooltip("같은 방에 AA가 있으면 무조건 다른 방으로 이동")]
    public bool fleeFromAA = true;
    public float fleeCheckInterval = 0.4f;

    private void Start()
    {
        // L/LL 둘 다: 같은 방에 AA 있으면 다른 방으로 이동
        if (fleeFromAA) StartCoroutine(FleeAALoop());
        StartCoroutine(KeepPluggedLoop());   // 꽂힌 생물 붙잡아 두기
        StartCoroutine(AutoPlugLoop());      // AA가 가까워지면 스스로 꽂힘

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
            // 기생(플러그) 중엔 AA를 피하지 않음
            if (tc != null && !HasPlugged && currentRoom != null && currentRoom.HasSpecies(CreatureID.AA))
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
            if (d == null) continue;
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
            // 비활성 방은 생산 안 함 (활성화되면 텐타클로 정상 생산). 랜덤 위치 생성 없음.
            if (currentRoom == null || !currentRoom.isActive)
            {
                yield return null;
                continue;
            }

            if (intent == CreatureIntent.Synthesizing)
            {
                yield return null;
                continue;
            }

            // 도망 중엔 생산 안 함 (헷갈림 방지)
            if (intent == CreatureIntent.Flee)
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
        // 꽂혀있으면 지정 종(A), 아니면 H/S 랜덤
        CreatureID toSpawn = HasPlugged ? currentSpawn
                           : (UnityEngine.Random.value < 0.5f ? CreatureID.H : CreatureID.S);
        GameObject spawnThis = WhichOneSpawn(toSpawn);
        if (spawnThis == null)
        {
            Debug.LogWarning($"[Lcreature] {name}: creatureDB에서 {toSpawn} prefab을 못 찾음 (DB 등록 확인)");
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

        // 풀리자마자 다시 잡히는 것 방지 — 10초 grab 면역
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

    // ── 플러그: 생물을 조종해 L에 꽂으면 출력 종이 바뀜 (AA→A, 나머지는 그 종) ──
    [Header("plug (출력 프로그래밍)")]
    [Tooltip("아무것도 안 꽂았을 때 기본 생산 종")]
    public CreatureID defaultSpawn = CreatureID.H;

    // HUD용: 지금 뭘 꽂았고 뭘 뽑는지
    public CreatureID pluggedInput { get; private set; } = CreatureID.H;

    // 생산기(L)만 플러그 대상. LL(합성기)은 제외
    public bool IsProducer => data != null && data.creatureID == CreatureID.L;

    // 현재 꽂혀있는 생물 (AA 등). 있으면 '기생' 상태 — L은 이 동안 AA를 안 피함.
    private Creature pluggedCreature;
    public bool HasPlugged => pluggedCreature != null && !pluggedCreature.IsDead;
    public Creature PluggedCreature => pluggedCreature;

    public void PlugCreature(Creature c)
    {
        if (!IsProducer || c == null || c.data == null) return;
        pluggedCreature = c;
        c.isPlugged = true;         // 이 동안 AA는 L을 쫓지 않음
        pluggedInput = c.data.creatureID;
        SetLSpawnCreature(MapPlug(c.data.creatureID));
        c.SetGrabImmunity(9999f);   // 꽂혀있는 동안 다른 데 안 잡히게
        SetIgnoreCollisionWith(c, true);   // 서로 밀어내지 않게 (맵 이탈 방지)

        Popup.Instance?.BurstMessage(this, "connected", Color.green);
    }

    public void Unplug()
    {
        if (pluggedCreature != null)
        {
            pluggedCreature.isPlugged = false;
            SetIgnoreCollisionWith(pluggedCreature, false);   // 충돌 복구
        }
        pluggedCreature = null;
        pluggedInput = defaultSpawn;
        SetLSpawnCreature(defaultSpawn);
    }

    private void SetIgnoreCollisionWith(Creature c, bool ignore)
    {
        if (c == null) return;
        var mine = GetComponentsInChildren<Collider>();
        var theirs = c.GetComponentsInChildren<Collider>();
        for (int i = 0; i < mine.Length; i++)
            for (int j = 0; j < theirs.Length; j++)
                if (mine[i] != null && theirs[j] != null)
                    Physics.IgnoreCollision(mine[i], theirs[j], ignore);
    }

    [Tooltip("이 반경 안에 AA가 오면 자동으로 꽂힘")]
    public float autoPlugRange = 3f;

    // AA가 스스로 다가와 가까워지면 자동으로 꽂힘 (플레이어 조작 불필요)
    private IEnumerator AutoPlugLoop()
    {
        var wait = new WaitForSeconds(0.3f);
        while (!IsDead)
        {
            if (IsProducer && !HasPlugged)
            {
                Creature aa = FindNearbyAA();
                if (aa != null) PlugCreature(aa);
            }
            yield return wait;
        }
    }

    private Creature FindNearbyAA()
    {
        var hits = Physics.OverlapSphere(transform.position, autoPlugRange);
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i].GetComponentInParent<Creature>();
            if (c != null && !c.IsDead && c.data != null && c.data.creatureID == CreatureID.AA)
                return c;
        }
        return null;
    }

    // 꽂혀있는 동안 L이 그 생물을 따라다님 (proxy를 꽂힌 생물로 고정 = 기생)
    private IEnumerator KeepPluggedLoop()
    {
        var myTC = GetComponent<TargetControl>();
        var wait = new WaitForSeconds(0.2f);
        while (!IsDead)
        {
            if (HasPlugged)
            {
                // 꽂힌 AA가 이 방을 벗어나면 플러그 해제 (L은 방 안에 남음, 따라 나가지 않음)
                if (pluggedCreature.currentRoom != currentRoom)
                {
                    Unplug();
                }
                else
                {
                    if (myTC != null) myTC.SetMovementTarget(pluggedCreature.transform);
                    PointPluggedTentacleAtMe();   // AA 촉수 하나가 L을 계속 향하게
                }
            }
            else if (pluggedCreature != null && pluggedCreature.IsDead)
            {
                Unplug();   // 꽂힌 게 죽으면 기본 생산으로
            }
            yield return wait;
        }
    }

    // 꽂힌 생물(AA)의 촉수 하나가 L(나)을 계속 향하게 — grab 타겟 방식 재활용
    private void PointPluggedTentacleAtMe()
    {
        var tent = pluggedCreature as TentacleCreature;
        if (tent == null || tent.tentacleGrab == null) return;

        var slots = tent.tentacleGrab.tentacles;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].tentacle == null) continue;
            if (slots[i].isGrabbing) continue;        // 잡는 중인 촉수는 건드리지 않음
            slots[i].tentacle.target = transform;     // 안 잡는 촉수 하나가 L을 향함
            return;
        }
    }

    private CreatureID MapPlug(CreatureID plugged)
    {
        switch (plugged)
        {
            case CreatureID.AA: return CreatureID.A;   // AA 꽂으면 A 생산
            default: return plugged;                    // 나머지는 그 종 그대로 생산
        }
    }
}
