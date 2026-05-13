using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CreatureTypes;

/// <summary>
/// 모든 방의 decomposedCounts를 합산해서 분해 누적 카운트로 사용.
/// Room.OnCreatureDecomposed 이벤트 구독해서 변동 시 OnFulfillmentChanged 발행.
/// </summary>
public class StoryUnlockManager : MonoBehaviour
{
    public static StoryUnlockManager Instance;

    public StoryDatabase database;
    public event Action OnFulfillmentChanged;

    private readonly List<Room> subscribedRooms = new List<Room>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (database == null) database = StoryDatabase.Instance;
    }

    void Start()
    {
        if (database == null) database = StoryDatabase.Instance;
        SubscribeAllRooms();
    }

    void OnDestroy()
    {
        foreach (var r in subscribedRooms)
            if (r != null) r.OnCreatureDecomposed -= HandleDecomposed;
        subscribedRooms.Clear();
    }

    void SubscribeAllRooms()
    {
        foreach (var room in FindObjectsOfType<Room>())
        {
            if (subscribedRooms.Contains(room)) continue;
            room.OnCreatureDecomposed += HandleDecomposed;
            subscribedRooms.Add(room);
        }
    }

    void HandleDecomposed(Creature target, CreatureID decomposerID)
    {
        OnFulfillmentChanged?.Invoke();
    }

    /// <summary>특정 생물의 모든 방 누적 분해 횟수</summary>
    public int GetDecomposedCount(CreatureData c)
    {
        if (c == null) return 0;
        int total = 0;
        foreach (var room in FindObjectsOfType<Room>())
        {
            if (room.decomposedCounts.TryGetValue(c, out int n)) total += n;
        }
        return total;
    }

    /// <summary>UI 상단에 표시할 (생물, 누적횟수) 목록</summary>
    public Dictionary<CreatureData, int> GetAllCounts()
    {
        var dict = new Dictionary<CreatureData, int>();
        foreach (var room in FindObjectsOfType<Room>())
        {
            foreach (var kv in room.decomposedCounts)
            {
                if (kv.Key == null) continue;
                if (!dict.ContainsKey(kv.Key)) dict[kv.Key] = 0;
                dict[kv.Key] += kv.Value;
            }
        }
        return dict;
    }

    /// <summary>현재 카운트 기준으로 해금된 모든 스토리</summary>
    public List<StoryData> GetAllUnlockedStories()
    {
        if (database == null) return new List<StoryData>();
        return database.allStories
            .Where(s => s.observingCreature != null
                     && s.requiredCount <= GetDecomposedCount(s.observingCreature))
            .ToList();
    }

    /// <summary>특정 생물 조건의 해금된 스토리만</summary>
    public List<StoryData> GetUnlockedStoriesFor(CreatureData c)
    {
        if (database == null || c == null) return new List<StoryData>();
        int count = GetDecomposedCount(c);
        return database.allStories
            .Where(s => s.observingCreature == c && s.requiredCount <= count)
            .ToList();
    }
}
