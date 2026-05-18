using UnityEngine;

public class TentacleLeg : MonoBehaviour
{
    [System.Serializable]
    public class TentacleSlot
    {
        public Tentacle tentacle;
        public Transform oldTarget;     // 공중 기본 포즈 (원래 tentacle.target)
        public Transform footTarget;    // 땅 디딜 목표 (tentacle.target에 연결)
    }

    [Header("Components")]
    public LayerMask ground;
    public Transform target;          // 따라갈 대상 (이동 목표)
    public Transform body;            // body↔tipTarget 거리 기준
    public TentacleSlot[] tentacles;

    [Header("Settings")]
    public float stride = 2f;
    [Tooltip("top↔foot 거리가 이보다 멀면 닿을 수 없다고 보고 공중 유지")]
    public float maxLegLength = 5f;
    [Tooltip("body↔tipTarget 거리가 이 이하면 굳이 안 디디고 공중 유지")]
    public float bodyToTipThreshold = 1.5f;
    [Tooltip("스텝 지점에서 가장 가까운 땅이 이 거리 이내여야 디딤")]
    public float maxGroundDist = 3f;
    [Tooltip("땅 탐색 반경")]
    public float groundSearchRadius = 5f;

    void Start()
    {
        if (body == null) body = transform;
        initTentacles();
    }

    void Update()
    {
        if (target == null || tentacles == null) return;

        for (int i = 0; i < tentacles.Length; i++)
        {
            var ten = tentacles[i];
            if (ten == null || ten.tentacle == null) continue;

            Transform topT = ten.tentacle.top;
            Transform footT = ten.tentacle.foot;
            Transform tip = ten.tentacle.tipTarget;
            if (topT == null || footT == null || tip == null) continue;

            float legLen = Vector3.Distance(topT.position, footT.position);
            float bodyToTip = Vector3.Distance(body.position, tip.position);

            // 1) 다리 너무 늘어남 OR body가 발끝에 충분히 가까움 → 공중 기본 포즈
            if (legLen > maxLegLength || bodyToTip <= bodyToTipThreshold)
            {
                ten.tentacle.target = ten.oldTarget;
                continue;
            }

            // 2) 그 외 → target 쪽으로 stride만큼 앞 지점에서 땅 찾기
            Vector3 dir = (target.position - tip.position).normalized;
            Vector3 destPos = tip.position + dir * stride;

            Vector3 nearest = FootUtil.SetTargetNearest(destPos, ground, groundSearchRadius);
            float groundDist = Vector3.Distance(destPos, nearest);

            if (groundDist <= maxGroundDist)
            {
                // 가까운 땅에 발 유지
                ten.footTarget.position = nearest;
                ten.tentacle.target = ten.footTarget;
            }
            else
            {
                // 디딜 땅 없음 → 공중
                ten.tentacle.target = ten.oldTarget;
            }
        }
    }

    private void initTentacles()
    {
        Tentacle[] found = GetComponentsInChildren<Tentacle>();
        tentacles = new TentacleSlot[found.Length];
        for (int i = 0; i < found.Length; i++)
        {
            var t = found[i];

            // footTarget은 TentacleLeg 밑에 둠 (target 하위 X — 분리)
            var go = new GameObject($"{t.name}_footTarget");
            go.transform.SetParent(transform);
            go.transform.position = t.foot != null ? t.foot.position : t.transform.position;

            tentacles[i] = new TentacleSlot
            {
                tentacle = t,
                oldTarget = t.target,        // 원래 target = 공중 기본 포즈
                footTarget = go.transform
            };
        }
    }
}
