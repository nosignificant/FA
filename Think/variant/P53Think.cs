using UnityEngine;
using CreatureTypes;

// p53(NPC 동반자): 생태계 AI 대신 항상 플레이어를 따라다님. (D와 같은 Think2/TargetControl 구조 재활용)
// 방 비활성·휴면 게이트를 무시해 어느 방에서든 따라옴.
public class P53Think : Think2
{
    [Header("P53 follow")]
    [Tooltip("플레이어로부터 유지할 거리(m). 이보다 가까우면 더 안 붙음")]
    public float followDistance = 3f;

    [Header("안내(guide) 모드")]
    [Tooltip("지정하면 플레이어를 따라가지 않고 이 위치로 이동 + 이쪽으로 상시 링크 표시. 비우면 플레이어 추종")]
    public Transform guideTarget;

    // 튜토리얼 등 외부에서 안내 위치 지정/해제 (대사 뒤 호출)
    public void SetGuide(Transform t) => guideTarget = t;
    public void ClearGuide() => guideTarget = null;

    // 안내 중이면 그쪽으로 상시 링크
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

    // 의도·상태 무관하게 항상 '플레이어 따라가기'
    protected override CreatureIntent DetermineIntent() => CreatureIntent.Wander;
    protected override ThinkState GetThinkState(CreatureIntent intent) => followState;
}
