using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CreatureTypes;
using System.Linq;


public class AAThink : TentacleThink
{
    private float grabbedSince = -1f;

    [Header("추격 포기 (도달 실패)")]
    [Tooltip("이 시간(초) 동안 쫓았는데 giveUpDist 안으로 못 좁히면 포기")]
    public float giveUpTime = 6f;
    [Tooltip("이 거리 안에 들어가면 '도달'로 간주(포기 타이머 리셋). 계속 못 들어가면 실패로 포기")]
    public float giveUpDist = 3f;
    [Tooltip("포기한 뒤 그 대상을 다시 쫓지 않는 시간(초)")]
    public float ignoreCooldown = 5f;
    private Creature _chaseTarget;   // 현재 추격 중인 대상(타이머 기준)
    private float _chaseStart;       // 이 대상을 쫓기 시작한 시각
    private Creature _ignored;       // 방금 포기한 대상
    private float _ignoreUntil = -1f;

    // H가 잡은 AA는 isBound로 알아서 멈추므로, 방에 H가 있어도 나머지 AA는 계속 L을 쫓아 변환.
    // (예전의 "방에 H 있으면 L 제외" 규칙은 제거 — 방 전체 AA가 멈춰버리는 부작용)
    public override bool IsValidTarget(Creature target)
    {
        // 방금 포기한 대상은 쿨다운 동안 쫓지 않음 (DoesNeedToChase/BestChaseTarget 둘 다 여기 통과)
        if (target != null && target == _ignored && Time.time < _ignoreUntil) return false;
        return base.IsValidTarget(target);
    }

    private int GrabbedCount()
    {
        if (tentacleGrab == null || tentacleGrab.tentacles == null) return 0;
        int n = 0;
        for (int i = 0; i < tentacleGrab.tentacles.Length; i++)
            if (tentacleGrab.tentacles[i].isGrabbing && tentacleGrab.tentacles[i].grabbedCreature != null)
                n++;
        return n;
    }

    protected override CreatureIntent DetermineIntent()
    {
        if (DoesNeedToFlee()) return CreatureIntent.Flee;

        // L을 잡으면 aaSingleGrabTimeout 동안 들고 대기 → 그 뒤 A로 변환 (1:1).
        if (GrabbedCount() >= 1)
        {
            if (grabbedSince < 0f) grabbedSince = Time.time;
            float hold = (self as AACreature)?.aaSingleGrabTimeout ?? 3f;
            if (Time.time - grabbedSince >= hold) return CreatureIntent.Synthesizing;
            return CreatureIntent.Wander;   // 잡은 채 대기 (새로 안 쫓음 → 2개 잡아 손실나는 것 방지)
        }

        grabbedSince = -1f;

        // 추격 포기(도달 실패): 같은 대상을 giveUpTime 동안 쫓았는데 giveUpDist 안으로 못 좁히면 포기
        if (self.intent == CreatureIntent.Chase && currentTarget.creature != null)
        {
            Creature tgt = currentTarget.creature;
            if (tgt != _chaseTarget) { _chaseTarget = tgt; _chaseStart = Time.time; }   // 새 대상 → 타이머 시작

            float dist = Vector3.Distance(self.rootTransform.position, tgt.rootTransform.position);
            if (dist <= giveUpDist) _chaseStart = Time.time;                 // 도달 성공 → 타이머 리셋(계속 시도)
            else if (Time.time - _chaseStart >= giveUpTime)                  // 제한시간 내 도달 실패 → 포기
            {
                _ignored = tgt;
                _ignoreUntil = Time.time + ignoreCooldown;
                _chaseTarget = null;
                LockState(false);
                return CreatureIntent.Wander;
            }
        }
        else _chaseTarget = null;   // 추격 상태 아님 → 타이머 리셋

        return DoesNeedToChase() ? CreatureIntent.Chase : CreatureIntent.Wander;
    }
    protected override bool DoesNeedToFlee()
    {
        if (detected == null) return false;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (IsCreatureDormant(t)) continue;   // 휴면 H로부턴 안 도망
            if (self.HasAction(t.data.creatureID, InteractionAction.Flee)) return true;
        }
        return false;
    }
    protected override bool DoesNeedToChase()
    {
        if (detected == null) return false;
        for (int i = 0; i < detected.Count; i++)
        {
            var t = detected[i];
            if (!IsValidTarget(t)) continue;
            if (t.IsGrabbed) continue;                        // 이미 잡힌 L은 쫓지 않음 (Chase인데 타겟 null 방지)
            if (t.currentRoom != self.currentRoom) continue;  // 다른 방 L도 제외 (currentTarget 비어 HUD 공백 방지)
            if (self.HasAction(t.data.creatureID, InteractionAction.Chase)) return true;
        }
        return false;
    }
}