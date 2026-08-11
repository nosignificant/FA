using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// weed 전용:
// - 스캔 반경 안에 다른 생물이 있으면 그쪽으로 proxy를 뻗어 반응
// - 그 생물이 반경 밖으로 벗어나면 추적을 멈추고 일반 wander(배회)로 복귀
// - 목적지 도달 후 dwellTime만큼 머문 뒤 새 점을 고름 (옵션)
public class WeedWanderState : ThinkState
{
    public float reachThreshold = 5f;   // 목적지 도달 판정 거리
    public float dwellTime = 0f;        // 도달 후 머무는 시간(초)

    private bool hasPoint = false;
    private float reachedTime = -1f;

    public WeedWanderState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        hasPoint = false;
        reachedTime = -1f;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        Creature target = FindNearestInRange();
        if (target != null)
        {
            newTarget.point = target.rootTransform.position;   // 반경 안 생물 추적
            newTarget.creature = target;                       // HUD·상호작용에서 타겟 생물 표시용
            hasPoint = false;                                  // 놓치면 새 wander 시작
            reachedTime = -1f;
            return;
        }

        // 반경 밖/없음 → 일반 wander (배회)
        newTarget.creature = null;
        if (!hasPoint)
        {
            newTarget.point = GetNewPoint(points);
            hasPoint = true;
            reachedTime = -1f;
        }

        // 목적지 도달 → dwellTime 머문 뒤 새 점
        float d = Vector3.Distance(GetSelfPos(), newTarget.point);
        if (d < reachThreshold)
        {
            if (reachedTime < 0f) reachedTime = Time.time;
            if (Time.time - reachedTime >= dwellTime)
            {
                oldTarget = newTarget;
                newTarget.point = GetNewPoint(points);
                reachedTime = -1f;
            }
        }
        else
        {
            reachedTime = -1f;   // 아직 이동 중이면 도달 타이머 리셋
        }
    }

    // 스캔 반경 안의 가장 가까운 유효 타겟 (반경 밖은 무시 → 추적 중단)
    private Creature FindNearestInRange()
    {
        if (detected == null) return null;

        float r = think.scanner != null ? think.scanner.scanRadius : float.MaxValue;
        float rSqr = r * r;
        Vector3 selfPos = GetSelfPos();
        Creature best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < detected.Count; i++)
        {
            var c = detected[i];
            if (!think.IsValidTarget(c)) continue;

            float sqr = (c.rootTransform.position - selfPos).sqrMagnitude;
            if (sqr > rSqr) continue;   // 반경 밖 → 추적 안 함
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }
}
