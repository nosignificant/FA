using System.Collections.Generic;
using UnityEngine;

// 동반자(p53)용: 스캐너·관계 무시하고 인스펙터로 지정한 타겟(Transform)으로만 이동.
// 너무 붙지 않게 followDistance 링 위의 점을 proxy 목표로 준다. 타겟 미지정이면 제자리.
public class FollowPlayerState : ThinkState
{
    public float followDistance = 3f;   // 타겟으로부터 유지할 거리(P53Think.followDistance로 덮임)

    private float _guideStuckSince = -1f;   // 가이드 중 목표에 못 도달하기 시작한 시각(-1=도달/비가이드)

    public FollowPlayerState(Think2 think) : base(think) { }

    public override void Refresh(List<Vector3> points)
    {
        var p53 = think as P53Think;
        Transform tgt = p53 != null ? p53.MoveTarget : null;

        // 타겟 미지정: 제자리 유지 (플레이어 추종 안 함)
        if (tgt == null)
        {
            newTarget.point = GetSelfPos();
            newTarget.creature = null;
            return;
        }

        float keep = p53 != null ? p53.followDistance : followDistance;

        Vector3 tp = tgt.position;
        Vector3 self = GetSelfPos();
        Vector3 off = self - tp;
        float m = off.magnitude;

        // 타겟으로부터 keep 거리 지점을 목표로 → 그 거리에서 멈춤 (keep=0이면 타겟점)
        Vector3 followPoint = (keep > 0f && m > 0.01f) ? tp + off / m * keep : tp;

        // 너무 멀어지면(벽·단절로 못 감) 타겟 옆으로 순간이동 — 가이드 중(guideTarget 지정)엔 거리 워프 안 함(걸어서 안내)
        if (p53 != null && p53.warpDistance > 0f && m > p53.warpDistance && p53.guideTarget == null)
            p53.WarpTo(followPoint);

        // 가이드 중: 일정 시간 안에 목표에 도달 못하면(벽·단절로 막힘) 목표 옆으로 순간이동
        if (p53 != null && p53.guideTarget != null && p53.guideWarpTime > 0f)
        {
            bool arrived = m <= keep + p53.guideArriveSlack;
            if (arrived)
            {
                _guideStuckSince = -1f;
            }
            else
            {
                if (_guideStuckSince < 0f) _guideStuckSince = Time.time;
                else if (Time.time - _guideStuckSince >= p53.guideWarpTime)
                {
                    p53.WarpTo(followPoint);
                    _guideStuckSince = -1f;
                }
            }
        }
        else
        {
            _guideStuckSince = -1f;
        }

        newTarget.point = followPoint;
        newTarget.creature = null;
    }
}
