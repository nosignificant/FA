using UnityEngine;
using CreatureTypes;

// 플레이어가 '현재 있는 방'의 L/A 개수를 두 줄(각 최대 9칸)로 표시.
//  - 각 칸: line(항상, 미리 배치) + fill(토글). 수만큼 앞에서부터 fill ON, 나머진 선만.
//  - 용량(9) 초과면 "+" ON.
//  - 임계 마커: 방의 activationMinCount 위치 마커만 ON (방마다 다름).
// 전부 미리 배치해두고 SetActive로만 켜고 끔. 방·수 변화 시에만 갱신.
public class RoomStateBar : MonoBehaviour
{
    [System.Serializable]
    public class Bar
    {
        [Tooltip("칸별 fill(면) 오브젝트. [0]=1칸째 … 최대 9개")]
        public GameObject[] fills;
        [Tooltip("용량 초과 표시 '+' (비우면 생략)")]
        public GameObject plusMark;
        [Tooltip("이 줄 전체 루트(방 없을 때 숨김용, 선택)")]
        public GameObject root;
    }

    [Header("L 줄 / A 줄")]
    public Bar lBar;
    public Bar aBar;

    [Header("임계 마커 (L/A 공용 — 하나만)")]
    [Tooltip("마커들의 부모. 자식 순서 = [0]=0개, [1]=1개 … → activationMinCount번째 자식만 ON")]
    public Transform thresholdParent;

    [Header("옵션")]
    [Tooltip("갱신 주기(초)")]
    public float refreshInterval = 0.2f;
    [Tooltip("플레이어 방이 없을 때 두 줄 숨김")]
    public bool hideWhenNoRoom = true;

    private float _next;
    private Room _lastRoom;
    private int _lastL = -1, _lastA = -1, _lastThresh = -1;

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + Mathf.Max(0.05f, refreshInterval);

        Room room = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (room == null)
        {
            if (hideWhenNoRoom) { SetActiveSafe(lBar?.root, false); SetActiveSafe(aBar?.root, false); }
            _lastRoom = null; _lastL = _lastA = _lastThresh = -1;
            return;
        }

        int nL = room.CountSpecies(CreatureID.L);
        int nA = room.CountSpecies(CreatureID.A);
        int thresh = room.activationMinCount;

        // 바뀐 게 없으면 불필요한 SetActive 생략
        if (room == _lastRoom && nL == _lastL && nA == _lastA && thresh == _lastThresh) return;
        _lastRoom = room; _lastL = nL; _lastA = nA; _lastThresh = thresh;

        SetActiveSafe(lBar?.root, true);
        SetActiveSafe(aBar?.root, true);
        ApplyBar(lBar, nL);
        ApplyBar(aBar, nA);
        ApplyThreshold(thresh);          // L/A 공용 마커 하나
    }

    private void ApplyBar(Bar bar, int count)
    {
        if (bar == null) return;
        int cap = bar.fills != null ? bar.fills.Length : 0;
        int shown = Mathf.Min(count, cap);

        for (int i = 0; i < cap; i++)                      // 앞에서부터 count칸 fill ON, 나머진 OFF(선만)
            SetActiveSafe(bar.fills[i], i < shown);

        SetActiveSafe(bar.plusMark, count > cap);          // 용량 초과면 '+'
    }

    // 임계 마커(L/A 공용): 부모 자식들 중 'threshold번째'(=threshold개 위치)만 보이게. [0]=0개, [1]=1개 …
    // SetActive 대신 CanvasGroup.alpha로 토글 → 레이아웃에서 안 빠져 자리 유지.
    private void ApplyThreshold(int threshold)
    {
        if (thresholdParent == null) return;
        int n = thresholdParent.childCount;
        for (int i = 0; i < n; i++)
            SetAlpha(thresholdParent.GetChild(i).gameObject, i == threshold ? 1f : 0f);
    }

    private static void SetActiveSafe(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }

    // CanvasGroup alpha로 보임/숨김 (레이아웃 자리 유지). 없으면 추가.
    private static void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        if (!Mathf.Approximately(cg.alpha, a)) cg.alpha = a;
    }
}
