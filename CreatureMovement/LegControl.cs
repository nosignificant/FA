// File: Assets/script/6.creatureTest/LegControl.cs
using UnityEngine;

public class LegControl : MonoBehaviour
{
    [Header("References")]
    public Leg[] legs;
    [SerializeField] private TargetControl targetControl;
    public Transform followingTarget;
    public TargetControl tc;
    public bool useKabschCenter = false;
    public bool followSetTarget = false;
    public bool followTC = false;

    private void Awake()
    {
        if (targetControl == null) targetControl = GetComponent<TargetControl>();
        if (targetControl == null) targetControl = GetComponentInParent<TargetControl>();
    }
    void Update()
    {
        if (followingTarget) ApplyTargetToLegs(followingTarget);
    }

    private void OnEnable()
    {
        if (targetControl != null)
            targetControl.TargetChanged += ApplyTargetToLegs;
    }

    private void OnDisable()
    {
        if (targetControl != null)
            targetControl.TargetChanged -= ApplyTargetToLegs;
    }

    private void Start()
    {
        if (legs == null || legs.Length == 0)
            legs = GetComponentsInChildren<Leg>();

        if (targetControl != null)
            ApplyTargetToLegs(targetControl.movementTarget);
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
        // 너무 가까운 스텝은 무시ㅋ
        if ((destPos - legAdding.tipTarget.position).sqrMagnitude < 0.1f * 0.1f)
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