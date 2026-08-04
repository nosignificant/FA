using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 레벨마다 "플레이어가 통과한 문(LevelPortal)의 판정 종(CreatureID)"을 순서대로 기록한다.
// (PlayerPrefs, 씬 넘어가도 유지) 마지막 엔딩 씬에서 AllChose(...)로 분기 판정.
public static class ChoiceProgress
{
    private const string Key = "level_choices";   // CSV of ints (CreatureID)

    public static void Record(CreatureID id)
    {
        var list = All();
        list.Add(id);
        PlayerPrefs.SetString(Key, string.Join(",", list.ConvertAll(v => ((int)v).ToString())));
        PlayerPrefs.Save();
    }

    public static List<CreatureID> All()
    {
        var list = new List<CreatureID>();
        string csv = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(csv)) return list;

        foreach (var tok in csv.Split(','))
            if (int.TryParse(tok, out int v)) list.Add((CreatureID)v);
        return list;
    }

    public static int Count => All().Count;

    // 기록된 선택이 minCount개 이상이고 전부 id면 true (예: 앞 3레벨 모두 A → AllChose(A, 3))
    public static bool AllChose(CreatureID id, int minCount)
    {
        var list = All();
        if (list.Count < minCount) return false;
        foreach (var c in list)
            if (c != id) return false;
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
