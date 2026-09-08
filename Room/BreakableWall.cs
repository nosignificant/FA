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
        if (breakable) Register();   // 부술 수 있는 것만 S 대상으로 등록
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
        if (on && !Broken) Register();
        else Unregister();
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
        var hits = Physics.OverlapSphere(transform.position, oppositeBreakRange);
        BreakableWall best = null;
        float bd = float.MaxValue;
        foreach (var h in hits)
        {
            var bw = h.GetComponentInParent<BreakableWall>();
            if (bw == null || bw == this || bw.Broken) continue;
            float d = Vector3.Distance(transform.position, bw.transform.position);
            if (d < bd) { bd = d; best = bw; }
        }
        best?.Break();
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
