using System;
using System.Collections;
using UnityEngine;
using CreatureTypes;

public class Door : MonoBehaviour
{
    [Header("References")]
    public Room roomA;
    public Room roomB;
    public Creature self;
    public GameObject light;
    public Rotate rot;
    public Rotate rot2;

    [Header("Door")]
    public Collider playerBlockCollider;
    public float moveDist = 10f;
    public Transform upperDoor;
    public Transform lowerDoor;

    [Header("Condition")]
    public CreatureData watchingCreature;

    [Header("State")]
    public bool isOpen = false;
    private bool conditionA = false;
    private bool conditionB = false;
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

        if (upperDoor != null) originalUpperY = upperDoor.position.y;
        if (lowerDoor != null) originalLowerY = lowerDoor.position.y;
    }

    void Start()
    {
        if (roomA == null) Debug.LogWarning($"[Door {name}] roomA 미설정");
        if (roomB == null) Debug.LogWarning($"[Door {name}] roomB 미설정 (인스펙터/RoomEditor에서 할당 필요)");

        if (roomA != null) roomA.RegisterCreature(self);
        if (roomB != null && roomB != roomA) roomB.RegisterCreature(self);

        roomA?.RegisterDoor(this);
        roomB?.RegisterDoor(this);

        // 조명을 현재 isOpen 상태에 맞춰 초기화 (닫힌 채 시작 시 조명 꺼짐)
        if (light != null) light.SetActive(isOpen);
        ApplyRotate(isOpen);

        // 인스펙터에서 isOpen을 켜둔 채 실행하면 열린 상태로 시작하도록 동기화
        if (isOpen) DoorCloseAndOpen(true);
    }

    private void OnEnable()
    {
        if (roomA == null) roomA = GetComponentInParent<Room>();
        // 생물 수가 바뀔 때마다 문 조건 재평가 (분해가 아니라 '방에 가장 많은 종' 기준)
        if (roomA != null) roomA.OnCreatureCountChanged += Reevaluate;
        if (roomB != null) roomB.OnCreatureCountChanged += Reevaluate;

        DoorManager.Instance.Register(this);

        EvaluateConditions();
    }

    private void OnDisable()
    {
        if (roomA != null) roomA.OnCreatureCountChanged -= Reevaluate;
        if (roomB != null) roomB.OnCreatureCountChanged -= Reevaluate;

        if (DoorManager.Existing != null) DoorManager.Existing.Unregister(this);   // 정리 중 재생성 방지
    }

    private void Reevaluate() => EvaluateConditions();

    private void EvaluateConditions()
    {
        if (watchingCreature == null) return;   // 조건 없는 문(튜토리얼 등)은 자동 개폐 안 함

        conditionA = roomA != null && CheckCondition(roomA.MostNumerousSpecies());
        conditionB = roomB != null && CheckCondition(roomB.MostNumerousSpecies());

        bool shouldOpen = conditionA || conditionB;
        if (shouldOpen != isOpen) DoorCloseAndOpen(shouldOpen);   // 상태 바뀔 때만
    }

    // 방에 가장 많은 종이 이 문이 요구하는 종과 같은가
    private bool CheckCondition(CreatureData best)
    {
        return best != null && best == watchingCreature;
    }

    private void ApplyRotate(bool open)
    {
        if (rot != null) rot.isSelfRotate = open;
        if (rot2 != null) rot2.isSelfRotate = open;
    }

    public void DoorCloseAndOpen(bool open)
    {
        bool changed = isOpen != open;
        isOpen = open;
        if (playerBlockCollider != null)
            playerBlockCollider.enabled = !open;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(open));

        //불 켜기 
        if (light != null) light.SetActive(open);
        //회로 연결한 척 하기
        ApplyRotate(open);
        if (RoomManager.Instance != null && Player.Instance?.currentRoom != null)
            RoomManager.Instance.UpdateActiveRooms(Player.Instance.currentRoom);

        // 열린 문 구성이 실제로 바뀌었으면 종별 카운트 갱신 알림
        if (changed && DoorManager.Existing != null) DoorManager.Existing.NotifyDoorChanged();
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
