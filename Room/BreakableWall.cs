using UnityEngine;

// 부술 수 있는 벽. S가 닿으면 Hit() → hitsToBreak만큼 맞으면 Break().
// 부서지면: 메시·콜라이더 끔 → 그 자리가 "통로"로 등록되어 L/A가 틈으로 넘어감.
public class BreakableWall : MonoBehaviour
{
    [Header("on/off")]
    [Tooltip("이 벽 타일을 부술 수 있는지. false면 일반(파괴불가) 벽 — S가 무시.")]
    public bool breakable = true;

    [Header("연결")]
    [Tooltip("이 벽이 접한 두 방 (S가 찾을 수 있게 등록됨)")]
    public Room roomA;
    public Room roomB;

    [Tooltip("부서지면 숨길 벽 메시 (비우면 자기 MeshRenderer 자동)")]
    public GameObject wallMesh;

    [Header("on/off 시 표시 전환")]
    [Tooltip("breakable=on일 때 활성화할 오브젝트 (부술 수 있는 벽 비주얼)")]
    public GameObject breakableObject;
    [Tooltip("breakable=on일 때 비활성화할 일반 벽 오브젝트")]
    public GameObject normalWall;

    [Header("breakable 색")]
    [Tooltip("켜면 breakableObject를 지정 색으로 칠함")]
    public bool tintBreakable = false;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color, SpriteRenderer는 .color)")]
    public string breakableColorProperty = "";
    public Color breakableColor = Color.white;

    [Header("window (옆방 보기용, 통과 불가)")]
    [Tooltip("켜면 창문 모드: 일반 벽 메쉬 끄고, 부술 수 없고(등록 안 함), 창문만 활성화")]
    public bool isWindow = false;
    [Tooltip("이미 씬에 둔 창문 오브젝트 (있으면 이걸 씀)")]
    public GameObject window;
    [Tooltip("창문 프리팹 후보들 (window가 비어있을 때 windowIndex로 골라 생성)")]
    public GameObject[] windowPrefabs;
    [Tooltip("windowPrefabs 중 사용할 인덱스")]
    public int windowIndex = 0;

    [Header("내구도")]
    [Min(1)] public int hitsToBreak = 3;
    [Tooltip("부서질 때 이 반경 안의 반대편(연결된 방) 벽도 같이 부숨")]
    public float oppositeBreakRange = 5f;

    public bool Broken { get; private set; }
    public Vector3 Position => transform.position;

    private int hits;

    private void Start()
    {
        if (roomA == null || roomB == null) AutoDetectRooms();
        ApplyBreakableVisual();                 // on/off·window 표시 초기화
        if (breakable && !isWindow) Register(); // 부술 수 있고 창문 아닌 것만 S 대상으로 등록
        ApplyOpposite();                        // 반대편 벽 처리 (창문/breakable이면 앞이 뚫려 보이게)
    }

    // 반대편(연결된 방) 벽 처리:
    //  - isWindow: 반대편도 창문 구멍(pane 없음)
    //  - breakable: 반대편 벽 메쉬 숨김 → 앞이 없는 것처럼 보임 (안 부순 상태에선 콜라이더 유지)
    private void ApplyOpposite()
    {
        if (!isWindow && !breakable) return;
        var opp = FindOpposite();
        if (opp == null) return;
        if (isWindow) opp.BecomeWindowOpening();
        else opp.ShowWallMesh(false);
    }

    // 반대편 벽: 창문 pane은 안 만들고 '구멍'만 (일반 벽 메쉬 끄기).
    // pane은 한쪽에만 있어야 겹침/z-fight 없이 양쪽에서 보임. 부술 수 없고 콜라이더는 유지(통과 X).
    public void BecomeWindowOpening()
    {
        if (isWindow) return;
        isWindow = true;
        Unregister();
        if (window != null) window.SetActive(false);
        if (breakableObject != null) breakableObject.SetActive(false);
        if (normalWall != null)
        {
            var mr = normalWall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }
    }

    // 표시 전환:
    //  - isWindow: 일반 벽 메쉬 끄고 window만 켬 (부술 수 없음)
    //  - breakable: breakableObject 켜고 일반 벽 메쉬 끔
    //  - 그 외(일반 벽): 일반 벽 메쉬만 켬
    // normalWall 안에 자식 메쉬가 있을 수 있어 오브젝트 통째로 끄지 않고 '자기 MeshRenderer'만 토글.
    private void ApplyBreakableVisual()
    {
        bool win = isWindow;
        if (win) EnsureWindowInstance();        // 프리팹 선택 방식이면 생성
        if (window != null) window.SetActive(win);
        if (breakableObject != null) breakableObject.SetActive(breakable && !win);
        if (normalWall != null)
        {
            var mr = normalWall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = !(breakable || win);   // breakable이나 window면 일반 벽 메쉬 끔
        }

        // breakable 비주얼 색 틴트
        if (tintBreakable && breakable && !win && breakableObject != null)
        {
            var rs = breakableObject.GetComponentsInChildren<Renderer>(true);
            ActivationColor.Apply(rs, breakableColor, breakableColorProperty);
        }
    }

    // window가 비어있고 프리팹 후보가 있으면 windowIndex로 골라 자식으로 하나 생성
    private void EnsureWindowInstance()
    {
        if (window != null) return;
        if (windowPrefabs == null || windowPrefabs.Length == 0) return;
        int idx = Mathf.Clamp(windowIndex, 0, windowPrefabs.Length - 1);
        var prefab = windowPrefabs[idx];
        if (prefab == null) return;
        window = Instantiate(prefab, transform.position, transform.rotation, transform);
        window.transform.localScale = Vector3.one * 0.05f;   // 창문 스케일
    }

    // roomA = 계층상 부모 방, roomB = 그 반대편(이 벽에 가장 가까운 다른 방).
    private void AutoDetectRooms()
    {
        if (roomA == null) roomA = GetComponentInParent<Room>();

        if (roomB == null)
        {
            Vector3 p = transform.position;
            Room best = null;
            float bd = float.MaxValue;
            foreach (var room in FindObjectsOfType<Room>())
            {
                if (room == roomA) continue;
                var col = room.homeBound != null ? room.homeBound : room.GetComponent<Collider>();
                if (col == null) continue;
                float d = col.bounds.SqrDistance(p);   // 방 안이면 0
                if (d < bd) { bd = d; best = room; }
            }
            roomB = best;
        }
    }

    private void Register()
    {
        if (roomA != null) roomA.RegisterBreakable(this);
        if (roomB != null) roomB.RegisterBreakable(this);
    }
    private void Unregister()
    {
        if (roomA != null) roomA.UnregisterBreakable(this);
        if (roomB != null) roomB.UnregisterBreakable(this);
    }

    // 런타임에 부술 수 있음/없음 토글
    public void SetBreakable(bool on)
    {
        if (breakable == on) return;
        breakable = on;
        ApplyBreakableVisual();       // 표시 전환
        if (on && !Broken && !isWindow) Register();
        else Unregister();

        // 반대편 벽: on이면 숨겨서 앞이 뚫려 보이게, off면 복구
        var opp = FindOpposite();
        if (opp != null && !isWindow) opp.ShowWallMesh(!on);
    }

    // S가 닿을 때마다 호출
    public void Hit()
    {
        if (Broken || !breakable) return;
        hits++;
        Popup.Instance?.BurstAt(transform.position);   // 내용 없이 이펙트만
        if (hits >= hitsToBreak) Break();
    }

    public void Break()
    {
        if (Broken) return;
        Broken = true;

        Unregister();   // 더 이상 부술 대상 아님

        // 부서진 자리를 "통로"로 등록 → migration이 이 틈으로 넘어감
        if (roomA != null) roomA.RegisterPassage(this);
        if (roomB != null) roomB.RegisterPassage(this);

        // 콜라이더 꺼서 물리적으로 통과 가능하게
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        // 벽 메시 숨김
        if (wallMesh != null) wallMesh.SetActive(false);
        else { var mr = GetComponentInChildren<MeshRenderer>(); if (mr != null) mr.enabled = false; }

        BreakOpposite();   // 반대편(연결된 방) 같은 위치 벽도 부숨
    }

    // 같은 위치의 반대편 벽 하나를 찾아 같이 부숨 (Broken 가드로 재귀 정지)
    private void BreakOpposite()
    {
        var opp = FindOpposite();
        if (opp != null && !opp.Broken) opp.Break();
    }

    // oppositeBreakRange 안의 가장 가까운 다른 BreakableWall (반대편 방 같은 위치 벽)
    private BreakableWall FindOpposite()
    {
        var hits = Physics.OverlapSphere(transform.position, oppositeBreakRange);
        BreakableWall best = null;
        float bd = float.MaxValue;
        foreach (var h in hits)
        {
            var bw = h.GetComponentInParent<BreakableWall>();
            if (bw == null || bw == this) continue;
            float d = Vector3.Distance(transform.position, bw.transform.position);
            if (d < bd) { bd = d; best = bw; }
        }
        return best;
    }

    // 반대편이 호출: 벽 메쉬만 숨김/보임 (부술 수 있음·콜라이더는 유지)
    public void ShowWallMesh(bool show)
    {
        if (normalWall == null) return;
        var mr = normalWall.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = show;
    }

    // 벽 콜라이더 표면까지의 거리 (중심이 아니라 표면 — 큰 벽도 닿음 판정 됨)
    public float DistanceFrom(Vector3 p)
    {
        var col = GetComponentInChildren<Collider>();
        if (col != null) return Vector3.Distance(p, col.ClosestPoint(p));
        return Vector3.Distance(p, transform.position);
    }

    // 이 통로 반대편 방
    public Room GetOtherRoom(Room from)
    {
        if (from == roomA) return roomB;
        if (from == roomB) return roomA;
        return null;
    }
}
