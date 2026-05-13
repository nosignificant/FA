using UnityEngine;
using CreatureTypes;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature Data")]

public class CreatureData : ScriptableObject
{
    public string creatureName;
    public CreatureID creatureID;

    public bool canAttackThis = true;
    public bool isGrabable = true;

    [Header("stat")]
    public int maxHP;
    public float weight = 1.0f;
    public float fleeWeight = 1f;
    public float chaseWeight = 1f;
}
