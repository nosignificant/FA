using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using CreatureTypes;

// C: 열기/닫기. ↑/↓: 선택. 생물마다 ObservationEntry 프리팹 1개씩 인스턴스화.
public class ObservationUI : MonoBehaviour
{
    [Header("Refs")]
    public ObservationLearner learner;
    public Player player;
    public CreatureDatabase creatureDB;

    [Tooltip("C로 켜고 끌 루트")]
    public GameObject panelRoot;

    [Header("List (생물당 1개)")]
    public ObservationEntry entryPrefab;
    public Transform listParent;

    [Header("Detail")]
    public TMP_Text detailText;

    [Header("Options")]
    public KeyCode toggleKey = KeyCode.C;
    public float refreshInterval = 0.25f;

    private readonly List<CreatureData> shown = new();
    private readonly List<ObservationEntry> entries = new();
    private int selected = 0;
    private float nextRefresh;
    private Interaction interaction;
    private readonly StringBuilder sb = new();
    private bool built = false;

    private void Awake()
    {
        if (player == null) player = Player.Instance;
        if (learner == null && player != null) learner = player.GetComponent<ObservationLearner>();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool on = panelRoot != null && !panelRoot.activeSelf;
            if (panelRoot != null) panelRoot.SetActive(on);
            if (on) { selected = 0; BuildList(); Refresh(); }
            // 열려있는 동안 플레이어 이동 차단 (W/S가 네비랑 겹침)
            PlayerControl.SetPlayerMove(!on);
            return;
        }

        if (panelRoot == null || !panelRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))   Move(-1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(1);

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + refreshInterval;
            Refresh();
        }
    }

    private void Move(int dir)
    {
        if (shown.Count == 0) return;
        selected = (selected + dir + shown.Count) % shown.Count;
        Refresh();
    }

    // 생물 목록 = 엔트리 프리팹 인스턴스 (1회 생성)
    private void BuildList()
    {
        if (built || creatureDB == null || creatureDB.allCreatures == null) return;
        if (entryPrefab == null || listParent == null) return;

        shown.Clear();
        foreach (var d in creatureDB.allCreatures)
        {
            if (d == null) continue;
            if (d.creatureID == CreatureID.Door || d.creatureID == CreatureID.Player) continue;
            shown.Add(d);
        }

        foreach (var d in shown)
        {
            var e = Instantiate(entryPrefab, listParent);
            entries.Add(e);
        }
        built = true;
    }

    private void Refresh()
    {
        if (shown.Count == 0) return;
        selected = Mathf.Clamp(selected, 0, shown.Count - 1);

        for (int i = 0; i < shown.Count; i++)
        {
            var d = shown[i];
            string n = string.IsNullOrEmpty(d.creatureName) ? d.creatureID.ToString() : d.creatureName;
            bool learned = player != null && player.learnedForms.Contains(d);
            int cur = learner != null ? learner.GetProgress(d.creatureID) : 0;
            int req = Mathf.Max(1, d.observationsToLearn);
            entries[i].Set(n, d.signatureIntent.ToString(), cur, req, learned, i == selected);
        }

        if (detailText != null)
            detailText.text = BuildDetail(shown[selected]);
    }

    private string BuildDetail(CreatureData self)
    {
        if (interaction == null)
            interaction = (player != null && player.pc != null) ? player.pc.interact
                                                                : FindObjectOfType<Interaction>();

        sb.Clear();
        string sn = string.IsNullOrEmpty(self.creatureName) ? self.creatureID.ToString() : self.creatureName;
        sb.AppendLine($"<b>{sn} 행동</b>");

        if (interaction == null) { sb.AppendLine("(Interaction 없음)"); return sb.ToString(); }

        foreach (InteractionAction act in System.Enum.GetValues(typeof(InteractionAction)))
        {
            if (act == InteractionAction.Ignore) continue;

            string targets = "";
            foreach (CreatureID tid in System.Enum.GetValues(typeof(CreatureID)))
            {
                if (tid == self.creatureID) continue;
                if (interaction.HasAction(self.creatureID, tid, act))
                    targets += (targets.Length > 0 ? ", " : "") + tid;
            }

            if (targets.Length > 0)
                sb.AppendLine($"· {act}: {targets}");
        }

        return sb.ToString();
    }
}
