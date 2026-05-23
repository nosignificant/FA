using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using CreatureTypes;

// C: 열기/닫기. ↑/↓: 선택.
// 플레이어가 있는 방의 생물 개체를 나열하고 각각의 관찰 진행도/빙의 가능 여부를 보여줌.
public class ObservationUI : MonoBehaviour
{
    public static ObservationUI Instance;

    [Header("Refs")]
    public ObservationLearner learner;
    public Player player;

    [Tooltip("C로 켜고 끌 루트")]
    public GameObject panelRoot;

    [Header("List (개체당 1개)")]
    public ObservationEntry entryPrefab;
    public Transform listParent;

    [Header("Detail")]
    public TMP_Text detailText;

    [Header("Options")]
    public KeyCode toggleKey = KeyCode.C;
    public KeyCode navKey = KeyCode.Space;     // 누를 때마다 아래로 이동, 맨 아래면 첫 칸으로
    public KeyCode lockOnKey = KeyCode.E;
    public float refreshInterval = 0.25f;

    private readonly List<Creature> shown = new();
    private readonly List<ObservationEntry> pool = new();
    private int selected = 0;
    private float nextRefresh;
    private Interaction interaction;
    private readonly StringBuilder sb = new();
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    // ESC를 이 프레임에 ObservationUI가 소비했는지 (실행 순서 무관)
    private static int escConsumedFrame = -1;
    public static bool EscConsumedThisFrame => escConsumedFrame == Time.frameCount;

    private void Awake()
    {
        Instance = this;
        if (player == null) player = Player.Instance;
        if (learner == null && player != null) learner = player.GetComponent<ObservationLearner>();

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void SetOpen(bool on)
    {
        if (panelRoot != null) panelRoot.SetActive(on);
        if (on) { selected = 0; Refresh(); }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetOpen(!IsOpen);
            return;
        }

        // ESC: 열려있으면 먼저 닫기 (이 프레임 ESC 소비 — 순서 무관하게 마커로)
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            escConsumedFrame = Time.frameCount;
            SetOpen(false);
            return;
        }

        if (!IsOpen) return;

        // 스페이스바: 한 칸 아래로, 맨 아래면 첫 칸으로 순환
        if (Input.GetKeyDown(navKey)) Move(1);

        // E: 선택한 생물로 락온하고 UI 닫기
        if (Input.GetKeyDown(lockOnKey))
        {
            LockOnSelected();
            return;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + refreshInterval;
            Refresh();
        }
    }

    // 선택한 생물로 강제 락온 후 패널 닫기
    private void LockOnSelected()
    {
        if (shown.Count == 0) return;
        Creature c = shown[selected];
        if (c == null) return;

        PlayerLockOn pl = player != null ? player.pl : null;
        if (pl != null) pl.ForceLock(c);

        SetOpen(false);
    }

    private void Move(int dir)
    {
        if (shown.Count == 0) return;
        selected = (selected + dir + shown.Count) % shown.Count;
        Refresh();
    }

    // 플레이어가 있는 방의 생물 개체로 목록 구성
    private void RebuildShown()
    {
        shown.Clear();

        Room room = player != null ? player.currentRoom : null;
        if (room == null || room.creatureList == null) return;

        foreach (var c in room.creatureList)
        {
            if (c == null || c.data == null) continue;
            if (c == (player != null ? player.pc : null)) continue;       // 플레이어 자신 제외
            if (c.data.creatureID == CreatureID.Door) continue;            // 문 제외
            if (c.data.creatureID == CreatureID.D) continue;               // D 제외
            shown.Add(c);
        }
    }

    // 필요한 만큼만 엔트리 풀에서 꺼내 쓰고 나머지는 비활성화
    private ObservationEntry GetEntry(int index)
    {
        while (pool.Count <= index)
        {
            var e = Instantiate(entryPrefab, listParent);
            pool.Add(e);
        }
        return pool[index];
    }

    private void Refresh()
    {
        if (entryPrefab == null || listParent == null) return;

        RebuildShown();

        if (shown.Count == 0) selected = 0;
        else selected = Mathf.Clamp(selected, 0, shown.Count - 1);

        for (int i = 0; i < shown.Count; i++)
        {
            var c = shown[i];
            var e = GetEntry(i);
            e.gameObject.SetActive(true);

            string n = string.IsNullOrEmpty(c.data.creatureName)
                ? c.data.creatureID.ToString() : c.data.creatureName;
            int req = Mathf.Max(1, c.data.observationsToLearn);

            e.Set(n, c.data.signatureIntent.ToString(),
                  c.observeCount, req, c.possessable, i == selected);
        }

        // 남는 엔트리 끄기
        for (int i = shown.Count; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);

        if (detailText != null)
            detailText.text = shown.Count > 0
                ? BuildDetail(shown[selected])
                : "(이 방에 관찰할 생물이 없습니다)";
    }

    private string BuildDetail(Creature self)
    {
        if (interaction == null)
            interaction = (player != null && player.pc != null) ? player.pc.interact
                                                                : FindObjectOfType<Interaction>();

        sb.Clear();
        var data = self.data;
        string sn = string.IsNullOrEmpty(data.creatureName) ? data.creatureID.ToString() : data.creatureName;
        sb.AppendLine($"<b>{sn} 행동</b>");
        sb.AppendLine(self.possessable
            ? "<color=#6fdc6f>관찰 완료 — 빙의 가능</color>"
            : $"관찰 {self.observeCount}/{Mathf.Max(1, data.observationsToLearn)}");

        if (interaction == null) { sb.AppendLine("(Interaction 없음)"); return sb.ToString(); }

        foreach (InteractionAction act in System.Enum.GetValues(typeof(InteractionAction)))
        {
            if (act == InteractionAction.Ignore) continue;

            string targets = "";
            foreach (CreatureID tid in System.Enum.GetValues(typeof(CreatureID)))
            {
                if (tid == data.creatureID) continue;
                if (interaction.HasAction(data.creatureID, tid, act))
                    targets += (targets.Length > 0 ? ", " : "") + tid;
            }

            if (targets.Length > 0)
                sb.AppendLine($"· {act}: {targets}");
        }

        return sb.ToString();
    }
}
