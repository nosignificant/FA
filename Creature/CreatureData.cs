using UnityEngine;
using CreatureTypes;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature Data")]

public class CreatureData : ScriptableObject
{
    public string creatureName;
    public CreatureID creatureID;
    public GameObject prefab;
    [Tooltip("스폰 시 homeBound 바닥 기준 Y 오프셋 (특정 높이에서 시작해야 하는 생물용)")]
    public float spawnYOffset = 0f;

    public bool canAttackThis = true;
    public bool isGrabable = true;

    [Header("stat")]
    public int maxHP;
    public float weight = 1.0f;
    public float fleeWeight = 1f;
    public float chaseWeight = 1f;
}
