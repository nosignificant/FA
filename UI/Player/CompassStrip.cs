using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CreatureTypes;

// 가로 눈금 띠 나침반.
// 모든 요소(눈금, N/S/E/W, 문 요구 마커)를 카메라 yaw 기준 각도 → 픽셀로 배치한다.
// 화면 중앙 = 카메라가 보는 방향. 정면이 N이면 N이 중앙에 온다.
//
// 좌표 규약: 월드 +Z = N(북), heading은 +Z에서 시계방향 각도(= cam.eulerAngles.y 규약과 동일).
public class CompassStrip : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;                  // 비우면 Camera.main 사용
    public RectTransform strip;            // 눈금/마커가 담길 부모(마스크 처리하면 깔끔). 비우면 자기 자신
    public Player player;                   // 현재 방의 문 요구 표시용. 비우면 Player.Instance

    [Header("Tick 프리팹 (얇은 세로 막대 Image 하나)")]
    public RectTransform tickPrefab;
    [Tooltip("눈금 간격(도). 15면 24칸")]
    public float tickSpacingDeg = 15f;
    [Tooltip("굵게 표시할 간격(도). 90이면 N/E/S/W 자리만 굵게")]
    public float majorEveryDeg = 90f;
    public float majorScale = 1.6f;

    [Header("배치 스케일")]
    [Tooltip("1도당 픽셀. (띠 폭/2) / (보여줄 각도/2)로 잡으면 됨. 예: 폭400·±90도 → 400/90≈4.44")]
    public float pixelsPerDegree = 4.44f;
    [Tooltip("정면 기준 ±이 각도까지만 표시(밖은 숨김)")]
    public float visibleHalfAngle = 90f;
    [Tooltip("북(N) 0점 보정. N이 중앙에 올 때까지 이 값을 조절")]
    public float northOffset = 0f;

    [Header("방위 라벨 (TMP 프리팹)")]
    public TMP_Text labelPrefab;
    public float labelY = 18f;

    [Header("문 요구 마커 (TMP 프리팹)")]
    public TMP_Text doorMarkerPrefab;
    public float doorMarkerY = -18f;

    // 풀
    private readonly List<RectTransform> tickPool = new();
    private readonly List<TMP_Text> labelPool = new();
    private readonly List<TMP_Text> doorPool = new();

    private struct DirLabel { public float heading; public string text; }
    private readonly DirLabel[] cardinals =
    {
        new DirLabel { heading = 0f,   text = "N" },
        new DirLabel { heading = 90f,  text = "E" },
        new DirLabel { heading = 180f, text = "S" },
        new DirLabel { heading = 270f, text = "W" },
    };

    private void Awake()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (strip == null) strip = transform as RectTransform;
        if (player == null) player = Player.Instance;
    }

    private void LateUpdate()
    {
        if (cam == null) { if (Camera.main != null) cam = Camera.main.transform; else return; }
        if (strip == null) return;

        float camYaw = cam.eulerAngles.y + northOffset;
        // strip 실제 폭의 절반. 눈금·글자·마커 모두 이 픽셀 범위 밖이면 숨김 → 컷오프 일치
        float halfWidth = strip.rect.width * 0.5f;

        PlaceTicks(camYaw, halfWidth);
        PlaceCardinals(camYaw, halfWidth);
        PlaceDoors(camYaw, halfWidth);
    }

    // 정면 기준 상대각을 픽셀 x로. 범위 밖이면 false
    private bool RelX(float camYaw, float heading, float halfWidth, out float x)
    {
        float rel = Mathf.DeltaAngle(camYaw, heading);
        x = rel * pixelsPerDegree;
        return Mathf.Abs(rel) <= visibleHalfAngle && Mathf.Abs(x) <= halfWidth;
    }

    // ── 눈금 ──────────────────────────────────────────────
    private void PlaceTicks(float camYaw, float halfWidth)
    {
        int shown = 0;
        // 0~360을 tickSpacingDeg 간격으로 순회하며 시야 안의 것만 배치
        for (float deg = 0f; deg < 360f; deg += tickSpacingDeg)
        {
            if (!RelX(camYaw, deg, halfWidth, out float x)) continue;

            RectTransform t = GetTick(shown++);
            t.anchoredPosition = new Vector2(x, 0f);

            bool major = Mathf.Abs(Mathf.DeltaAngle(deg, Mathf.Round(deg / majorEveryDeg) * majorEveryDeg)) < 0.01f;
            t.localScale = major ? new Vector3(1f, majorScale, 1f) : Vector3.one;
        }
        // 남는 풀 비활성
        for (int i = shown; i < tickPool.Count; i++) tickPool[i].gameObject.SetActive(false);
    }

    private RectTransform GetTick(int i)
    {
        while (i >= tickPool.Count)
        {
            RectTransform t = Instantiate(tickPrefab, strip);
            CenterAnchor(t);
            tickPool.Add(t);
        }
        tickPool[i].gameObject.SetActive(true);
        return tickPool[i];
    }

    // anchoredPosition을 strip 중앙 기준으로 통일 (눈금·글자·마커 정렬 일치)
    private static void CenterAnchor(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
    }

    // ── N/S/E/W 라벨 ──────────────────────────────────────
    private void PlaceCardinals(float camYaw, float halfWidth)
    {
        if (labelPrefab == null) return;
        int shown = 0;
        foreach (var c in cardinals)
        {
            if (!RelX(camYaw, c.heading, halfWidth, out float x)) continue;

            TMP_Text lbl = GetLabel(shown++);
            lbl.text = c.text;
            ((RectTransform)lbl.transform).anchoredPosition = new Vector2(x, labelY);
        }
        for (int i = shown; i < labelPool.Count; i++) labelPool[i].gameObject.SetActive(false);
    }

    private TMP_Text GetLabel(int i)
    {
        while (i >= labelPool.Count)
        {
            TMP_Text lbl = Instantiate(labelPrefab, strip);
            CenterAnchor((RectTransform)lbl.transform);
            labelPool.Add(lbl);
        }
        labelPool[i].gameObject.SetActive(true);
        return labelPool[i];
    }

    // ── 현재 방 문 요구 마커 ──────────────────────────────
    private void PlaceDoors(float camYaw, float halfWidth)
    {
        if (doorMarkerPrefab == null) return;
        if (player == null) player = Player.Instance;
        Room room = player != null ? player.currentRoom : null;

        int shown = 0;
        if (room != null)
        {
            foreach (Direction dir in DirectionExt.All)
            {
                Door door = room.GetDoor(dir);
                if (door == null || door.watchingCreature == null) continue;

                if (!RelX(camYaw, HeadingOf(dir), halfWidth, out float x)) continue;

                TMP_Text m = GetDoorMarker(shown++);
                string name = string.IsNullOrEmpty(door.watchingCreature.creatureName)
                    ? door.watchingCreature.creatureID.ToString()
                    : door.watchingCreature.creatureName;
                m.text = door.isOpen ? $"[{name}]" : name;
                ((RectTransform)m.transform).anchoredPosition = new Vector2(x, doorMarkerY);
            }
        }
        for (int i = shown; i < doorPool.Count; i++) doorPool[i].gameObject.SetActive(false);
    }

    private TMP_Text GetDoorMarker(int i)
    {
        while (i >= doorPool.Count)
        {
            TMP_Text m = Instantiate(doorMarkerPrefab, strip);
            CenterAnchor((RectTransform)m.transform);
            doorPool.Add(m);
        }
        doorPool[i].gameObject.SetActive(true);
        return doorPool[i];
    }

    // 월드 방위 → heading(도, +Z=0, 시계방향). cam.eulerAngles.y와 같은 규약.
    private static float HeadingOf(Direction dir)
    {
        Vector3 v = dir.ToVec();
        return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;   // +Z=0, +X=90
    }
}
