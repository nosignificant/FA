using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 화면 미니맵. RoomManager의 방들을 월드 XZ 좌표대로 UI에 배치하고,
// 플레이어 방·활성 방을 색으로 표시한다.
public class MiniMap : MonoBehaviour
{
    [Header("refs")]
    [Tooltip("방들이 배치될 영역 RectTransform (비우면 이 오브젝트). 앵커/피벗을 center로 두는 게 좋음")]
    public RectTransform mapArea;
    [Tooltip("방 하나를 그릴 UI 프리팹 (MiniMapRoom + Image)")]
    public MiniMapRoom roomPrefab;

    [Header("설정")]
    [Min(0f)] public float padding = 12f;         // 가장자리 여백(px)
    public float refreshInterval = 0.2f;          // 색·우세종 갱신 주기

    [Header("크기 모드")]
    [Tooltip("true면 '방 하나 = roomPixelSize' 고정 스케일. false면 mapArea에 맞춰 자동 축소")]
    public bool useFixedPixelPerRoom = true;
    [Tooltip("방 하나 박스 크기(px). 프리팹이 198이면 198")]
    public float roomPixelSize = 198f;

    private readonly List<MiniMapRoom> roomUIs = new();

    IEnumerator Start()
    {
        // RoomManager.Start도 yield 1프레임 뒤 방 등록을 끝내므로 여유 있게 대기
        yield return null;
        yield return null;

        Build();
        StartCoroutine(RefreshLoop());
    }

    void Build()
    {
        if (mapArea == null) mapArea = GetComponent<RectTransform>();
        if (RoomManager.Instance == null || roomPrefab == null || mapArea == null) return;

        // 방 수집
        var rooms = new List<Room>();
        foreach (var r in RoomManager.Instance.rooms.Values)
            if (r != null) rooms.Add(r);
        if (rooms.Count == 0) return;

        // 월드 XZ 범위 → 미니맵 영역에 맞게 스케일 (종횡비 유지)
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var r in rooms)
        {
            Vector3 p = r.transform.position;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
        }
        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        float worldW = Mathf.Max(0.001f, maxX - minX);
        float worldH = Mathf.Max(0.001f, maxZ - minZ);

        // 스케일 결정
        float scale;
        if (useFixedPixelPerRoom)
        {
            // 방 하나(roomSize.x)가 roomPixelSize가 되도록 고정 스케일 → 방 간격도 그에 맞춰 벌어짐
            float refWorld = rooms[0].roomSize.x;
            if (refWorld < 0.001f) refWorld = 60f;
            scale = roomPixelSize / refWorld;
        }
        else
        {
            Rect area = mapArea.rect;
            scale = Mathf.Min((area.width - padding * 2f) / worldW,
                              (area.height - padding * 2f) / worldH);
            if (float.IsInfinity(scale) || scale <= 0f) scale = 1f;
        }

        foreach (var r in rooms)
        {
            MiniMapRoom ui = Instantiate(roomPrefab, mapArea);
            ui.Bind(r);

            Vector3 rel = r.transform.position - center;
            Vector2 pos = new Vector2(rel.x * scale, rel.z * scale);   // 월드 XZ → UI XY (중심 기준)
            Vector2 size = new Vector2(r.roomSize.x * scale, r.roomSize.z * scale);   // 방 실제 크기 비례

            ui.SetLayout(pos, size);
            roomUIs.Add(ui);
        }
    }

    IEnumerator RefreshLoop()
    {
        var wait = new WaitForSeconds(refreshInterval);
        while (true)
        {
            Room pr = Player.Instance != null ? Player.Instance.currentRoom : null;
            for (int i = 0; i < roomUIs.Count; i++)
                if (roomUIs[i] != null) roomUIs[i].Refresh(pr);
            yield return wait;
        }
    }
}
