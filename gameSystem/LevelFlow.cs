using System.Collections.Generic;
using CreatureTypes;

// 레벨 진행 흐름을 한 곳에 모아둔 맵.
// LevelPortal이 '현재 씬 이름'으로 여기서 '다음 씬'을 찾아 로드한다.
// 매 씬의 LevelPortal targetScene을 일일이 바꿀 필요 없이 여기만 고치면 전체 흐름이 바뀜.
public static class LevelFlow
{
    // ★ 실제 씬 이름/순서에 맞게 수정하세요 (현재 씬 → 다음 씬).
    public static readonly Dictionary<string, string> Next = new()
    {
        { "tutorial0", "level1" },
        { "level1",    "level2" },
        { "level2",    "level3" },
        { "level3",    "level4" },
        { "level4",    "level5" },
        { "level5",    "ending" },
    };

    // A/L 선택에 따라 다른 씬으로 분기하고 싶을 때만 "씬:선택" 키로 추가 (분기 우선).
    //  예: { "level3:A", "level4_A" }, { "level3:L", "level4_L" }
    public static readonly Dictionary<string, string> NextByChoice = new()
    {
    };

    // 현재 씬(+선택)에 대한 다음 씬. 선택 분기가 있으면 우선, 없으면 씬 기본. 없으면 null.
    public static string GetNext(string currentScene, CreatureID choice)
    {
        if (string.IsNullOrEmpty(currentScene)) return null;
        if (NextByChoice.TryGetValue($"{currentScene}:{choice}", out var branched)) return branched;
        return Next.TryGetValue(currentScene, out var s) ? s : null;
    }
}
