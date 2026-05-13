using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Creature self;
    public Camera cam;

    [Header("Aim")]
    [Tooltip("화면 중앙에서 얼마나 허용할지(px). 0이면 정확히 중앙만.")]
    public float screenRadiusPx = 500f;

    [Tooltip("레이캐스트 최대 거리")]
    public float maxDistance = 1000f;

    [Tooltip("TargetCollider가 붙은 오브젝트의 레이어만 맞추면 성능 좋음(선택). 0이면 전체.")]
    public LayerMask hitMask = ~0;

    [Header("Damage")]
    public int damage = 1;

    [Header("Debug")]
    public bool drawRay = true;

    private void Awake()
    {
        if (self == null) self = GetComponent<Creature>();
        if (cam == null) cam = Camera.main;
        LayerMask hitMask = LayerMask.GetMask("Creature");
    }

    public void TryHitAtScreenCenter()
    {
        if (cam == null) return;

        //가운데 배치 
        Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        // 1) 중앙 픽셀에서 레이
        Ray ray = cam.ScreenPointToRay(center);

        if (drawRay)
            Debug.DrawRay(ray.origin, ray.direction * Mathf.Min(maxDistance, 30f), Color.yellow, 0.5f);

        float radius = 1f; // 0.1~0.5 정도로 튜닝
        if (Physics.SphereCast(ray, radius, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Collide))
        {
            // 2) 맞은 지점이 TargetCollider(또는 자식)인지 확인
            var tc = hit.collider.GetComponentInParent<TargetCollider>();
            if (tc == null)
            {
                Debug.Log("no trigger collider");
                return;
            }

            // 3) 실제 Creature 찾기 (TargetCollider는 Creature 하위에 있으니 InParent로)
            var creature = tc.GetComponentInParent<Creature>();
            if (creature == null) return;

            creature.TakeDamage(damage, self);
            Debug.Log($"[PlayerClickAttack] Hit {creature.name}, -{damage} HP");
        }
    }
}