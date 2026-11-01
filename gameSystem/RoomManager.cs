using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    // roomID로 조회(튜토리얼 등). additive로 여러 레벨이 뜨면 같은 이름이 덮어쓸 수 있으니 '조회용'으로만 씀.
    public Dictionary<string, Room> rooms = new();
    // 모든 등록된 방(인스턴스 기준, 충돌 없음). 카운트·순회는 이걸 쓴다.
    public readonly HashSet<Room> allRooms = new();

    [Header("상시 시뮬 (경량)")]
    [Tooltip("플레이어가 없는 방도 계속 연산할지. 끄면 예전처럼 비활성 방은 완전 정지")]
    public bool simulateInactiveRooms = true;
    [Tooltip("비활성 방 연산 배율. 클수록 느리게(가볍게) 돎. 예: 4면 활성 방의 1/4 빈도로 판단")]
    [Min(1f)] public float inactiveTickScale = 4f;
    [Tooltip("열린 문으로 연결되면 튜토리얼 방도 활성화. 끄면 튜토리얼 방은 플레이어가 직접 들어가야 활성(그 전엔 생물 정지)")]
    public bool activateConnectedTutorialRooms = true;

    // 현재 roomChanged를 구독 중인 플레이어 (씬마다 새로 생기므로 추적해서 재바인딩)
    private Player _boundPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);           // 씬 넘어가도 상주 — 모든 레벨 방을 누적 관리
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator Start()
    {
        // 한 프레임 대기: 모든 Room.Start(방 등록)·Door.Start(문 등록·isOpen 확정)가 끝난 뒤 계산
        yield return null;
        BindPlayerAndRefresh();
    }

    // 씬이 새로 로드되면 그 씬의 '새 플레이어/새 방'에 다시 바인딩 (상주라 Start는 한 번뿐이므로 필요)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindNextFrame());
    }

    private IEnumerator RebindNextFrame()
    {
        yield return null;   // 새 씬의 Room.Start(방 등록)가 끝난 뒤
        BindPlayerAndRefresh();
    }

    // 현재 씬의 Player.roomChanged에 (재)구독하고 활성 방 1회 갱신
    private void BindPlayerAndRefresh()
    {
        var p = Player.Instance;
        if (p != _boundPlayer)
        {
            if (_boundPlayer != null) _boundPlayer.roomChanged -= UpdateActiveRooms;
            if (p != null) p.roomChanged += UpdateActiveRooms;
            _boundPlayer = p;
        }

        // 초기 1회 무조건 실행 — 플레이어 방을 아직 몰라도(null) 모든 방을 비활성=정지 처리.
        UpdateActiveRooms(p != null ? p.currentRoom : null);
    }

    public void Register(Room room)
    {
        if (room == null) return;
        allRooms.Add(room);
        rooms[room.roomID] = room;   // 조회용 (충돌 시 마지막 등록이 우선)
    }

    // 방이 파괴될 때만 해제. 같은 roomID로 이미 '다른' 방이 재등록됐으면 dict는 건드리지 않음.
    public void Unregister(Room room)
    {
        if (room == null) return;
        allRooms.Remove(room);
        if (rooms.TryGetValue(room.roomID, out var r) && r == room)
            rooms.Remove(room.roomID);
    }

    public void UpdateActiveRooms(Room playerRoom)
    {
        // 전부 비활성화
        foreach (var r in allRooms)
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
                if (other.isTutorial && !activateConnectedTutorialRooms) continue;   // 토글 꺼지면 튜토리얼 방은 통과 제외

                other.SetActive(true);
                visited.Add(other);
                queue.Enqueue(other);
            }
        }
    }
}
