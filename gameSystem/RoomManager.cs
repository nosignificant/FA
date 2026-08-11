using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    public Dictionary<string, Room> rooms = new();

    [Header("상시 시뮬 (경량)")]
    [Tooltip("플레이어가 없는 방도 계속 연산할지. 끄면 예전처럼 비활성 방은 완전 정지")]
    public bool simulateInactiveRooms = true;
    [Tooltip("비활성 방 연산 배율. 클수록 느리게(가볍게) 돎. 예: 4면 활성 방의 1/4 빈도로 판단")]
    [Min(1f)] public float inactiveTickScale = 4f;

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
            Player.Instance.roomChanged += UpdateActiveRooms;

        // 초기 1회 무조건 실행 — 플레이어 방을 아직 몰라도(null) 모든 방을 비활성=정지 처리.
        // (currentRoom이 null이라 안 부르면, SetActive가 안 걸려서 비활성 방 생물이 안 얼어붙음)
        UpdateActiveRooms(Player.Instance != null ? Player.Instance.currentRoom : null);
    }

    public void Register(Room room)
    {
        rooms[room.roomID] = room;
    }

    public void UpdateActiveRooms(Room playerRoom)
    {
        // 전부 비활성화
        foreach (var r in rooms.Values)
            if (r != null) r.SetActive(false);

        if (playerRoom == null) return;

        // 플레이어 방에서 "열린 문"으로 연결된 방 전체를 flood-fill(BFS)로 활성화 (다단계 포함)
        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();

        playerRoom.SetActive(true);
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

                other.SetActive(true);
                visited.Add(other);
                queue.Enqueue(other);
            }
        }
    }
}
