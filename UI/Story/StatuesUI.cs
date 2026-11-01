using UnityEngine;

// "statues" 패널: 현재 L/A 방 개수를 자릿수 글리프로 표시한다.
//  각 자리(10의 자리 / 1의 자리)마다 0~9 글리프 10개를 받아, 현재 숫자 하나만 켠다.
public class StatuesUI : MonoBehaviour
{
    [Header("L / A total 자릿수 표시")]
    [Tooltip("각 배열에 0~9 글리프 10개를 넣으세요. 순서 무관 — 글리프 '이름 끝의 숫자'로 매칭합니다 (예: \"num (1)\"→1, \"num 0\"→0).")]
    public GameObject[] lOnes;   // L total 1의 자리
    public GameObject[] lTens;   // L total 10의 자리
    public GameObject[] aOnes;   // A total 1의 자리
    public GameObject[] aTens;   // A total 10의 자리
    [Tooltip("켜면 10 미만일 때 10의 자리에 0을 표시. 끄면 10의 자리를 비움(예: 5 → ' 5')")]
    public bool showLeadingZero = false;

    private int _lastL = -1, _lastA = -1;

    // StatuesMenu가 열 때 호출 — 강제 1회 갱신
    public void Show()
    {
        _lastL = _lastA = -1;
        Refresh();
    }

    // 패널이 열려 있는 동안 방 상태 변화를 실시간 반영
    private void Update()
    {
        if (StatuesMenu.AnyOpen) Refresh();
    }

    private void Refresh()
    {
        int l = CountRooms(Room.RoomActivation.L);
        int a = CountRooms(Room.RoomActivation.A);
        if (l == _lastL && a == _lastA) return;   // 바뀔 때만 글리프 토글
        _lastL = l; _lastA = a;

        ShowNumber(lTens, lOnes, l);
        ShowNumber(aTens, aOnes, a);
    }

    // count(0~99)를 10의 자리 / 1의 자리 글리프로 표시
    private void ShowNumber(GameObject[] tens, GameObject[] ones, int count)
    {
        count = Mathf.Clamp(count, 0, 99);
        int tensDigit = count / 10;
        int onesDigit = count % 10;

        SetDigit(ones, onesDigit);
        // 10 미만이고 leading zero를 안 쓰면 10의 자리는 비움(-1 → 전부 끔)
        SetDigit(tens, (count < 10 && !showLeadingZero) ? -1 : tensDigit);
    }

    // 이름 끝 숫자가 digit 인 글리프만 켠다. digit<0 이면 전부 끔.
    private static void SetDigit(GameObject[] glyphs, int digit)
    {
        if (glyphs == null) return;
        for (int i = 0; i < glyphs.Length; i++)
        {
            if (glyphs[i] == null) continue;
            bool on = digit >= 0 && GlyphValue(glyphs[i].name) == digit;
            glyphs[i].SetActive(on);
        }
    }

    // 오브젝트 이름 끝의 정수를 뽑는다. "num (1)"→1, "num 0"→0, 없으면 -1.
    private static int GlyphValue(string name)
    {
        int i = name.Length - 1;
        while (i >= 0 && !char.IsDigit(name[i])) i--;   // 뒤쪽 숫자 끝 찾기
        int end = i;
        while (i >= 0 && char.IsDigit(name[i])) i--;     // 숫자 구간 시작 전까지
        if (end < 0 || i >= end) return -1;
        return int.TryParse(name.Substring(i + 1, end - i), out int v) ? v : -1;
    }

    // 현재 해당 상태(L/A)인 방 개수
    private static int CountRooms(Room.RoomActivation state)
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;
        int count = 0;
        foreach (var r in RoomManager.Instance.allRooms)
            if (r != null && r.Activation == state) count++;
        return count;
    }
}
