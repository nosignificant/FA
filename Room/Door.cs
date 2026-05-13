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
    public Room roomA;
    public Room roomB;
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

    public Room GetOtherRoom(Room from)
    {
        if (from == roomA) return roomB;
        if (from == roomB) return roomA;
        return null;
    }

    void Awake()
    {
        // roomA가 비어있으면 부모에서 자동 할당
        if (roomA == null) roomA = GetComponentInParent<Room>();
        if (self == null) self = GetComponent<Creature>();
    }

    void Start()
    {
        originalUpperY = upperDoor.position.y;
        originalLowerY = lowerDoor.position.y;

        // 양쪽 방에 등록
        if (roomA != null) roomA.RegisterCreature(self);
        if (roomB != null && roomB != roomA) roomB.RegisterCreature(self);

        Debug.Log($"[Door {name}] Start. A={roomA?.roomID ?? "NULL"}, B={roomB?.roomID ?? "NULL"}, observingC={roomCondition.observingC?.name ?? "NULL"}");
    }

    private void OnEnable()
    {
        if (roomA == null) roomA = GetComponentInParent<Room>();
        if (roomA != null) roomA.OnCreatureDecomposed += OnRoomADecomposed;
        if (roomB != null) roomB.OnCreatureDecomposed += OnRoomBDecomposed;

        EvaluateConditions();
    }

    private void OnDisable()
    {
        if (roomA != null) roomA.OnCreatureDecomposed -= OnRoomADecomposed;
        if (roomB != null) roomB.OnCreatureDecomposed -= OnRoomBDecomposed;
    }

    private void EvaluateConditions()
    {
        if (roomCondition.observingC == null) return;

        if (roomA != null)
        {
            var (best, diff) = roomA.MostDecomposedAndSecond();
            conditionA = CheckCondition(best, diff, roomCondition);
        }
        if (roomB != null)
        {
            var (best, diff) = roomB.MostDecomposedAndSecond();
            conditionB = CheckCondition(best, diff, roomCondition);
        }
        if (conditionA || conditionB) DoorCloseAndOpen(true);
    }

    private void OnRoomADecomposed(Creature creature, CreatureID decomposerID)
    {
        if (decomposerID != CreatureID.D) return;
        if (creature.data != roomCondition.observingC) return;

        var (best, diff) = roomA.MostDecomposedAndSecond();
        conditionA = CheckCondition(best, diff, roomCondition);
        DoorCloseAndOpen(conditionA || conditionB);
    }

    private void OnRoomBDecomposed(Creature creature, CreatureID decomposerID)
    {
        if (decomposerID != CreatureID.D) return;
        if (creature.data != roomCondition.observingC) return;

        var (best, diff) = roomB.MostDecomposedAndSecond();
        conditionB = CheckCondition(best, diff, roomCondition);
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
