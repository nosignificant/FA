using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 생물이 분해되면 그 위치에서 텍스트 여러 개가 랜덤 방향으로 터져나가는 연출.
// 방향은 카메라 정면 평면 위로 흩뿌려서 플레이어 시점에서 잘 퍼져 보이게 함.
public class DecomposePopup : MonoBehaviour
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

    // (방, 핸들러) 쌍 보관 — 정리용
    private readonly List<(Room room, Action<Creature, CreatureID> handler)> subs = new();

    private IEnumerator Start()
    {
        yield return null;   // 모든 Room.Start(RoomManager 등록) 끝난 뒤

        if (RoomManager.Instance == null) yield break;

        foreach (var r in RoomManager.Instance.rooms.Values)
        {
            if (r == null) continue;
            Action<Creature, CreatureID> h = (target, who) => Burst(target);
            r.OnCreatureDecomposed += h;
            subs.Add((r, h));
        }
    }

    private void OnDestroy()
    {
        foreach (var s in subs)
            if (s.room != null) s.room.OnCreatureDecomposed -= s.handler;
        subs.Clear();
    }

    private void Burst(Creature target)
    {
        if (popupPrefab == null || target == null) return;

        Transform t = target.rootTransform != null ? target.rootTransform : target.transform;
        Vector3 center = t.position + offset;

        Camera cam = Camera.main;
        // 카메라 정면 평면의 축 (없으면 월드 축 폴백)
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;

        for (int i = 0; i < burstCount; i++)
        {
            // 원형으로 고르게 퍼지되 약간의 랜덤 (i번째 슬롯 + 지터)
            float angle = (i / (float)burstCount) * Mathf.PI * 2f
                          + UnityEngine.Random.Range(-0.3f, 0.3f);
            Vector3 dir = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);

            Vector3 pos = center + dir * UnityEngine.Random.Range(0f, spawnScatter);
            float spd = speed * (1f + UnityEngine.Random.Range(-speedJitter, speedJitter));

            var ft = Instantiate(popupPrefab, pos, Quaternion.identity);
            ft.Launch(message, dir, spd);
        }
    }
}
