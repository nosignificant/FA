using System.Collections.Generic;
using UnityEngine;

// 스토리 진행(Player.Stage + CreatureStory 획득 목록)을 PlayerPrefs로 영속화.
// 씬을 넘어가도(=씬 오브젝트가 파괴돼도) 값이 유지된다.
// 새 게임 시작 시엔 Clear()로 초기화할 것.
public static class StoryProgress
{
    private const string StageKey = "story_stage";
    private const string CollectedKey = "story_collected";   // "0,1,2" 형태 CSV

    // ── Stage ────────────────────────────────────────────
    public static int LoadStage() => PlayerPrefs.GetInt(StageKey, 0);

    public static void SaveStage(int stage)
    {
        PlayerPrefs.SetInt(StageKey, stage);
        PlayerPrefs.Save();
    }

    // ── CreatureStory 획득 목록 ──────────────────────────
    public static HashSet<int> LoadCollected()
    {
        var set = new HashSet<int>();
        string csv = PlayerPrefs.GetString(CollectedKey, "");
        if (string.IsNullOrEmpty(csv)) return set;

        foreach (var tok in csv.Split(','))
            if (int.TryParse(tok, out int v)) set.Add(v);
        return set;
    }

    public static void SaveCollected(IEnumerable<int> stages)
    {
        PlayerPrefs.SetString(CollectedKey, string.Join(",", stages));
        PlayerPrefs.Save();
    }

    // ── 새 게임 등 초기화 ────────────────────────────────
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(CollectedKey);
        PlayerPrefs.Save();
    }
}
