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

    // roomB가 비어있으면 문 위치 기준으로 반대쪽 방 자동 탐색
    private void ResolveRoomB()
    {
        if (roomB != null) return;

        Vector3 p = transform.position;
        Room best = null;
        float bestDist = float.MaxValue;

        foreach (var r in FindObjectsOfType<Room>())
        {
            if (r == null || r == roomA) continue;
            Collider col = r.homeBound != null ? r.homeBound : r.GetComponent<Collider>();
            if (col == null) continue;

            // 문 위치를 포함하거나 가장 가까운 방
            float d = (col.ClosestPoint(p) - p).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = r; }
        }

        // 너무 멀면(인접 아님) 연결 안 함
        if (best != null && bestDist <= 4f * 4f) roomB = best;
    }

    void Start()
    {
        originalUpperY = upperDoor.position.y;
        originalLowerY = lowerDoor.position.y;

        ResolveRoomB();   // roomB 자동 연결

        // 양쪽 방에 creature 등록
        if (roomA != null) roomA.RegisterCreature(self);
        if (roomB != null && roomB != roomA) roomB.RegisterCreature(self);

        // 양쪽 방의 doors 리스트에 자기 등록 (비대칭 연결 방지 — 이주가 양방향으로 됨)
        if (roomA != null && roomA.doors != null && !roomA.doors.Contains(this))
            roomA.doors.Add(this);
        if (roomB != null && roomB.doors != null && !roomB.doors.Contains(this))
            roomB.doors.Add(this);

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
        if (roomA != null)
        {
            var (best, diff) = roomA.MostDecomposedAndSecond();
            conditionA = CheckCondition(best, diff);
        }
        if (roomB != null)
        {
            var (best, diff) = roomB.MostDecomposedAndSecond();
            conditionB = CheckCondition(best, diff);
        }
        if (conditionA || conditionB) DoorCloseAndOpen(true);
    }

    private void OnRoomADecomposed(Creature creature, CreatureID decomposerID)
    {
        if (decomposerID != CreatureID.D) return;

        var (best, diff) = roomA.MostDecomposedAndSecond();
        conditionA = CheckCondition(best, diff);
        DoorCloseAndOpen(conditionA || conditionB);
    }

    private void OnRoomBDecomposed(Creature creature, CreatureID decomposerID)
    {
        if (decomposerID != CreatureID.D) return;

        var (best, diff) = roomB.MostDecomposedAndSecond();
        conditionB = CheckCondition(best, diff);
        DoorCloseAndOpen(conditionA || conditionB);
    }

    // 방의 최다 분해 생물이 이 문의 observingC면 열림 (1·2위 차 howManyMore 이상)
    private bool CheckCondition(CreatureData best, int diff)
    {
        return best != null
            && best == roomCondition.observingC
            && diff >= roomCondition.howManyMore;
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
