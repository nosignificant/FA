using System.Linq;
using UnityEngine;
using CreatureTypes;

public class AACreature : TentacleCreature
{
    [Header("AABehavior")]
    [Range(0f, 1f)] public float lNearbyChanceBonus = 0.3f;

    // 인접한 방에 L이 있으면 이주 확률 ↑
    protected override float GetMigrateChance()
    {
        float baseChance = base.GetMigrateChance();

        bool anyAdjacentHasL = currentRoom != null && currentRoom.doors.Any(d =>
        {
            Room other = d != null ? d.GetOtherRoom(currentRoom) : null;
            return other != null && other.creatureList.Any(x =>
                x != null && x.data != null && x.data.creatureID == CreatureID.L);
        });

        return anyAdjacentHasL
            ? Mathf.Clamp01(baseChance + lNearbyChanceBonus)
            : baseChance;
    }
}
