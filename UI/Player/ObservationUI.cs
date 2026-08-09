using System.Text;
using UnityEngine;
using TMPro;
using CreatureTypes;

// 상시 표시되는 정보 패널.
// ① 현재 방의 방위(N/S/E/W)별 문 요구 종
// ② 레벨 안 "열린 방"들의 현재 우세 분해종
public class ObservationUI : MonoBehaviour
{
    public static ObservationUI Instance;

    [Header("Refs")]
    public Player player;
    public TMP_Text arealabel;
    public TMP_Text currentRoomlabel;

    [Header("갱신")]
    public float refreshInterval = 0.25f;

    private float nextRefresh;
    private readonly StringBuilder sb = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 모든 Awake가 끝난 뒤라 Player.Instance가 세팅돼 있음
        if (player == null) player = Player.Instance;
    }

    private void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshInterval;
        Refresh();
    }

    private void Refresh()
    {
        // ── ① 현재 방의 분해된 생물 수 (문 방향은 나침반이 표시) ──────
        if (currentRoomlabel != null)
        {
            sb.Clear();
            Room room = player != null ? player.currentRoom : null;

            if (room != null)
            {
                sb.AppendLine($"room {room.roomID}");
                arealabel.text = sb.ToString();
                sb.Clear();

                AppendCreatureList(room);
            }

            currentRoomlabel.text = sb.ToString();
        }
    }

    // 현재 방에 살아있는 생물을 종별로 목록에 추가 (문 열림 기준과 동일 집계)
    private void AppendCreatureList(Room room)
    {
        var counts = room.SpeciesCounts();
        if (counts.Count == 0) return;

        sb.AppendLine("creatures");
        foreach (var kv in counts)
        {
            if (kv.Key == null || kv.Value <= 0) continue;
            sb.AppendLine($"{Name(kv.Key)}   x{kv.Value}");
        }
    }

    private static string Name(CreatureData d) =>
        string.IsNullOrEmpty(d.creatureName) ? d.creatureID.ToString() : d.creatureName;
}
