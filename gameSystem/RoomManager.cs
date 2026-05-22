using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    public Dictionary<string, Room> rooms = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (Player.Instance != null)
        {
            Player.Instance.roomChanged += UpdateActiveRooms;
            // Player.Start가 먼저 돌아 초기 roomChanged를 놓쳤을 경우 대비해 즉시 동기화
            if (Player.Instance.currentRoom != null)
                UpdateActiveRooms(Player.Instance.currentRoom);
        }
    }

    public void Register(Room room)
    {
        rooms[room.roomID] = room;
    }

    public Room GetRoom(string id)
    {
        rooms.TryGetValue(id, out var room);
        return room;
    }

    public void UpdateActiveRooms(Room playerRoom)
    {
        // 전부 비활성화
        foreach (var r in rooms.Values)
            r.isActive = false;

        if (playerRoom == null) return;

        // 플레이어 방 + 열린 문으로 연결된 방만 활성화
        playerRoom.isActive = true;
        foreach (var d in playerRoom.doors)
        {
            if (!d.isOpen) continue;
            Room other = d.GetOtherRoom(playerRoom);
            // 튜토리얼 방은 문 열려도 자동 활성화 안 함
            if (other != null && !other.isTutorial) other.isActive = true;
        }
    }
}
