using System.Collections.Generic;
using UnityEngine;

// 동반자(p53)용: 스캐너·관계 무시하고 항상 '플레이어'를 따라감.
// 너무 붙지 않게 followDistance 링 위의 점을 proxy 목표로 준다 (D의 chase와 같은 원리, 대상만 플레이어 고정).
public class FollowPlayerState : ThinkState
{
    public float followDistance = 3f;   // 플레이어로부터 유지할 거리

    public FollowPlayerState(Think2 think) : base(think) { }

    public override void Refresh(List<Vector3> points)
    {
        // 안내(guide) 모드: 지정 위치로 이동 (플레이어 추종 대신)
        var p53 = think as P53Think;
        if (p53 != null && p53.guideTarget != null)
        {
            newTarget.point = p53.guideTarget.position;
            newTarget.creature = null;
            return;
        }

        var player = Player.Instance;
        if (player == null) return;

        Vector3 pp = player.transform.position;
        Vector3 self = GetSelfPos();
        Vector3 off = self - pp;
        float m = off.magnitude;

        // 플레이어로부터 followDistance 지점을 목표로 → 그 거리에서 멈춤
        newTarget.point = m > 0.01f ? pp + off / m * followDistance : pp;
        newTarget.creature = player.pc;   // HUD 표시용(없으면 null)
    }
}
