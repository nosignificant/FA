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

    private IEnumerator Start()
    {
        // 한 프레임 대기: 모든 Room.Start(방 등록)·Door.Start(문 등록·isOpen 확정)가 끝난 뒤 계산
        yield return null;

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

    public void UpdateActiveRooms(Room playerRoom)
    {
        // 전부 비활성화
        foreach (var r in rooms.Values)
            if (r != null) r.isActive = false;

        if (playerRoom == null) return;

        // 플레이어 방에서 "열린 문"으로 연결된 방 전체를 flood-fill(BFS)로 활성화 (다단계 포함)
        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();

        playerRoom.isActive = true;
        visited.Add(playerRoom);
        queue.Enqueue(playerRoom);

        while (queue.Count > 0)
        {
            Room room = queue.Dequeue();
            if (room.doors == null) continue;

            foreach (var d in room.doors)
            {
                if (d == null || !d.isOpen) continue;
                Room other = d.GetOtherRoom(room);
                if (other == null || visited.Contains(other)) continue;
                if (other.isTutorial) continue;   // 튜토리얼 방은 자동 활성화·통과 제외

                other.isActive = true;
                visited.Add(other);
                queue.Enqueue(other);
            }
        }
    }
}
