using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 스토리 해금 단계 저장소.
// advancesStory 종을 빙의하면 단계 +1 (개체당 1회 — 같은 개체 재빙의는 무시).
public class StoryManager : MonoBehaviour
{
    private static StoryManager _instance;
    public static StoryManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindObjectOfType<StoryManager>();
            if (_instance == null)
                _instance = new GameObject("StoryManager").AddComponent<StoryManager>();
            return _instance;
        }
    }

    public int Stage { get; private set; }

    // 단계가 오를 때 발행 (UI/연출이 구독)
    public event Action<int> OnStageChanged;

    // 이미 스토리에 기여한 개체 (개체당 1회 보장)
    private readonly HashSet<Creature> counted = new();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    // 빙의 성공 시 CreaturePossess가 호출
    public void TryAdvanceFromPossess(Creature c)
    {
        if (c == null || c.data == null || !c.data.advancesStory) return;
        if (!counted.Add(c)) return;   // 이미 카운트된 개체면 Add가 false → 무시

        Stage++;
        Debug.Log($"[Story] 단계 +1 → {Stage} ({c.data.creatureName} 빙의)");
        OnStageChanged?.Invoke(Stage);
    }
}
