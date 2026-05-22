using System;
using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;


[DisallowMultipleComponent]
public sealed class TargetControl : MonoBehaviour
{
    [SerializeField] public Transform movementTarget;
    public float threshold = 10f;
    public Transform rotatingTarget;

    public bool isBoidChildren = false;

    public bool isEngineLeg = false;
    public bool isQuadLeg = false;
    public bool isFollowingRB = false;
    [Tooltip("flee 또는 migrate 중일 때 moveSpeed에 더해줄 가속량")]
    public float urgentSpeedBonus = 5f;
    public event Action<Transform> TargetChanged;

    private Vector3 smoothedDir;

    private Creature self;
    private EngineLegs engineLegs;
    private QuadLegs quadLegs;
    private float engineBaseSpeed, quadBaseSpeed;

    void Awake()
    {
        self = GetComponentInParent<Creature>();
        if (isEngineLeg)
        {
            engineLegs = GetComponentInChildren<EngineLegs>();
            if (engineLegs != null) engineBaseSpeed = engineLegs.moveSpeed;
        }
        if (isQuadLeg)
        {
            quadLegs = GetComponentInChildren<QuadLegs>();
            if (quadLegs != null) quadBaseSpeed = quadLegs.moveSpeed;
        }
    }

    void Update()
    {
        if (movementTarget == null) return;

        // 도망/이주 중이면 다리 속도 +bonus
        bool urgent = self != null &&
            (self.intent == CreatureIntent.Flee || self.wantToMigrate);
        float add = urgent ? urgentSpeedBonus : 0f;

        if (engineLegs != null) engineLegs.moveSpeed = engineBaseSpeed + add;
        if (quadLegs != null)   quadLegs.moveSpeed   = quadBaseSpeed + add;
    }

    public void SetMovementTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        movementTarget = newTarget;
        TargetChanged?.Invoke(movementTarget);
        if (isBoidChildren) SetBoidTarget();
        if (isEngineLeg) SetEngineTarget();
        if (isQuadLeg) SetQuadTarget();
        if (isFollowingRB) SetFollowingRBTarget();

    }
    public void GoToTarget()
    {
        if (movementTarget == null || rotatingTarget == null) return;

        float d = Vector3.Distance(movementTarget.position, rotatingTarget.position);
        Vector3 dir = movementTarget.position - rotatingTarget.position;
        if (dir.sqrMagnitude < 10f || d < threshold) return;

        smoothedDir = Vector3.Slerp(smoothedDir, dir, Time.deltaTime * 5f);

        Quaternion targetRot = Quaternion.LookRotation(smoothedDir);
        rotatingTarget.rotation = Quaternion.Slerp(rotatingTarget.rotation, targetRot, Time.deltaTime * 8f);
    }

    public void SetBoidTarget()
    {
        BoidFlocking[] boids = GetComponentsInChildren<BoidFlocking>();
        foreach (var b in boids) b.target = movementTarget;

    }

    public void SetEngineTarget()
    {
        EngineLegs engine = GetComponentInChildren<EngineLegs>();
        engine.followingTarget = movementTarget;
    }

    public void SetQuadTarget()
    {
        QuadLegs qa = GetComponentInChildren<QuadLegs>();
        qa.followingTarget = movementTarget;
    }

    public void SetFollowingRBTarget()
    {
        RBpart rb = GetComponentInChildren<RBpart>();
        if (rb == null) return;
        rb.target = movementTarget;

    }
}
