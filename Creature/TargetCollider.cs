// File: Assets/script/TargetCollider.cs
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TargetCollider : MonoBehaviour
{
    [Header("Link")]
    [SerializeField] private Creature creature;

    [Header("Center Source")]
    public bool useRenderers = true;   // true: Renderer 평균 / false: Collider 평균

    [Header("Offset")]
    public Vector3 worldOffset = Vector3.zero;

    private Renderer[] renderersCache;
    private Collider[] collidersCache;

    void Start()
    {
        if (creature == null)
            creature = GetComponentInParent<Creature>();

        // 캐시(성능)
        renderersCache = creature.GetComponentsInChildren<Renderer>();
        collidersCache = creature.GetComponentsInChildren<Collider>();

        gameObject.layer = LayerMask.NameToLayer("Creature"); ;

        // 시작 위치 한 번 맞춤
        MoveToCenter();
    }

    private void LateUpdate()
    {
        MoveToCenter();
    }

    [ContextMenu("Move To Center Now")]
    public void MoveToCenter()
    {
        Vector3 center = useRenderers
            ? GetAverageCenterFromRenderers(renderersCache)
            : GetAverageCenterFromColliders(collidersCache);

        transform.position = center + worldOffset;
    }

    private static Vector3 GetAverageCenterFromRenderers(Renderer[] rs)
    {
        if (rs == null || rs.Length == 0) return Vector3.zero;

        // 전체 bounds 합쳐서 중심(가장 흔한 “평균 위치”)
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            b.Encapsulate(rs[i].bounds);
        }
        return b.center;
    }

    private static Vector3 GetAverageCenterFromColliders(Collider[] cs)
    {
        if (cs == null || cs.Length == 0) return Vector3.zero;

        Bounds b = cs[0].bounds;
        for (int i = 1; i < cs.Length; i++)
        {
            if (cs[i] == null) continue;
            b.Encapsulate(cs[i].bounds);
        }
        return b.center;
    }
}