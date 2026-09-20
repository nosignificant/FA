using System.Collections.Generic;
using UnityEngine;

// CCDIK 생물용: weed 반응/배회로 정한 지점을 self 기준 '반경 내'로 clamp + 부드럽게 lerp.
// proxy가 이 점으로 이동하고, CCDIK는 proxy(=movementTarget)를 바라봄.
public class CCDIKWanderState : WeedWanderState
{
    public float maxRadius = 8f;       // proxy가 self에서 벗어날 수 있는 최대 반경
    public float followFactor = 0.3f;  // 틱당 proxy가 목표로 다가가는 비율(0~1)

    public CCDIKWanderState(Think2 think) : base(think) { }

    public override void Refresh(List<Vector3> points)
    {
        base.Refresh(points);   // weed: 반응 대상 있으면 그쪽 / 없으면 배회로 newTarget.point 결정

        Vector3 self = GetSelfPos();
        Vector3 off = newTarget.point - self;
        if (off.magnitude > maxRadius)                    // 반경 밖이면 경계로 당김
            newTarget.point = self + off.normalized * maxRadius;

        // 현재 proxy 위치에서 목표로 부드럽게 (틱당 followFactor만큼)
        newTarget.point = Vector3.Lerp(think.ProxyTarget.position, newTarget.point, Mathf.Clamp01(followFactor));
    }
}
