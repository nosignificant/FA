using System;
using UnityEngine;
using System.Collections.Generic;


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
    public event Action<Transform> TargetChanged;

    private Vector3 smoothedDir;

    void Update()
    {
        if (movementTarget == null) return;

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
