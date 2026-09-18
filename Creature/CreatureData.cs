using UnityEngine;
using CreatureTypes;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature Data")]

public class CreatureData : ScriptableObject
{
    public string creatureName;
    public CreatureID creatureID;
    public GameObject prefab;
    public float spawnYOffset = 0f;
    public bool isGrabable = true;
    public bool lockable = true;       // 플레이어가 락온(관찰) 가능 여부
    public bool controllable = true;   // 플레이어 조종(possess) 가능 여부
    public bool advancesStory = false; // 이 종을 빙의하면 스토리 단계 +1 (개체당 1회)
    public bool dieOnPossess = false;  // 빙의 시 조종 없이 즉시 사망 (일회성 소모)
    [Tooltip("방 생물 목록·문 열림 카운트에서 제외 (weed 등). D처럼 세지 않을 종")]
    public bool excludeFromRoomCount = false;

    public bool walkingCreature = false;

    [Header("방 상태 각성 게이트")]
    [Tooltip("켜면 방이 requiredRoomState일 때만 각성(그 외엔 휴면)")]
    public bool gateByRoomState = false;
    [Tooltip("이 방 상태에서만 각성 (gateByRoomState 켜졌을 때)")]
    public Room.RoomActivation requiredRoomState = Room.RoomActivation.A;

    [Header("stat")]
    public float weight = 1.0f;
    public float fleeWeight = 1f;
    public float chaseWeight = 1f;
    public float wanderWeight = 1.0f;
}
