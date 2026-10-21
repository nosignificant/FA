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

    [Header("시작 상태")]
    [Tooltip("게임 시작부터 이미 부서진 상태로 둠 (통로로 열려있음)")]
    public bool startBroken = false;

    [Header("breakable 색 (방 상태 L/A 따라 tint)")]
    [Tooltip("켜면 색을 roomA의 활성화 상태(L/A/None)에 따라 칠함")]
    public bool tintBreakable = false;
    [Tooltip("색(tint)을 적용할 부모. 이 오브젝트 하위 Renderer 전부 칠함. 비우면 Breakable Object 사용")]
    public GameObject tintTarget;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color, SpriteRenderer는 .color)")]
    public string breakableColorProperty = "";
    public Color noneColor = Color.white;
    public Color lColor = new Color(0.6f, 1f, 0.3f);
    public Color aColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("내구도")]
    [Min(1)] public int hitsToBreak = 3;
    [Tooltip("부서질 때 이 반경 안의 반대편(연결된 방) 벽도 같이 부숨")]
    public float oppositeBreakRange = 5f;

    public bool Broken { get; private set; }
    public Vector3 Position => transform.position;

    private int hits;

#if UNITY_EDITOR
    // 에디터에서 breakable 토글 시 즉시 표시 반영 (breakable 꺼지면 breakableObject active false)
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (breakableObject != null) breakableObject.SetActive(breakable);
            if (tintTarget != null) tintTarget.SetActive(breakable);
        };
    }
#endif

    private void Start()
    {
        if (roomA == null || roomB == null) AutoDetectRooms();
        ApplyBreakableVisual();                 // on/off 표시 초기화
        if (breakable) Register();              // 부술 수 있는 것만 S 대상으로 등록
        // 반대편 벽 메쉬 자동 숨김 제거 — 수동으로 관리

        // 방 상태 바뀔 때 색 갱신
        if (tintBreakable && roomA != null) roomA.OnActivationChanged += OnRoomActivation;

        if (startBroken) Break();               // 시작부터 부서진 상태 (통로 열림)
    }

    private void OnDestroy()
    {
        if (roomA != null) roomA.OnActivationChanged -= OnRoomActivation;
    }

    private void OnRoomActivation(Room.RoomActivation _) => ApplyBreakableVisual();

    // 표시 전환:
    //  - breakable: breakableObject 켜고 일반 벽 메쉬 끔
    //  - 그 외(일반 벽): 일반 벽 메쉬만 켬
    // normalWall 안에 자식 메쉬가 있을 수 있어 오브젝트 통째로 끄지 않고 '자기 MeshRenderer'만 토글.
    private void ApplyBreakableVisual()
    {
        // 부서지면(Broken) breakableObject(조각)만 꺼둠 — tint 갱신 때 다시 켜지는 것 방지.
        // 부모(tintTarget)는 Broken이어도 유지해서 틀/틴트를 남긴다.
        if (breakableObject != null) breakableObject.SetActive(breakable && !Broken);
        if (tintTarget != null) tintTarget.SetActive(breakable);
        if (normalWall != null)
        {
            var mr = normalWall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = !breakable;   // breakable이면 일반 벽 메쉬 끔
        }

        // 색 적용 대상: tintTarget 지정 시 그것, 없으면 breakableObject (부서져도 부모 틴트는 유지)
        GameObject colorRoot = tintTarget != null ? tintTarget : breakableObject;
        if (tintBreakable && breakable && colorRoot != null)
        {
            var state = roomA != null ? roomA.Activation : Room.RoomActivation.None;
            Color c = state == Room.RoomActivation.A ? aColor
                    : state == Room.RoomActivation.L ? lColor : noneColor;
            var rs = colorRoot.GetComponentsInChildren<Renderer>(true);
            ActivationColor.Apply(rs, c, breakableColorProperty);
        }
    }

    // roomA = 계층상 부모 방, roomB = 그 반대편(이 벽에 가장 가까운 다른 방).
    private void AutoDetectRooms()
    {
        if (roomA == null) roomA = GetComponentInParent<Room>();
        if (roomB != null) return;

        Vector3 p = transform.position;

        // 1순위: roomA 중심에서 벽 쪽(= 밖) 방향으로 한 발 나간 점을 포함하는 방 = 벽 너머 방
        if (roomA != null && roomA.homeBound != null)
        {
            Vector3 away = p - roomA.homeBound.bounds.center; away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
            {
                away.Normalize();
                Vector3 probe = p + away * Mathf.Max(1f, oppositeBreakRange);
                foreach (var room in FindObjectsOfType<Room>())
                {
                    if (room == roomA || room.homeBound == null) continue;
                    if (room.homeBound.bounds.Contains(probe)) { roomB = room; break; }
                }
            }
        }

        // 폴백: 가장 가까운 다른 방
        if (roomB == null)
        {
            Room best = null;
            float bd = float.MaxValue;
            foreach (var room in FindObjectsOfType<Room>())
            {
                if (room == roomA || room.homeBound == null) continue;
                float d = room.homeBound.bounds.SqrDistance(p);   // 방 안이면 0
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
        if (on && !Broken) Register();
        else Unregister();
        // 반대편 벽 메쉬 자동 숨김/복구 제거 — 수동으로 관리
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

        // 부술 수 있는 벽 비주얼 오브젝트 비활성화 → 그 자리가 뚫려 통로가 됨
        if (breakableObject != null) breakableObject.SetActive(false);

        // 벽 메시 숨김
        if (wallMesh != null) wallMesh.SetActive(false);
        else { var mr = GetComponentInChildren<MeshRenderer>(); if (mr != null) mr.enabled = false; }

        BreakOpposite();   // 반대편(연결된 방) 같은 위치 벽도 부숨
    }

    // 같은 방 경계를 이루는 같은 방향의 반대편 벽만 같이 부숨 (Broken 가드로 재귀 정지)
    private void BreakOpposite()
    {
        var opp = FindOpposite();
        if (opp != null && !opp.Broken) opp.Break();
    }

    // 반대편 방과 연결되고 벽 방향도 같은 후보 중 가장 가까운 것만 선택한다.
    // 단순 거리만 보면 모서리에서 직각으로 만난 다른 방의 벽까지 연쇄 파괴될 수 있다.
    private BreakableWall FindOpposite()
    {
        var hits = Physics.OverlapSphere(transform.position, oppositeBreakRange);
        BreakableWall best = null;
        float bd = float.MaxValue;
        foreach (var h in hits)
        {
            var bw = h.GetComponentInParent<BreakableWall>();
            if (bw == null || bw == this || bw.Broken || !bw.breakable) continue;
            if (!ConnectsSameRooms(bw)) continue;
            if (Mathf.Abs(Vector3.Dot(transform.forward, bw.transform.forward)) < 0.9f) continue;

            float d = Vector3.Distance(transform.position, bw.transform.position);
            if (d < bd) { bd = d; best = bw; }
        }
        return best;
    }

    private bool ConnectsSameRooms(BreakableWall other)
    {
        if (roomA == null || roomB == null || other.roomA == null || other.roomB == null)
            return false;

        return (other.roomA == roomA && other.roomB == roomB)
            || (other.roomA == roomB && other.roomB == roomA);
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
