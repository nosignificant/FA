using UnityEngine;

[System.Serializable]
public class StoryData
{
    public string storyId;
    public CreatureData observingCreature;  // 어느 생물 조건에 묶이는지
    public int requiredCount;                // 이 생물 분해 누적 N개 이상이면 해금
    public string title;
    [TextArea(5, 30)] public string content;
}
