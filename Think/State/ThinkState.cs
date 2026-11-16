using CreatureTypes;
using UnityEngine;
using System.Collections.Generic;

//state 내에서는 상태 안에서 할 행동(대상 변경 등)에 대해서만 서술
//다른 상태로 변하는 조건은 적지 않음

public abstract class ThinkState
{
    protected Think2 think;
    protected IReadOnlyList<Creature> detected;

    public ThinkTarget newTarget;
    public ThinkTarget oldTarget;

    public ThinkState(Think2 think) { this.think = think; }

    //state base//

    public virtual void Enter(ThinkTarget prevTarget)
    {
        oldTarget = prevTarget;
        newTarget = default;
        think.currentLockTime = 0f;
        think.LockState(false);

    }
    public virtual void Refresh(List<Vector3> points) { }

    public virtual ThinkTarget Exit()
    {
        return newTarget;
    }

    //point think//
    public Vector3 GetNewPoint(List<Vector3> points)
    {
        Vector3 selfPos = GetSelfPos();
        if (points == null || points.Count == 0) return selfPos;

        Vector3 best = points[0];   // fallback: 첫 점
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < points.Count; i++)
        {
            float s = Score(points[i]);
            if (s > bestScore) { bestScore = s; best = points[i]; }
        }
        return best;
    }

    public virtual float Score(Vector3 point) => 0f;

    void StateChanged(Think2 think)
    {
        oldTarget = newTarget;
    }

    // 이 거리(m) 너머의 생물은 Score 기여(distTerm=1/(d+1)^1.5)가 거의 0이라
    //  비싼 IsBlocked(벽 체크 raycast)를 생략한다. 넉넉히 크게 잡아 AI 동작은 사실상 불변.
    protected const float MaxInfluenceDistance = 28f;

    protected bool IsBlocked(Vector3 from, Vector3 to)
    {
        Vector3 a = from + Vector3.up;
        Vector3 b = to + Vector3.up;
        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return false;
        return Physics.Raycast(a, dir / dist, dist, think.obstacleMask);
    }

    protected Vector3 GetSelfPos() => think.self.rootTransform.position;
    protected Vector3 GetTargetPos(Creature t) => t.rootTransform.position;
}






