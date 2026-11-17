using System.Collections.Generic;
using UnityEngine;

// 게임 중 나온 대사를 레벨(씬)별로 '나온 순서대로' 기록하는 전역 저장소.
//  - TutorialGuide.Say()가 한 줄 표시될 때마다 Record(씬이름, 줄) 호출.
//  - DialogueMenuUI가 레벨별 누적 대사를 읽어 세부창에 표시.
// 추가 씬(level3/4/5 등)을 아직 안 갔으면 그 버킷이 비어 있어 자동으로 '열람 불가'가 된다.
// (세션 동안만 유지 — 게임 재시작 시 저장/복원은 별도 작업)
public static class DialogueLog
{
    private static readonly Dictionary<string, List<string>> _byLevel = new();

    // 도메인 리로드 끄는 fast play-mode 대비: 재생 시작 때 비움
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _byLevel.Clear();

    // 한 줄 기록. 같은 줄이 바로 직전과 같으면(중복 Say) 무시.
    public static void Record(string levelKey, string line)
    {
        if (string.IsNullOrEmpty(levelKey) || string.IsNullOrEmpty(line)) return;
        if (!_byLevel.TryGetValue(levelKey, out var list))
            _byLevel[levelKey] = list = new List<string>();
        if (list.Count > 0 && list[list.Count - 1] == line) return;   // 연속 중복 방지
        list.Add(line);
    }

    // 이 레벨에 기록된 대사가 하나라도 있나 (= 그 레벨을 방문했나)
    public static bool HasContent(string levelKey)
        => _byLevel.TryGetValue(levelKey, out var l) && l.Count > 0;

    // 이 레벨의 대사 줄들 (없으면 빈 배열)
    public static IReadOnlyList<string> Lines(string levelKey)
        => _byLevel.TryGetValue(levelKey, out var l) ? l : System.Array.Empty<string>();
}
