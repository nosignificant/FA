using System;
using System.Collections;
using UnityEngine;
using CreatureTypes;

public class Door : MonoBehaviour
{
    [Serializable]
    public struct Condition
    {
        public CreatureData observingC;
        public int howManyMore;
    }

    [Header("References")]
    public Room room;
    public Room nextRoom;
    public Creature self;

    [Header("Door")]
    public Collider playerBlockCollider;
    public float moveDist = 10f;
    public Transform upperDoor;
    public Transform lowerDoor;

    [Header("State")]
    public bool isOpen = false;
    private bool conditionA = false;
    private bool conditionB = false;
    public Condition roomCondition;
    private float originalUpperY;
    private float originalLowerY;

    void Awake()
    {
        if (room == null) room = GetComponentInParent<Room>();
        if (self == null) self = GetComponent<Creature>();
    }

    void Start()
    {
        originalUpperY = upperDoor.position.y;
        originalLowerY = lowerDoor.position.y;

        if (room != null) room.RegisterCreature(self);

        Debug.Log($"[Door {name}] Start. room={room?.roomID ?? "NULL"}, nextRoom={nextRoom?.roomID ?? "NULL"}, observingC={roomCondition.observingC?.name ?? "NULL"}, howManyMore={roomCondition.howManyMore}");
    }

    // 구독은 OnEnable/OnDisable에서 (RoomManager가 켜고 껐다 해도 안전하게)
    private void OnEnable()
    {
        if (room == null) room = GetComponentInParent<Room>();
        if (room != null) room.OnCreatureDecomposed += OnRoomDecomposed;
        if (nextRoom != null) nextRoom.OnCreatureDecomposed += OnNextRoomDecomposed;

        // 비활성 동안 놓친 분해 이벤트 보완 → 현재 카운트로 조건 재평가
        EvaluateConditions();
    }

    private void EvaluateConditions()
    {
        Debug.Log($"[Door {name}] EvaluateConditions called. observingC={roomCondition.observingC?.name ?? "NULL"}");
        if (roomCondition.observingC == null) return;

        if (room != null)
        {
            var (best, diff) = room.MostDecomposedAndSecond();
            conditionA = CheckCondition(best, diff, roomCondition);
            Debug.Log($"[Door {name}] room({room.roomID}): best={best?.name ?? "NULL"}, diff={diff}, conditionA={conditionA}");
        }
        if (nextRoom != null)
        {
            var (best, diff) = nextRoom.MostDecomposedAndSecond();
            conditionB = CheckCondition(best, diff, roomCondition);
            Debug.Log($"[Door {name}] nextRoom({nextRoom.roomID}): best={best?.name ?? "NULL"}, diff={diff}, conditionB={conditionB}");
        }
        Debug.Log($"[Door {name}] final: A={conditionA} B={conditionB} → open={(conditionA || conditionB)}");
        if (conditionA || conditionB) DoorCloseAndOpen(true);
    }

    private void OnDisable()
    {
        if (room != null) room.OnCreatureDecomposed -= OnRoomDecomposed;
        if (nextRoom != null) nextRoom.OnCreatureDecomposed -= OnNextRoomDecomposed;
    }

    private void OnRoomDecomposed(Creature creature, CreatureID decomposerID)
    {
        Debug.Log($"[Door {name}] OnRoomDecomposed fired. decomposerID={decomposerID}, creature.data={creature?.data?.name}, condition.observingC={roomCondition.observingC?.name}");
        if (decomposerID != CreatureID.D) { Debug.Log($"[Door {name}] reject: decomposerID != D"); return; }
        if (creature.data != roomCondition.observingC) { Debug.Log($"[Door {name}] reject: data mismatch"); return; }

        var (best, diff) = room.MostDecomposedAndSecond();
        Debug.Log($"[Door {name}] best={best?.name}, diff={diff}, required={roomCondition.howManyMore}");
        conditionA = CheckCondition(best, diff, roomCondition);
        Debug.Log($"[Door {name}] conditionA={conditionA}, will open: {(conditionA || conditionB)}");

        DoorCloseAndOpen(conditionA || conditionB);
    }

    private void OnNextRoomDecomposed(Creature creature, CreatureID decomposerID)
    {
        Debug.Log($"[Door {name}] OnNextRoomDecomposed fired. decomposerID={decomposerID}, creature.data={creature?.data?.name}");
        if (decomposerID != CreatureID.D) return;
        if (creature.data != roomCondition.observingC) return;

        var (best, diff) = nextRoom.MostDecomposedAndSecond();
        conditionB = CheckCondition(best, diff, roomCondition);
        Debug.Log($"[Door {name}] (next) best={best?.name}, diff={diff}, conditionB={conditionB}");

        DoorCloseAndOpen(conditionA || conditionB);
    }

    private bool CheckCondition(CreatureData best, int diff, Condition condition)
    {
        return best == condition.observingC && diff >= condition.howManyMore;
    }

    public void DoorCloseAndOpen(bool open)
    {
        isOpen = open;
        if (playerBlockCollider != null)
            playerBlockCollider.enabled = !open;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(open));

        if (RoomManager.Instance != null && Player.Instance?.currentRoom != null)
            RoomManager.Instance.UpdateActiveRooms(Player.Instance.currentRoom);
    }

    private IEnumerator MoveDoor(bool open)
    {
        float upperTarget = open ? originalUpperY + moveDist : originalUpperY;
        float lowerTarget = open ? originalLowerY - moveDist : originalLowerY;

        float elapsed = 0f;
        float duration = 1.0f;

        Vector3 upperStart = upperDoor.position;
        Vector3 lowerStart = lowerDoor.position;
        Vector3 upperEnd = new Vector3(upperDoor.position.x, upperTarget, upperDoor.position.z);
        Vector3 lowerEnd = new Vector3(lowerDoor.position.x, lowerTarget, lowerDoor.position.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            upperDoor.position = Vector3.Lerp(upperStart, upperEnd, t);
            lowerDoor.position = Vector3.Lerp(lowerStart, lowerEnd, t);
            yield return null;
        }

        upperDoor.position = upperEnd;
        lowerDoor.position = lowerEnd;
    }
}
