using System;
using System.Collections.Generic;
using UnityEngine;

// 씬의 모든 문을 추적하고, "현재 열린 문"을 watching 종(CreatureData)별로 합산한다.
// 문 상태가 바뀌면 OnOpenDoorsChanged를 발행 → 퍼즐 조건이 구독해서 반응.
public class DoorManager : MonoBehaviour
{
    private static DoorManager _instance;
    private static bool quitting;   // 종료/씬 언로드 중이면 새로 만들지 않음

    public static DoorManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            if (quitting) return null;   // 정리 도중엔 생성 금지 (유령 오브젝트 방지)

            _instance = FindObjectOfType<DoorManager>();
            if (_instance == null)
                _instance = new GameObject("DoorManager").AddComponent<DoorManager>();
            return _instance;
        }
    }

    // 자동 생성 없이 현재 인스턴스만 조회 (OnDisable/OnDestroy 정리용)
    public static DoorManager Existing => _instance;

    private readonly HashSet<Door> doors = new();

    // 열린 문 구성이 바뀔 때마다 발행 (문 열림/닫힘 시)
    public event Action OnOpenDoorsChanged;

    // 플레이 시작마다 static 초기화 (도메인 리로드 꺼진 fast playmode에서 값이 남는 것 방지)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _instance = null; quitting = false; }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        quitting = false;
    }

    private void OnDestroy()
    {
        // 씬 언로드로 파괴되는 것일 수 있으므로 quitting은 세우지 않음 —
        // 다음 씬의 문이 Instance(지연 생성)를 쓸 수 있어야 함. 정리 호출은 이미 Existing을 쓴다.
        if (_instance == this) _instance = null;
    }

    private void OnApplicationQuit() => quitting = true;   // 진짜 종료 때만 유령 생성 차단

    public void Register(Door d) { if (d != null) doors.Add(d); }
    public void Unregister(Door d) { if (d != null) doors.Remove(d); }

    // 문이 열리거나 닫힐 때 Door가 호출
    public void NotifyDoorChanged() => OnOpenDoorsChanged?.Invoke();

    // 특정 상태(L/A)를 요구하는 "열린" 문 개수
    public int OpenDoorCount(Room.RoomActivation state)
    {
        int n = 0;
        foreach (var d in doors)
            if (d != null && d.isOpen && d.requiredState == state) n++;
        return n;
    }

    // 현재 열린 문을 요구 상태(L/A)별로 합산 (statues UI용)
    public Dictionary<Room.RoomActivation, int> OpenDoorCountsByState()
    {
        var map = new Dictionary<Room.RoomActivation, int>();
        foreach (var d in doors)
        {
            if (d == null || !d.isOpen) continue;
            map.TryGetValue(d.requiredState, out int c);
            map[d.requiredState] = c + 1;
        }
        return map;
    }
}
