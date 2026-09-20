using UnityEngine;

// CCDIK 생물 전용 Think: weed처럼 반응(주변 생물)/배회하되, proxy를 반경 내로 제한 + 부드럽게 이동.
// CCDIK는 이 proxy(=movementTarget)를 바라봄 → TargetControl moveMode=CCDIK 로 연결.
public class CCDIKThink : WeedThink
{
    [Header("CCDIK proxy 이동")]
    [Tooltip("proxy가 자기 위치에서 벗어날 최대 반경")]
    public float proxyMaxRadius = 8f;
    [Tooltip("틱당 proxy가 목표로 다가가는 비율(0~1, 클수록 즉각적)")]
    [Range(0f, 1f)] public float proxyFollowFactor = 0.3f;

    protected override WeedWanderState CreateWanderState()
    {
        return new CCDIKWanderState(this)
        {
            reachThreshold = weedReachThreshold,
            dwellTime = dwellTime,
            maxTrackTime = maxTrackTime,
            maxRadius = proxyMaxRadius,
            followFactor = proxyFollowFactor,
        };
    }
}
