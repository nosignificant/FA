using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CreatureTypes;

public class PlayerRoomCreatureUI : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject areaSectionPrefab;  // 구역 헤더
    [SerializeField] private GameObject creatureRowPrefab;  // 생물 행

    public Room subscribedRoom;

    void Start()
    {
        var room = RoomManager.Instance?.GetRoom(Player.Instance?.roomID);
        Player.Instance.roomChanged += SubscribeTo;
        SubscribeTo(Player.Instance.currentRoom);
    }

    private void SubscribeTo(Room newRoom)
    {
        if (subscribedRoom != null) subscribedRoom.OnCreatureDecomposed -= RefreshUI;

        if (newRoom == null)
        {
            ClearUI();
            return;
        }

        subscribedRoom = newRoom;
        subscribedRoom.OnCreatureDecomposed += RefreshUI;
        Debug.Log("subscribed room " + subscribedRoom.roomID);

        RefreshUI();
    }

    private void RefreshUI() => RefreshUI(null, CreatureID.Player);

    private void RefreshUI(Creature target, CreatureID decomposerID)
    {
        ClearUI();
        if (subscribedRoom == null) return;

        // 구역 헤더
        var sectionGo = Instantiate(areaSectionPrefab, parent);

        var sectionTmp = sectionGo.GetComponentInChildren<TextMeshProUGUI>();
        if (sectionTmp != null)
        {
            sectionTmp.text = $"Area {subscribedRoom.roomID}";
        }

        // 구역 내 생물 목록
        foreach (var kv in subscribedRoom.decomposedCounts)
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
