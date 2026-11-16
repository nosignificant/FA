using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 활성화된 방 총개수(level total)에 비례해 children 세그먼트를 None/L/A 색으로 칠하는 진행도 미터.
// 앞에서부터 L방 비율만큼 lColor, 그다음 A방 비율만큼 aColor, 나머지는 noneColor.
// 방 상태(L/A/None)가 바뀔 때마다 자동 갱신. (WallActivationTint와 같은 구독 패턴)
// 예: P53 엔딩 임계치(total > 19)까지의 진행도 표시 → fullAtTotal에 그 기준을 넣으면 됨.
public class LevelProgressMeter : MonoBehaviour
{
    [Tooltip("순서대로 칠할 세그먼트. 비우면 이 오브젝트의 '직속 자식'을 위에서부터 순서대로 사용")]
    public GameObject[] segments;

    [Tooltip("미터가 꽉 차는 기준 활성 방 수. 0이면 세그먼트 개수(= 방 1개당 세그 1개)")]
    public int fullAtTotal = 0;

    [Tooltip("이 씬(레벨)의 방만 셀지. 끄면 로드된 전체 방 합계(엔딩 판정 기준과 동일)")]
    public bool thisSceneOnly = true;

    [Header("세그먼트 색 (None/L/A) — WallActivationTint와 동일 기본값")]
    public Color noneColor = Color.white;
    public Color lColor = new Color(0.6f, 1f, 0.3f);   // 연두
    public Color aColor = new Color(0.9f, 0.2f, 0.2f); // 빨강
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color). SpriteRenderer/UI는 .color 사용")]
    public string colorProperty = "";

    private GameObject[] _segs;
    private Room[] _subscribed;

    private GameObject[] Segs()
    {
        if (_segs != null) return _segs;
        if (segments != null && segments.Length > 0) { _segs = segments; return _segs; }
        var list = new List<GameObject>();
        foreach (Transform c in transform) list.Add(c.gameObject);   // 직속 자식만
        _segs = list.ToArray();
        return _segs;
    }

    private void OnEnable() => StartCoroutine(Bind());

    private void OnDisable()
    {
        if (_subscribed == null) return;
        foreach (var r in _subscribed)
            if (r != null) r.OnActivationChanged -= OnRoomChanged;
        _subscribed = null;
    }

    // RoomManager 준비 + 방 등록을 기다렸다가 대상 방들을 구독하고 1회 갱신.
    private IEnumerator Bind()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.allRooms == null)
            yield return null;
        yield return null;   // 같은 씬 방들이 모두 등록될 시간을 한 프레임 줌

        var list = new List<Room>();
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            if (thisSceneOnly && r.gameObject.scene != gameObject.scene) continue;
            r.OnActivationChanged -= OnRoomChanged;   // 중복 구독 방지
            r.OnActivationChanged += OnRoomChanged;
            list.Add(r);
        }
        _subscribed = list.ToArray();
        Refresh();
    }

    private void OnRoomChanged(Room.RoomActivation _) => Refresh();

    // --- 임시 테스트용 강제 카운트 (EndingTestHarness가 설정; null이면 실제 방 사용) ---
    [System.NonSerialized] public int? testL;
    [System.NonSerialized] public int? testA;
    public void SetTestCounts(int? l, int? a) { testL = l; testA = a; Refresh(); }

    // 활성화된 L 방 / A 방 수 (thisSceneOnly면 이 씬만, 아니면 전체)
    private void CountLA(out int l, out int a)
    {
        l = 0; a = 0;
        if (testL.HasValue || testA.HasValue) { l = testL ?? 0; a = testA ?? 0; return; }
        if (RoomManager.Instance == null) return;
        if (!thisSceneOnly)
        {
            l = RoomManager.Instance.CountRooms(Room.RoomActivation.L);
            a = RoomManager.Instance.CountRooms(Room.RoomActivation.A);
            return;
        }
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null || r.gameObject.scene != gameObject.scene) continue;
            if (r.Activation == Room.RoomActivation.L) l++;
            else if (r.Activation == Room.RoomActivation.A) a++;
        }
    }

    // 총개수에 비례해 앞에서부터 L칸 → A칸 → None칸 순으로 칠함.
    public void Refresh()
    {
        var segs = Segs();
        if (segs == null || segs.Length == 0) return;

        CountLA(out int l, out int a);
        int total = l + a;
        int full = fullAtTotal > 0 ? fullAtTotal : segs.Length;
        int lit = Mathf.Clamp(Mathf.RoundToInt(total / (float)full * segs.Length), 0, segs.Length);

        // 켜질 칸(lit)을 L:A 비율로 나눔 (합이 정확히 lit이 되도록 A는 나머지)
        int lLit = total > 0 ? Mathf.RoundToInt(l / (float)total * lit) : 0;
        int aLit = lit - lLit;

        for (int i = 0; i < segs.Length; i++)
        {
            if (segs[i] == null) continue;
            Room.RoomActivation state = i < lLit ? Room.RoomActivation.L
                                      : i < lLit + aLit ? Room.RoomActivation.A
                                      : Room.RoomActivation.None;
            Colorize(segs[i], state);
        }
    }

    // 세그먼트(+자식)의 Renderer와 UI Graphic을 상태 색으로 칠함.
    private void Colorize(GameObject seg, Room.RoomActivation state)
    {
        Color col = state == Room.RoomActivation.A ? aColor
                  : state == Room.RoomActivation.L ? lColor : noneColor;

        var rends = seg.GetComponentsInChildren<Renderer>(true);          // 3D/Sprite
        if (rends != null && rends.Length > 0) ActivationColor.Apply(rends, col, colorProperty);

        var graphics = seg.GetComponentsInChildren<Graphic>(true);         // UI Image/Text
        if (graphics != null)
            foreach (var g in graphics) if (g != null) g.color = col;
    }
}
