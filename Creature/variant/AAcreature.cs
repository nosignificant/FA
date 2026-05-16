using System.Collections;
using System.Linq;
using UnityEngine;
using CreatureTypes;

public class AACreature : TentacleCreature
{
    [Header("AABehavior")]
    public float migrateThreshold = 5f;
    public float forgetDelay = 10f;

    private void Start()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return;

        migrateThreshold += Random.Range(-1.5f, 3.5f);
        StartCoroutine(AAbehaviour());
    }

    private IEnumerator AAbehaviour()
    {
        while (!IsDead)
        {
            if (intent == CreatureIntent.Wander)
            {
                // 인접한 방 중 하나라도 L이 있으면 이주 확률 증가
                bool anyAdjacentHasL = currentRoom != null && currentRoom.doors.Any(d =>
                {
                    Room other = d?.GetOtherRoom(currentRoom);
                    return other != null && other.creatureList.Any(x =>
                        x != null && x.data != null && x.data.creatureID == CreatureID.L);
                });

                // threshold가 낮을수록 잘 옮김. L 있으면 threshold 효과적으로 -3
                float effective = anyAdjacentHasL ? migrateThreshold - 3f : migrateThreshold;
                bool goNow = Random.Range(1, 11) > effective;

                if (goNow) wantToMigrate = true;
                yield return new WaitForSeconds(forgetDelay);
                wantToMigrate = false;
            }
            else
            {
                yield return null;
            }
        }
    }
}
