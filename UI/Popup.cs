using System.Collections;
using UnityEngine;
using CreatureTypes;

// 생물이 분해되면 그 위치에서 텍스트 여러 개가 랜덤 방향으로 터져나가는 연출.
// 방향은 카메라 정면 평면 위로 흩뿌려서 플레이어 시점에서 잘 퍼져 보이게 함.
public class Popup : MonoBehaviour
{
    [Header("Prefab")]
    public FloatingText popupPrefab;

    [Header("연출")]
    public string message = "분해됨";
    [Min(1)] public int burstCount = 5;
    public float speed = 2.5f;
    [Tooltip("속도 랜덤 편차 (0.3이면 ±30%)")]
    [Range(0f, 1f)] public float speedJitter = 0.3f;
    [Tooltip("생성 위치를 중심에서 이만큼 랜덤하게 흩음")]
    public float spawnScatter = 0.3f;
    public Vector3 offset = new Vector3(0f, 1f, 0f);

    private IEnumerator Start()
    {
        yield return null;

        if (RoomManager.Instance == null) yield break;

        foreach (var r in RoomManager.Instance.rooms.Values)
            if (r != null) r.OnCreatureDecomposed += OnDecomposed;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance == null) return;

        foreach (var r in RoomManager.Instance.rooms.Values)
            if (r != null) r.OnCreatureDecomposed -= OnDecomposed;
    }

    // 어느 방에서든 분해가 나면 그 위치에 터뜨림
    private void OnDecomposed(Creature target, CreatureID decomposerID) => Burst(target);

    private void Burst(Creature target)
    {
        if (popupPrefab == null || target == null) return;

        Transform t = target.rootTransform != null ? target.rootTransform : target.transform;
        Vector3 center = t.position + offset;

        Debug.Log($"[PopupDbg] {target.name} / root={t.name} rootPos={t.position} " +
                  $"creaturePos={target.transform.position} → center={center}");

        Camera cam = Camera.main;
        // 카메라 정면 평면의 축 (없으면 월드 축 폴백)
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;

        for (int i = 0; i < burstCount; i++)
        {
            // 원형으로 고르게 퍼지되 약간의 랜덤 (i번째 슬롯 + 지터)
            float angle = (i / (float)burstCount) * Mathf.PI * 2f
                          + Random.Range(-0.3f, 0.3f);
            Vector3 dir = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);

            Vector3 pos = center + dir * Random.Range(0f, spawnScatter);
            float spd = speed * (1f + Random.Range(-speedJitter, speedJitter));

            var ft = Instantiate(popupPrefab, pos, Quaternion.identity);
            ft.Launch(message, dir, spd);

            if (i == 0) Debug.Log($"[PopupDbg] 요청pos={pos} / 실제pos={ft.transform.position} parent={ft.transform.parent}");
        }
    }
}
