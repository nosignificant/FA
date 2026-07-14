using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CreatureTypes;

// C: 열기/닫기. ↑/↓: 선택.
// 플레이어가 있는 방의 생물 개체를 나열하고 각각의 관찰 진행도/빙의 가능 여부를 보여줌.
public class ObservationUI : MonoBehaviour
{
    public static ObservationUI Instance;

    [Header("Refs")]
    public Player player;

    [Tooltip("C로 켜고 끌 루트")]
    public GameObject panelRoot;

    [Header("List (개체당 1개)")]
    public Entry entryPrefab;
    public Transform listParent;

    public float refreshInterval = 0.25f;

    private readonly List<Creature> shown = new();
    private readonly List<Entry> pool = new();
    private int selected = 0;
    private float nextRefresh;
    public bool IsOnOff => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (player == null) player = Player.Instance;

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OnOff(bool on)
    {
        if (panelRoot != null) panelRoot.SetActive(on);
        if (on) { selected = 0; Refresh(); }
    }

    private void Update()
    {
        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + refreshInterval;
            Refresh();
        }
    }

    // 선택한 생물로 강제 락온 후 패널 닫기
    public void LockOnSelected()
    {
        if (shown.Count == 0) return;
        Creature c = shown[selected];
        if (c == null) return;

        PlayerLockOn pl = player != null ? player.pl : null;
        if (pl != null) pl.ForceLock(c);

        OnOff(false);
    }

    public void Move(int dir)
    {
        if (shown.Count == 0) return;
        selected = (selected + dir + shown.Count) % shown.Count;
        Refresh();
    }

    // 플레이어가 있는 방의 생물 개체로 목록 구성
    private void RebuildShown()
    {
        //새로 만들기 전에 비우기 
        shown.Clear();

        Room room = player != null ? player.currentRoom : null;
        if (room == null || room.creatureList == null) return;

        //방에 있는 생물 목록 > 인스턴스화
        foreach (var c in room.creatureList)
        {
            if (c == null || c.data == null) continue;
            if (c == (player != null ? player.pc : null)) continue;       // 플레이어 자신 제외
            if (c.data.creatureID == CreatureID.Door) continue;            // 문 제외
            shown.Add(c);
        }
    }

    private Entry GetEntry(int index)
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
            //생물 정보 가져옴
            var c = shown[i];
            //만들어놓은 엔트리 가져옴
            var e = GetEntry(i);
            e.gameObject.SetActive(true);

            string n = $"{c.data.creatureName} ({c.intent})";
            e.Set(n, i == selected);
        }

        // 남는 엔트리 끄기
        for (int i = shown.Count; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }
}
