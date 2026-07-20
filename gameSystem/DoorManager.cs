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

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        quitting = false;
    }

    private void OnDestroy()
    {
        if (_instance == this) { _instance = null; quitting = true; }
    }

    private void OnApplicationQuit() => quitting = true;

    public void Register(Door d) { if (d != null) doors.Add(d); }
    public void Unregister(Door d) { if (d != null) doors.Remove(d); }

    // 문이 열리거나 닫힐 때 Door가 호출
    public void NotifyDoorChanged() => OnOpenDoorsChanged?.Invoke();

    // 특정 종을 watching하는 "열린" 문 개수
    public int OpenDoorCount(CreatureData species)
    {
        if (species == null) return 0;
        int n = 0;
        foreach (var d in doors)
            if (d != null && d.isOpen && d.watchingCreature == species) n++;
        return n;
    }

    // 종 → 열린 문 개수 전체 (UI 등에서 사용)
    public Dictionary<CreatureData, int> OpenDoorCountsBySpecies()
    {
        var dict = new Dictionary<CreatureData, int>();
        foreach (var d in doors)
        {
            if (d == null || !d.isOpen || d.watchingCreature == null) continue;
            dict.TryGetValue(d.watchingCreature, out int c);
            dict[d.watchingCreature] = c + 1;
        }
        return dict;
    }
}
