using UnityEngine;
using CreatureTypes;

// p53(NPC 동반자): 생태계 AI 대신, 인스펙터로 지정한 타겟 위치로만 이동. (D와 같은 Think2/TargetControl 구조 재활용)
// 방 비활성·휴면 게이트를 무시해 어느 방에서든 움직임. 타겟 미지정이면 제자리.
public class P53Think : Think2
{
    [Header("P53 이동 타겟")]
    [Tooltip("평소 따라다니는 물체(예: p53following). 이걸 따라갈 땐 링크 선을 숨김")]
    public Transform followTarget;
    [Tooltip("안내 지점. 지정하면 followTarget 대신 이쪽으로 이동 + 링크 선 표시. 비우면 followTarget 추종")]
    public Transform guideTarget;
    [Tooltip("타겟으로부터 유지할 거리(m). 이보다 가까우면 더 안 붙음. 0이면 타겟점에 정확히")]
    public float followDistance = 3f;
    [Tooltip("타겟과 이 거리 이상 벌어지면(벽·단절로 못 감) 타겟 옆으로 순간이동. 0이면 끔")]
    public float warpDistance = 20f;

    // 실제 이동 목표: 안내 지점(guideTarget) 우선, 없으면 평소 추종 물체(followTarget)
    public Transform MoveTarget => guideTarget != null ? guideTarget : followTarget;

    // 튜토리얼 등 외부에서 안내 지점 지정/해제 (대사 뒤 호출)
    public void SetGuide(Transform t) => guideTarget = t;
    public void ClearGuide() => guideTarget = null;

    // 링크 선은 '안내 지점(guideTarget)'일 때만. followTarget(p53following) 추종 중엔 null → 선 숨김
    public override Transform LinkOverrideTarget => guideTarget;

    private FollowPlayerState followState;

    protected override void Awake()
    {
        base.Awake();
        if (self != null)
        {
            self.canMigrate = true;     // 플레이어 따라 방 넘나듦
            self.alwaysActive = true;   // 방 비활성·연결 무관하게 항상 따라다님 (안 얼고, 통로 무관 방 소속 갱신)
        }
        followState = new FollowPlayerState(this) { followDistance = followDistance };
    }

    // 비활성 방에서도 계속 따라옴 / 휴면 안 됨
    protected override bool IgnoreRoomActiveGate => true;
    public override bool IsDormant() => false;

    // 의도·상태 무관하게 항상 '지정 타겟으로 이동'
    protected override CreatureIntent DetermineIntent() => CreatureIntent.Wander;
    protected override ThinkState GetThinkState(CreatureIntent intent) => followState;

    // 벽·단절로 못 따라올 때: 전체(루트+자식 물리바디)를 pos로 순간이동하고 속도 리셋.
    public void WarpTo(Vector3 pos)
    {
        if (self == null || self.rootTransform == null) return;

        Vector3 delta = pos - self.rootTransform.position;
        self.transform.position += delta;   // 생물 통째로 이동 → rootTransform이 pos에 옴

        foreach (var rb in self.GetComponentsInChildren<Rigidbody>())
        {
            rb.position = rb.transform.position;   // 물리 위치를 새 transform 위치로 동기화
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (ProxyTarget != null) ProxyTarget.position = pos;   // 즉시 되돌아가려는 것 방지
    }
}
