using System.Text;
using UnityEngine;
using TMPro;

// "statues" 패널: 현재까지 열린 문을 watching 종별로 보여준다. (짧아서 스크롤 없음)
//  -- 열린문 --
//  s : 2개
//  h : 1개
// 열고 닫기는 StatuesMenu가 담당. 여기선 텍스트 갱신만.
public class StatuesUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text text;            // 목록을 뿌릴 TMP

    private void OnEnable()
    {
        if (DoorManager.Existing != null)
            DoorManager.Existing.OnOpenDoorsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (DoorManager.Existing != null)
            DoorManager.Existing.OnOpenDoorsChanged -= Refresh;
    }

    // StatuesMenu가 열 때 호출
    public void Show()
    {
        // OnEnable이 Existing==null 타이밍이면 구독을 놓칠 수 있어 진입 때 한 번 더 보정
        if (DoorManager.Instance != null)
        {
            DoorManager.Instance.OnOpenDoorsChanged -= Refresh;
            DoorManager.Instance.OnOpenDoorsChanged += Refresh;
        }
        Refresh();
    }

    private void Refresh()
    {
        if (text == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("-- current open door --");

        var dm = DoorManager.Existing;
        if (dm != null)
        {
            var map = dm.OpenDoorCountsByState();
            if (map.Count == 0) sb.AppendLine(" ");
            else
                foreach (var kv in map)
                    sb.AppendLine($"{kv.Key} : {kv.Value}개");
        }

        text.text = sb.ToString().TrimEnd();
    }
}
