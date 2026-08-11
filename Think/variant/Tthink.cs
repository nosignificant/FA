using System.Collections.Generic;
using UnityEngine;

// 발신기(SignalTransmitter) 전용 think. WeedThink 기반 — 몸은 고정, proxy(텐타클)만 타겟으로 뻗음.
// 타겟 우선순위:
//   연결됨   → 수신기        (텐타클이 수신기로 뻗음 = 연결선)
//   조종 중  → 플레이어/proxy (manualControl이 Think를 건너뛰고 CreaturePossess가 proxy를 몰아서 자동)
//   그 외    → WeedThink 기본 (주변 생물 반응 / 배회 — 살아있는 느낌)
public class Tthink : WeedThink
{
    protected override WeedWanderState CreateWanderState()
    {
        return new TransmitterWeedState(this)
        {
            reachThreshold = weedReachThreshold,
            dwellTime = dwellTime,
        };
    }
}

// 연결돼 있으면 수신기를 타겟, 아니면 weed 기본 동작
public class TransmitterWeedState : WeedWanderState
{
    public TransmitterWeedState(Think2 think) : base(think) { }

    public override void Refresh(List<Vector3> points)
    {
        var tx = think.self as SignalTransmitter;
        if (tx != null && tx.IsConnected && tx.ConnectedReceiverTransform != null)
        {
            newTarget.point = tx.ConnectedReceiverTransform.position;   // 수신기로 텐타클 뻗기
            newTarget.creature = tx.ConnectedReceiver;                  // HUD에 연결된 수신기 표시
            return;
        }

        // 미연결 → 중립(제자리): 주변 생물 안 쫓고 자기 몸으로 텐타클 거둠 (#2)
        var self = think.self;
        newTarget.point = self.rootTransform != null ? self.rootTransform.position : self.transform.position;
        newTarget.creature = null;
    }
}
