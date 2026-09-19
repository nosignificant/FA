using System;
using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;


[DisallowMultipleComponent]
public sealed class TargetControl : MonoBehaviour
{
    // 이동 방식 (하나만 선택)
    public enum MoveMode
    {
        None,          // 이동 컴포넌트에 target 전달 안 함
        BoidChildren,  // 하위 Boid들이 target으로
        EngineLeg,
        QuadLeg,
        FollowingRB,
        LegHead,       // 머리(LegHead)가 target으로
        Tentacle,      // 하위 Tentacle 전부 target으로 (weed 등)
        Leg,           // 하위 Leg 전부 target 직접 전달 (발 고정, 머리만 — 텐타클 여러 개처럼)
        CCDIK,         // 하위 CCDIK 전부 target으로
    }

    [SerializeField] public Transform movementTarget;
    public float threshold = 10f;
    public Transform rotatingTarget;

    [Tooltip("이동 방식 (하나 선택)")]
    public MoveMode moveMode = MoveMode.None;

    [Tooltip("flee 또는 migrate 중일 때 moveSpeed에 더해줄 가속량")]
    public float urgentSpeedBonus = 5f;
    [Tooltip("플레이어 조종 중 다리 속도에 더해줄 가속량 (proxy를 잘 따라가게)")]
    public float controlledSpeedBonus = 8f;
    public event Action<Transform> TargetChanged;

    private Vector3 smoothedDir;

    private Creature self;
    private EngineLegs engineLegs;
    private QuadLegs quadLegs;
    private float engineBaseSpeed, quadBaseSpeed;

    void Awake()
    {
        self = GetComponentInParent<Creature>();
        if (moveMode == MoveMode.EngineLeg)
        {
            engineLegs = GetComponentInChildren<EngineLegs>();
            if (engineLegs != null) engineBaseSpeed = engineLegs.moveSpeed;
        }
        if (moveMode == MoveMode.QuadLeg)
        {
            quadLegs = GetComponentInChildren<QuadLegs>();
            if (quadLegs != null) quadBaseSpeed = quadLegs.moveSpeed;
        }
    }

    void Update()
    {
        if (movementTarget == null) return;

        // 도망/이주 중이면 +urgent, 조종 중이면 +controlled (proxy 따라잡게)
        float add = 0f;
        if (self != null)
        {
            if (self.intent == CreatureIntent.Controlled) add = controlledSpeedBonus;
            else if (self.intent == CreatureIntent.Flee)  add = urgentSpeedBonus;
        }

        if (engineLegs != null) engineLegs.moveSpeed = engineBaseSpeed + add;
        if (quadLegs != null) quadLegs.moveSpeed = quadBaseSpeed + add;
    }

    public void SetMovementTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        movementTarget = newTarget;
        TargetChanged?.Invoke(movementTarget);
        switch (moveMode)
        {
            case MoveMode.BoidChildren: SetBoidTarget(); break;
            case MoveMode.EngineLeg:    SetEngineTarget(); break;
            case MoveMode.QuadLeg:      SetQuadTarget(); break;
            case MoveMode.FollowingRB:  SetFollowingRBTarget(); break;
            case MoveMode.LegHead:      SetLegHead(); break;
            case MoveMode.Tentacle:     SetTentacleTarget(); break;
            case MoveMode.Leg:          SetLegsTarget(); break;
            case MoveMode.CCDIK:        SetCCDIKTarget(); break;
        }
    }

    // 하위 Leg 전부에게 target 직접 전달 (발 고정 + 머리만 target으로 → 여러 텐타클처럼)
    public void SetLegsTarget()
    {
        Leg[] legs = GetComponentsInChildren<Leg>();
        foreach (var l in legs)
            if (l != null) l.SetTarget(movementTarget);
    }

    // 하위 CCDIK 전부에게 target 전달
    public void SetCCDIKTarget()
    {
        CCDIK[] iks = GetComponentsInChildren<CCDIK>();
        foreach (var ik in iks)
            if (ik != null) ik.SetTarget(movementTarget);
    }

    // 하위 Tentacle 전부에게 같은 target 전달 (weed: proxy를 모든 촉수가 향하게)
    public void SetTentacleTarget()
    {
        Tentacle[] tentacles = GetComponentsInChildren<Tentacle>();
        foreach (var t in tentacles)
            if (t != null) t.target = movementTarget;
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

    public void SetLegHead()
    {
        LegHead lh = GetComponentInChildren<LegHead>();
        lh.target = movementTarget;
    }
}
