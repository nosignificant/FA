using UnityEngine;

// 임시 테스트용 — 엔딩 분기(A 과반 / L 과반)와 임계(20개 이상) 게이트를 실제 방 없이 강제한다.
// 사용법: 아무 오브젝트에 붙이고, 플레이 중 인스펙터에서 '이 컴포넌트 헤더 우클릭 → 메뉴' 실행.
//   → P53EndingTrigger의 카운트를 덮어써서, p53 바라보고 F 누르면 해당 분기로 엔딩이 진행된다.
// 테스트가 끝나면 이 파일/컴포넌트는 통째로 삭제하면 됨 (실제 로직은 안 건드림).
public class EndingTestHarness : MonoBehaviour
{
    [Header("A 과반 시나리오 (총합이 임계 초과하도록)")]
    public int aHeavyA = 15;
    public int aHeavyL = 6;

    [Header("L 과반 시나리오")]
    public int lHeavyA = 6;
    public int lHeavyL = 15;

    [ContextMenu("A 과반으로 설정")]
    public void SetAHeavy() => Apply(aHeavyA, aHeavyL);

    [ContextMenu("L 과반으로 설정")]
    public void SetLHeavy() => Apply(lHeavyA, lHeavyL);

    [ContextMenu("강제 해제 (실제 방 카운트 사용)")]
    public void Clear()
    {
        var t = P53EndingTrigger.Instance;
        if (t != null) { t.testA = null; t.testL = null; }
        else Debug.LogWarning("[EndingTest] P53EndingTrigger.Instance 없음 — 플레이 중에 실행하세요.");

        foreach (var m in Meters()) m.SetTestCounts(null, null);   // 메터도 실제 카운트로 복귀
        Debug.Log("[EndingTest] 강제 카운트 해제 — 실제 RoomManager 카운트로 복귀.");
    }

    private void Apply(int a, int l)
    {
        var t = P53EndingTrigger.Instance;
        if (t != null) { t.testA = a; t.testL = l; }
        else Debug.LogWarning("[EndingTest] P53EndingTrigger.Instance 없음 — 엔딩 게이트는 테스트 안 됨(플레이 중 실행).");

        foreach (var m in Meters()) m.SetTestCounts(l, a);   // LevelProgressMeter 색도 같은 L/A로 갱신
        Debug.Log($"[EndingTest] 강제 A={a} L={l} (총 {a + l}) → 엔딩 {(a > l ? "A" : "L")} 분기 예상 + 메터 색 갱신. p53 바라보고 F 두 번.");
    }

    // 비활성 area의 메터까지 포함해 모두 찾음
    private static LevelProgressMeter[] Meters() =>
        FindObjectsByType<LevelProgressMeter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}
