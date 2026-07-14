using UnityEngine;
using CreatureTypes;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature Data")]

public class CreatureData : ScriptableObject
{
    public string creatureName;
    public CreatureID creatureID;
    public GameObject prefab;
    public float spawnYOffset = 0f;
    public bool canAttackThis = true;
    public bool isGrabable = true;
    public bool controllable = true;   // 플레이어 조종(possess) 가능 여부
    public bool advancesStory = false; // 이 종을 빙의하면 스토리 단계 +1 (개체당 1회)
    public bool dieOnPossess = false;  // 빙의 시 조종 없이 즉시 사망 (일회성 소모)

    public bool walkingCreature = false;

    [Header("stat")]
    public int maxHP;
    public float weight = 1.0f;
    public float fleeWeight = 1f;
    public float chaseWeight = 1f;
    public float wanderWeight = 1.0f;
}
