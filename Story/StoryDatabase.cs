using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 스토리 데이터의 소스 오브 트루스.
/// CreatureData 참조는 Inspector에서 할당하고,
/// 본문(title/content)은 BuildStories()에서 코드로 작성.
/// </summary>
public class StoryDatabase : MonoBehaviour
{
    public static StoryDatabase Instance;

    [Header("CreatureData References (Inspector에서 할당)")]
    public CreatureData hData;
    public CreatureData lData;
    public CreatureData ssData;
    public CreatureData dData;
    public CreatureData aData;
    public CreatureData aaData;
    public CreatureData hhData;
    // 필요한 만큼 추가

    [Header("Runtime")]
    public List<StoryData> allStories = new List<StoryData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildStories();
    }

    void BuildStories()
    {
        allStories.Clear();

        // ===== H 관련 =====
        allStories.Add(new StoryData
        {
            storyId = "h_001",
            observingCreature = hData,
            requiredCount = 1,
            title = "First Sighting of H",
            content = "처음 H를 분해했을 때 손에 묻은 점액질의 감촉이 잊혀지지 않는다..."
        });

        allStories.Add(new StoryData
        {
            storyId = "h_002",
            observingCreature = hData,
            requiredCount = 5,
            title = "Patterns of H",
            content = "다섯 마리째. 이 생물들은 무리지어 다닌다. 분해해보니 모두 같은 구조였다..."
        });

        // ===== L 관련 =====
        allStories.Add(new StoryData
        {
            storyId = "l_001",
            observingCreature = lData,
            requiredCount = 1,
            title = "The Tentacled One",
            content = "L의 촉수는 살아있는 것처럼 움직였다..."
        });

        // 더 추가하고 싶은 만큼 작성
    }
}
