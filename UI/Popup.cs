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
    [Min(1)] public int burstCount = 5;
    [Tooltip("팝업 하나씩 사이의 간격(초). 0이면 동시에 다 튀어나옴")]
    public float spawnInterval = 2f;
    public float storyInterval = 2f;

    public float fixedLifeTime = 1.5f;
    public float storyLifeTime = 5f;
    public float speed = 2.5f;
    [Tooltip("속도 랜덤 편차 (0.3이면 ±30%)")]
    [Range(0f, 1f)] public float speedJitter = 0.5f;
    [Tooltip("생성 위치를 중심에서 이만큼 랜덤하게 흩음")]
    public float spawnScatter = 0.3f;
    public Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("메시지별 색 (호출 시 color 미지정이면 이 표에서 문구로 찾음)")]
    [Tooltip("표에 없는 문구는 defaultColor 사용")]
    public Color defaultColor = Color.white;
    public MessageColor[] messageColors =
    {
        new MessageColor { message = "produce",     color = new Color(0.3f, 1f, 0.3f) },
        new MessageColor { message = "decomposed",  color = new Color(1f, 0.5f, 0.3f) },
        new MessageColor { message = "synthesized", color = new Color(0.4f, 0.7f, 1f) },
    };

    [System.Serializable]
    public struct MessageColor { public string message; public Color color; }

    // color가 지정됐으면 그대로, 아니면 문구로 표에서 찾고, 없으면 defaultColor
    private Color ResolveColor(string message, Color? color)
    {
        if (color.HasValue) return color.Value;
        if (messageColors != null)
            for (int i = 0; i < messageColors.Length; i++)
                if (messageColors[i].message == message) return messageColors[i].color;
        return defaultColor;
    }

    public static Popup Instance { get; private set; }

    private void Awake() => Instance = this;

    private IEnumerator Start()
    {
        yield return null;

        if (RoomManager.Instance == null) yield break;

        foreach (var r in RoomManager.Instance.rooms.Values)
        {
            if (r != null)
            {
                r.OnCreatureDecomposed += OnDecomposed;
                r.OnCreatureSynthesized += OnSynthesized;
            }
        }
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance == null) return;

        foreach (var r in RoomManager.Instance.rooms.Values)
        {
            if (r == null) continue;
            r.OnCreatureDecomposed -= OnDecomposed;
            r.OnCreatureSynthesized -= OnSynthesized;
        }
    }

    // 문구는 '누가 했는지(by)'가 아니라 '무슨 이벤트인지'로 정함 (합성 주체가 L/AA/A 등 다양하므로)
    private void OnDecomposed(Creature target, CreatureID by) => Burst(target, "decomposed");
    private void OnSynthesized(Creature target, CreatureID by) => Burst(target, "synthesized");

    // 고정 문구 1종을 burstCount개 뿌림
    private void Burst(Creature target, string message, Color? color = null)
    {
        string[] msgs = new string[burstCount];
        for (int i = 0; i < burstCount; i++) msgs[i] = message;
        SpawnAt(target, msgs, fixedLifeTime, -1f, ResolveColor(message, color));
    }

    // 외부에서 임의 문구 팝업 (예: 조종 시작 시 "controlled"). color 지정 가능
    public void BurstMessage(Creature target, string message, Color? color = null) => Burst(target, message, color);

    // 위치에서 이펙트 터뜨림 (생물 아닌 대상용, 예: 부서지는 벽). message 비우면 내용 없이 이펙트만.
    public void BurstAt(Vector3 worldPos, string message = "", Color? color = null)
    {
        if (popupPrefab == null) return;
        string[] msgs = new string[burstCount];
        for (int i = 0; i < burstCount; i++) msgs[i] = message;
        StartCoroutine(SpawnRoutine(worldPos + offset, msgs, fixedLifeTime, spawnInterval, ResolveColor(message, color)));
    }

    [Header("방향 팝업(상태 변화) 랜덤")]
    [Tooltip("크기 랜덤 배율 범위 (프리팹 기본 스케일에 곱함)")]
    public Vector2 directionalSizeRange = new Vector2(0.6f, 1.5f);
    [Tooltip("수명 랜덤 편차 (0.4면 ±40%)")]
    [Range(0f, 1f)] public float directionalLifeJitter = 0.3f;

    // 지정 위치에서 지정 방향으로 팝업 하나 발사 (방 상태 변화 알림 등 — 방향 고정, 카메라 분산 안 함)
    // 크기·속도·수명을 개체마다 랜덤하게 흩어 단조로움 방지
    public void BurstDirectional(Vector3 worldPos, Vector3 dir, string message = "", Color? color = null)
    {
        if (popupPrefab == null) { Debug.LogWarning("[Popup] BurstDirectional: popupPrefab 미할당"); return; }
        float spd = speed * (1f + Random.Range(-speedJitter, speedJitter));
        float life = fixedLifeTime * (1f + Random.Range(-directionalLifeJitter, directionalLifeJitter));

        var ft = Instantiate(popupPrefab, worldPos, Quaternion.identity);
        ft.transform.localScale *= Random.Range(directionalSizeRange.x, directionalSizeRange.y);   // 크기 랜덤
        ft.Launch(message, dir.normalized, spd, life, ResolveColor(message, color));
    }

    // 스토리 생물 빙의 → 현재 단계 대사 줄들을 "줄당 하나씩" 뿌림 (외부에서 호출)
    public void BurstStoryLines(Creature target)
    {
        if (CreatureStory.Instance == null) return;
        // 뿌리는 순간 해당 단계를 '획득' 처리 → 코덱스 UI에 해금
        SpawnAt(target, CreatureStory.Instance.CollectAndGetCurrentLines(), storyLifeTime, storyInterval);
    }

    // 대상 위치에서 messages를 하나씩 순차로 터뜨림 (시작 위치는 지금 고정 — 생물이 죽어도 안전)
    // interval이 음수면 기본 spawnInterval 사용
    private void SpawnAt(Creature target, string[] messages, float lifeTime, float interval = -1f, Color? color = null)
    {
        if (popupPrefab == null || target == null || messages == null || messages.Length == 0) return;

        Transform t = target.rootTransform != null ? target.rootTransform : target.transform;
        Vector3 center = t.position + offset;

        StartCoroutine(SpawnRoutine(center, messages, lifeTime, interval < 0f ? spawnInterval : interval, color));
    }

    private IEnumerator SpawnRoutine(Vector3 center, string[] messages, float lifeTime, float interval, Color? color = null)
    {
        int count = messages.Length;
        for (int i = 0; i < count; i++)
        {
            // 매번 카메라 축을 새로 읽어 현재 시점 기준으로 퍼지게
            Camera cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;

            // 원형으로 고르게 퍼지되 약간의 랜덤 (count로 나눠 한 바퀴에 균등 분포)
            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector3 dir = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);

            Vector3 pos = center + dir * Random.Range(0f, spawnScatter);
            float spd = speed * (1f + Random.Range(-speedJitter, speedJitter));

            var ft = Instantiate(popupPrefab, pos, Quaternion.identity);
            ft.Launch(messages[i], dir, spd, lifeTime, color);

            if (interval > 0f) yield return new WaitForSeconds(interval);
        }
    }
}
