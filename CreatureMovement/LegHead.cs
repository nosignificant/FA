using UnityEngine;

public class LegHead : MonoBehaviour
{
    [Header("References")]
    public Leg[] legs;
    public Transform target;
    public TargetControl tc;
    public LayerMask ground;
    public float height = 10f;
    public float speed = 5f;

    private void Awake()
    {
        if (tc == null) tc = GetComponent<TargetControl>();
        if (tc == null) tc = GetComponentInParent<TargetControl>();
        ground = LayerMask.GetMask("Ground");
    }

    private void Start()
    {
        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>();
        ApplyTargetToLegs(transform);

    }

    void Update()
    {
        if (target != null) LerpHead();

    }

    private void LerpHead()
    {
        Vector3 groundPos = FootUtil.SetTargetGround(target.position, ground);
        Vector3 targetPos = new Vector3(target.position.x, groundPos.y + height, target.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
    }

    private void ApplyTargetToLegs(Transform target)
    {
        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>();

        if (legs == null) return;

        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            leg.SetTarget(target);
        }
    }

    public bool CheckValidFootPos(Vector3 destPos, Leg legAdding)
    {
        if ((destPos - legAdding.tipTarget.position).sqrMagnitude < 0.2f * 0.2f)
            return false;

        if (legs == null) return true;

        float minDist = legAdding.stride * 0.7f;
        float minDistSqr = minDist * minDist;

        for (int i = 0; i < legs.Length; i++)
        {
            Leg otherLeg = legs[i];
            if (otherLeg == null) continue;
            if (otherLeg == legAdding) continue;

            Transform tip = otherLeg.tipTarget;
            if ((tip.position - destPos).sqrMagnitude < minDistSqr)
                return false;
        }
        return true;
    }
}
