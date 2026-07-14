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

    Room subscribedRoom;
    void Start()
    {
        //플레이어 방이 바뀔 때마다 onRoomChange 실행
        if (Player.Instance != null)
        {
            Player.Instance.roomChanged += OnRoomChanged;
            // 이미 방이 정해져 있으면 즉시 구독 + 표시 (초기 roomChanged 놓쳐도 대비)
            if (Player.Instance.currentRoom != null) OnRoomChanged(Player.Instance.currentRoom);
        }
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.roomChanged -= OnRoomChanged;
        if (subscribedRoom != null) subscribedRoom.OnCreatureDecomposed -= OnDecomposed;
    }


    private void OnRoomChanged(Room newRoom)
    {
        if (subscribedRoom != null) subscribedRoom.OnCreatureDecomposed -= OnDecomposed;  // 첫 진입 땐 null이므로 체크
        subscribedRoom = newRoom;
        if (subscribedRoom != null) subscribedRoom.OnCreatureDecomposed += OnDecomposed;
        RefreshUI();
    }

    private void OnDecomposed(Creature target, CreatureID who) => RefreshUI();
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
