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

    [Header("learn (관찰 학습)")]
    [Tooltip("이 생물의 대표 행동 — 이 intent로 진입하는 걸 목격하면 1회 카운트")]
    public CreatureIntent signatureIntent = CreatureIntent.Wander;
    [Tooltip("이 횟수만큼 대표 행동을 목격하면 변신 폼 학습")]
    public int observationsToLearn = 1;

    [Header("stat")]
    public int maxHP;
    public float weight = 1.0f;
    public float fleeWeight = 1f;
    public float chaseWeight = 1f;
}
