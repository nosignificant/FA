using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 락온 안 했을 때 '보이는 모든 생물' 위에 빈 네모를 띄운다 (텍스트 없음).
// 락온(관찰) 중이면 전부 숨김 — 그때는 CreatureHUD가 그 생물 하나만 상세 박스로 보여줌.
// CreatureHUD와 같은 ScreenSpace-Overlay 캔버스 하위에 두고, boxPrefab(빈 프레임 Image)만 할당하면 됨.
public class CreatureBoxOverlay : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("빈 네모 프리팹 (RectTransform + 프레임 Image). 풀링됨")]
    public RectTransform boxPrefab;
    [Tooltip("박스들이 생길 부모(캔버스 하위). 비우면 이 오브젝트")]
    public RectTransform container;

    [Header("표시 조건")]
    public float maxVisibleDistance = 150f;
    public float minVisibleDistance = 0f;
    public float padding = 10f;
    public float maxBoxWidth = 600f;
    public float maxBoxHeight = 500f;
    [Tooltip("락온 가능한 생물만 (끄면 Player·Door 빼고 전부)")]
    public bool onlyLockable = true;
    [Tooltip("생물 목록 갱신 주기(초) — 위치는 매 프레임, 목록만 주기적")]
    public float refreshInterval = 0.3f;

    private Camera cam;
    private PlayerLockOn pl;
    private readonly List<RectTransform> pool = new();
    private readonly List<Creature> creatures = new();
    private float nextRefresh;
    private readonly Vector3[] corners = new Vector3[8];

    private void Start()
    {
        cam = Camera.main;
        if (Player.Instance != null) pl = Player.Instance.pl;
        // container가 비었거나 씬 오브젝트가 아니면(프리팹 에셋 등) 자기 transform으로
        if (container == null || !container.gameObject.scene.IsValid())
            container = (RectTransform)transform;
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (pl == null && Player.Instance != null) pl = Player.Instance.pl;
        if (cam == null || boxPrefab == null) { HideFrom(0); return; }

        // 락온(관찰) 중이면 빈 박스 전부 숨김 (상세 박스는 CreatureHUD가 담당)
        if (pl != null && pl.targetCreature != null) { HideFrom(0); return; }

        if (Time.time >= nextRefresh)
        {
            RefreshCreatures();
            nextRefresh = Time.time + Mathf.Max(0.05f, refreshInterval);
        }

        int used = 0;
        for (int i = 0; i < creatures.Count; i++)
        {
            var cr = creatures[i];
            if (cr == null || cr.IsDead) continue;

            Collider col = cr.mainCollider != null ? cr.mainCollider : cr.GetComponentInChildren<Collider>();
            if (col == null) continue;

            float dist = Vector3.Distance(cam.transform.position, col.bounds.center);
            if (dist > maxVisibleDistance || dist < minVisibleDistance) continue;

            if (!ScreenRect(col.bounds, out Vector3 center, out Vector2 size)) continue;

            var box = GetBox(used++);
            box.sizeDelta = size;
            box.position = center;   // 스크린 좌표 (ScreenSpace-Overlay)
        }
        HideFrom(used);
    }

    // 방들의 creatureList에서 대상 생물 목록 수집 (주기적)
    private void RefreshCreatures()
    {
        creatures.Clear();
        if (RoomManager.Instance == null) return;
        foreach (var kv in RoomManager.Instance.rooms)
        {
            var r = kv.Value;
            if (r == null || r.creatureList == null) continue;
            foreach (var cr in r.creatureList)
            {
                if (cr == null || cr.data == null) continue;
                if (cr.data.creatureID == CreatureID.Player || cr.data.creatureID == CreatureID.Door) continue;
                if (onlyLockable && !cr.IsLockable) continue;
                if (!creatures.Contains(cr)) creatures.Add(cr);
            }
        }
    }

    // collider bounds를 화면 사각형(중심 스크린좌표 + 크기)으로. 카메라 뒤면 false.
    private bool ScreenRect(Bounds b, out Vector3 center, out Vector2 size)
    {
        center = default; size = default;

        corners[0] = new Vector3(b.min.x, b.min.y, b.min.z);
        corners[1] = new Vector3(b.min.x, b.min.y, b.max.z);
        corners[2] = new Vector3(b.min.x, b.max.y, b.min.z);
        corners[3] = new Vector3(b.min.x, b.max.y, b.max.z);
        corners[4] = new Vector3(b.max.x, b.min.y, b.min.z);
        corners[5] = new Vector3(b.max.x, b.min.y, b.max.z);
        corners[6] = new Vector3(b.max.x, b.max.y, b.min.z);
        corners[7] = new Vector3(b.max.x, b.max.y, b.max.z);

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        bool any = false;
        for (int i = 0; i < 8; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(corners[i]);
            if (sp.z <= 0f) continue;
            any = true;
            if (sp.x < minX) minX = sp.x;
            if (sp.x > maxX) maxX = sp.x;
            if (sp.y < minY) minY = sp.y;
            if (sp.y > maxY) maxY = sp.y;
        }
        if (!any) return false;

        float w = Mathf.Clamp((maxX - minX) + padding * 2f, 0f, maxBoxWidth);
        float h = Mathf.Clamp((maxY - minY) + padding * 2f, 0f, maxBoxHeight);
        size = new Vector2(w, h);
        center = cam.WorldToScreenPoint(b.center);
        return true;
    }

    private RectTransform GetBox(int i)
    {
        while (i >= pool.Count)
        {
            var box = Instantiate(boxPrefab);        // 부모 없이 생성 (persistent 부모 에러 방지)
            box.SetParent(container, false);         // 그다음 씬 컨테이너 하위로
            pool.Add(box);
        }
        if (!pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(true);
        return pool[i];
    }

    private void HideFrom(int start)
    {
        for (int i = start; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
    }
}
