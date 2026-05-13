using UnityEngine;
using System.Collections;
using CreatureTypes;

public class TentacleGrab2 : MonoBehaviour
{
    public Creature self;
    public CreatureID forcedTargetID = CreatureID.Player;

    [System.Serializable]
    public struct TentacleSlot
    {
        public Tentacle tentacle;
        public Transform oldTarget;
        public bool isGrabbing;
        public bool isPending;
        public Creature grabbedCreature;
    }
    public TentacleSlot[] tentacles;
    public int reservedForSpawn = -1;
    public float holdTime = 2f;


    void Start()
    {
        self = GetComponentInParent<Creature>();
        initTentacles();

    }

    public void TryGrab(Creature target)
    {
        if (target.IsDead || target.data == null) return;
        if (!target.data.isGrabable) return;
        if (target.intent == CreatureIntent.Grabbed || target.intent == CreatureIntent.Decomposing) return;
        if (forcedTargetID != CreatureID.Player && target.data.creatureID != forcedTargetID) return;
        if (!self.HasAction(target.data.creatureID, InteractionAction.Grab)) return;

        // 비어있는 슬롯에 잡기 시도
        for (int i = 0; i < tentacles.Length; i++)
        {
            if (tentacles[i].isGrabbing || tentacles[i].isPending) continue;
            if (i == reservedForSpawn) continue;
            if (IsTargetedByOtherSlot(i, target)) continue;

            tentacles[i].tentacle.target = target.rootTransform;
            StartCoroutine(GrabAfterDelay(i, target));
            break;
        }
    }

    bool IsTargetedByOtherSlot(int selfIndex, Creature c)
    {
        for (int i = 0; i < tentacles.Length; i++)
        {
            if (i == selfIndex) continue;
            if (tentacles[i].grabbedCreature == c) return true;
        }
        return false;
    }

    IEnumerator GrabAfterDelay(int i, Creature c)
    {
        tentacles[i].isPending = true;

        yield return new WaitForSeconds(holdTime);

        tentacles[i].isPending = false;
        if (c == null || c.IsDead || c.intent == CreatureIntent.Grabbed) yield break;
        AttachToSlot(i, c);
    }

    public void AttachToSlot(int i, Creature c)
    {
        tentacles[i].isGrabbing = true;
        tentacles[i].grabbedCreature = c;
        tentacles[i].tentacle.target = tentacles[i].oldTarget;

        c.OnGrabbed(tentacles[i].tentacle.foot);
    }

    public void ReleaseSlot(int i)
    {
        Creature c = tentacles[i].grabbedCreature;
        if (c == null) return;

        tentacles[i].isGrabbing = false;
        tentacles[i].grabbedCreature = null;

        // 풀려나는 쪽한테 위임
        c.OnReleased();
    }


    private void initTentacles()
    {
        Tentacle[] found = GetComponentsInChildren<Tentacle>();
        tentacles = new TentacleSlot[found.Length];
        for (int i = 0; i < found.Length; i++)
        {
            tentacles[i].tentacle = found[i];
            tentacles[i].isGrabbing = false;
            tentacles[i].isPending = false;
            tentacles[i].grabbedCreature = null;
            tentacles[i].oldTarget = found[i].target;
        }
    }
}
