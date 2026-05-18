using System;
using System.Collections;
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

    private void Start()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return;
        if (needToSpawn)
        {
            tentacleGrab.reservedForSpawn = spawnCreatureAtTentacleIndex;
            StartCoroutine(Lbehaviour());
        }
    }

    private IEnumerator Lbehaviour()
    {
        if (spawnCreatureAtTentacleIndex >= tentacleGrab.tentacles.Length) yield break;

        while (currentRoom == null || !currentRoom.isActive)
            yield return null;

        while (!IsDead)
        {
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
        c.SetGrabImmunity(10f);
    }

    public GameObject WhichOneSpawn(CreatureID idx)
    {
        if (currentRoom == null || currentRoom.creatureDB == null) return null;
        return currentRoom.creatureDB.GetPrefab(idx);
    }

    public void SetLSpawnCreature(CreatureID idx) { currentSpawn = idx; }
}
