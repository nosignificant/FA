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
            Player.Instance.roomChanged += UpdateActiveRooms;
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
            if (d.isOpen && d.nextRoom != null)
                d.nextRoom.isActive = true;
        }
    }
}
