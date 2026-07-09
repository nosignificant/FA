using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CreatureTypes;

public class PlayerRoomCreatureUI : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject areaSectionPrefab;  // 구역 헤더
    [SerializeField] private GameObject creatureRowPrefab;  // 생물 행

    // 구독한 방들 (정리용)
    private readonly List<Room> subscribed = new();

    void Start()
    {
        if (Player.Instance != null) Player.Instance.roomChanged += OnRoomChanged;
        StartCoroutine(SubscribeAllRoomsNextFrame());
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.roomChanged -= OnRoomChanged;
        foreach (var r in subscribed)
            if (r != null) r.OnCreatureDecomposed -= OnAnyDecomposed;
        subscribed.Clear();
    }

    // 모든 Room.Start(RoomManager 등록)가 끝난 뒤 구독 — 단일 방 구독의 인스턴스/타이밍 어긋남 방지
    private IEnumerator SubscribeAllRoomsNextFrame()
    {
        yield return null;

        if (RoomManager.Instance != null)
        {
            foreach (var r in RoomManager.Instance.rooms.Values)
            {
                if (r == null || subscribed.Contains(r)) continue;
                r.OnCreatureDecomposed += OnAnyDecomposed;
                subscribed.Add(r);
            }
        }

        RefreshUI();
    }

    private void OnRoomChanged(Room newRoom) => RefreshUI();

    // 어느 방에서든 분해가 나면 호출 — 플레이어 현재 방에서 난 것만 갱신
    private void OnAnyDecomposed(Creature target, CreatureID decomposerID)
    {
        if (Player.Instance == null || Player.Instance.currentRoom == null) return;
        if (target != null && target.currentRoom == Player.Instance.currentRoom)
            RefreshUI();
    }

    private void RefreshUI()
    {
        ClearUI();

        Room room = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (room == null) return;

        // 구역 헤더
        var sectionGo = Instantiate(areaSectionPrefab, parent);
        var sectionTmp = sectionGo.GetComponentInChildren<TextMeshProUGUI>();
        if (sectionTmp != null)
            sectionTmp.text = $"Area {room.roomID}";

        // 구역 내 분해 카운트
        foreach (var kv in room.decomposedCounts)
        {
            if (kv.Value == 0) continue;
            if (kv.Key.creatureID == CreatureID.Player) continue;

            var rowGo = Instantiate(creatureRowPrefab, parent);
            var tmps = rowGo.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length >= 2)
            {
                tmps[0].text = kv.Key.creatureName;
                tmps[1].text = $"x{kv.Value}";
            }
        }
    }

    private void ClearUI()
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }
}
