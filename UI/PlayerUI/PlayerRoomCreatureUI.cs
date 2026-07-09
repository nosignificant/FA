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

    void Start()
    {
        //플레이어 방 변경 이벤트 구독
        if (Player.Instance != null) Player.Instance.roomChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        if (Player.Instance != null) Player.Instance.roomChanged -= OnRoomChanged;
    }


    private void OnRoomChanged(Room newRoom) => RefreshUI();

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
