using UnityEngine;
using System.Collections.Generic;
using CreatureTypes;

class WanderState : ThinkState
{
    public float changeTargetThreshold = 5f;
    public float maxStayTime = 3f;          // 점에 못 닿아도 이 시간 지나면 강제 갱신
    public bool needToGetNewPoint = false;
    public bool hasTarget = false;
    private float lastRefreshTime;

    [Header("weed 근처 머무름 (L/A 전용, 개체마다 랜덤)")]
    public Vector2 weedLingerTimeRange = new Vector2(3f, 10f);   // 머무는 시간 (min~max 중 랜덤)
    public Vector2 weedHoverRadiusRange = new Vector2(1.5f, 4f); // 배회 반경 (작을수록 더 바짝 붙음)
    private float weedLingerStart = -1f;
    private float _lingerTime = -1f;   // 이 개체가 굴린 값 (음수=아직 안 굴림)
    private float _hoverRadius;

    public WanderState(Think2 think) : base(think) { }

    public override void Enter(ThinkTarget prev)
    {
        base.Enter(prev);
        hasTarget = false;
        lastRefreshTime = Time.time;
    }

    public override void Refresh(List<Vector3> points)
    {
        detected = think.scanner.Results;

        // L/A가 weed를 발견하면 일정 시간 그 주변을 배회하고 이주를 억제 (더 오래 머무름)
        if (IsParticle() && TryLingerNearWeed()) return;

        if (!hasTarget)
        {
            newTarget.point = GetNewPoint(points);
            hasTarget = true;
            lastRefreshTime = Time.time;
        }
        // 옆방에 Chase 대상이 있을 때 이주
        var mig = think.migration;
        if (mig != null && think.self.canMigrate
            && mig.HasChaseTargetInAdjacentRoom() && mig.TickMigration())
        {
            newTarget.point = mig.migrateTargetPoint;
            return;
        }

        // 열린 문으로 흘러가기 (A/H) — 쫓을 대상이 없어도 확률적으로 이주
        if (mig != null && think.self.canMigrate
            && mig.WantsWanderMigrate() && mig.TickMigration())
        {
            newTarget.point = mig.migrateTargetPoint;
            return;
        }

        // 도달했거나 너무 오래 머물렀으면 새 점
        float d = Vector3.Distance(think.self.rootTransform.position, newTarget.point);
        bool reached = d < changeTargetThreshold;
        bool tooLong = Time.time - lastRefreshTime > maxStayTime;
        needToGetNewPoint = reached || tooLong;

        if (needToGetNewPoint)
        {
            oldTarget = newTarget;
            newTarget.point = GetNewPoint(points);
            lastRefreshTime = Time.time;
        }
    }

    // 이 생물이 L/A 입자인가 (weed 머무름은 이들만)
    private bool IsParticle()
    {
        var id = think.self != null && think.self.data != null ? think.self.data.creatureID : CreatureID.Player;
        return id == CreatureID.L || id == CreatureID.A;
    }

    // weed가 스캔 범위에 있으면 그 주변을 배회하고 이주 억제. 머무는 시간 지나면 false(정상 배회 복귀).
    private bool TryLingerNearWeed()
    {
        // 개체 개성 1회 롤 (어떤 애는 오래 머물고, 어떤 애는 더 바짝 붙음)
        if (_lingerTime < 0f)
        {
            _lingerTime  = Random.Range(weedLingerTimeRange.x, weedLingerTimeRange.y);
            _hoverRadius = Random.Range(weedHoverRadiusRange.x, weedHoverRadiusRange.y);
        }

        Creature weed = NearestWeed();
        if (weed == null) { weedLingerStart = -1f; return false; }

        if (weedLingerStart < 0f) weedLingerStart = Time.time;       // 발견 시작
        if (Time.time - weedLingerStart >= _lingerTime) return false; // 충분히 머묾 → 정상 복귀(이주 허용)

        // weed 주변으로 목표점 (도달했거나 목표 없으면 새로)
        Vector3 self = think.self.rootTransform.position;
        if (!hasTarget || Vector3.Distance(self, newTarget.point) < changeTargetThreshold)
        {
            Vector2 r = Random.insideUnitCircle * _hoverRadius;
            newTarget.point = weed.rootTransform.position + new Vector3(r.x, 0f, r.y);
            newTarget.creature = weed;
            hasTarget = true;
            lastRefreshTime = Time.time;
        }
        return true;   // 이주 블록 스킵 → 방 안 weed 주변에 머무름
    }

    // 스캔 범위 안 가장 가까운 weed
    private Creature NearestWeed()
    {
        if (detected == null) return null;
        float r = think.scanner != null ? think.scanner.scanRadius : float.MaxValue;
        float rSqr = r * r;
        Vector3 self = think.self.rootTransform.position;
        Creature best = null; float bestSqr = float.MaxValue;
        for (int i = 0; i < detected.Count; i++)
        {
            var c = detected[i];
            if (c == null || c.IsDead || c.data == null) continue;
            var id = c.data.creatureID;
            if (id != CreatureID.Weed && id != CreatureID.WalkingWeed) continue;
            float sqr = (c.rootTransform.position - self).sqrMagnitude;
            if (sqr > rSqr) continue;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }

    public override float Score(Vector3 point)
    {
        float total = 0f;

        float dAnchor = Vector3.Distance(point, oldTarget.point);
        float minDist = 10f;
        float anchorPenalty = 2.0f;
        if (dAnchor < minDist)
            total -= (minDist - dAnchor) * anchorPenalty;

        return total;
    }
}
