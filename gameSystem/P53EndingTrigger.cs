using UnityEngine;
using UnityEngine.Events;

// 튜토리얼 방에서 p53을 락온한 채 F를 누르면 엔딩 판정.
//  조건(모두 충족): 지금까지 모은 L/A 방 합계 > totalThreshold  +  플레이어가 이 트리거와 같은 씬(튜토리얼) 방에 있음.
//  판정: 현재 로드된 전체 방 중 A방 vs L방 과반 → LastBranch 기록.
//   - A > L      → A 과반 (세포자살 종료 대사)
//   - 그 외(L ≥ A) → L 과반 (아래 공정 보러 대사)
//  발동 시: 연출 훅(aOnJudge/lOnJudge, "특정 오브젝트 아래로 내려가는" 연출용 — 플레이어가 실제로 내려가는 건 아님) →
//           TutorialGuide.BeginP53Ending()으로 EndingText 대사 재생 → 끝에 Ending_tut5(thankYou 씬).
//  락온 대상이 p53인지는 호출부(PlayerInputManager)가 판정하고, 이 컴포넌트는 total·방 게이트만 본다.
public class P53EndingTrigger : MonoBehaviour
{
    // 홈(튜토리얼) 씬에 하나. PlayerInputManager가 F 입력 시 찾아 호출.
    public static P53EndingTrigger Instance;

    // 마지막 판정 결과. EndingText가 대사 분기에 읽음. 아직 안 누르면 None.
    public static Room.RoomActivation LastBranch = Room.RoomActivation.None;

    [Header("조건")]
    [Tooltip("모은 L/A 방 합계가 이 값을 '초과'하면 F 가능. 기본 19 → total > 19")]
    public int totalThreshold = 19;
    [Tooltip("비우면 이 트리거와 같은 씬(튜토리얼)의 아무 방에서나 가능. 지정하면 그 roomID의 방에 있을 때만")]
    public string requiredRoomID = "";

    [Header("엔딩 대사 재생")]
    [Tooltip("엔딩 대사를 재생할 TutorialGuide. 비우면 자동 탐색")]
    public TutorialGuide tutorialGuide;

    [Header("연출 훅 (대사 재생 직전)")]
    [Tooltip("A 과반일 때 연출 (예: 특정 오브젝트 내려가는 애니메이션). 연출용 — 실제로 내려가는 건 아님")]
    public UnityEvent aOnJudge;
    [Tooltip("L 과반일 때 연출")]
    public UnityEvent lOnJudge;

    private bool _used;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; LastBranch = Room.RoomActivation.None; }

    private void OnEnable() { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }

    // 지금 엔딩 F가 가능한가 (UI/안내 표시용으로도 공개).
    public bool CanTrigger()
    {
        if (_used) return false;
        if (RoomManager.Instance == null) return false;
        if (RoomManager.Instance.CountActivatedTotal() <= totalThreshold) return false;

        var cur = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (cur == null) return false;
        if (cur.gameObject.scene != gameObject.scene) return false;          // 같은(튜토리얼) 씬의 방인지
        if (!string.IsNullOrEmpty(requiredRoomID) && cur.roomID != requiredRoomID) return false;
        return true;
    }

    // PlayerInputManager가 F(락온 대상=p53) 때 호출. 엔딩을 발동했으면 true(→ 빙의로 안 넘어감), 아니면 false.
    public bool TryTriggerEnding()
    {
        if (!CanTrigger()) return false;
        _used = true;

        int a = RoomManager.Instance.CountRooms(Room.RoomActivation.A);
        int l = RoomManager.Instance.CountRooms(Room.RoomActivation.L);
        bool isA = a > l;                       // A가 과반일 때만 A분기, 동점은 L분기
        LastBranch = isA ? Room.RoomActivation.A : Room.RoomActivation.L;
        Debug.Log($"[P53Ending] 판정 A={a} L={l} → {LastBranch} 분기");

        (isA ? aOnJudge : lOnJudge)?.Invoke();   // 연출(내려가는 애니 등)

        var guide = tutorialGuide != null ? tutorialGuide : FindObjectOfType<TutorialGuide>();
        if (guide != null)
            guide.BeginP53Ending();              // EndingText 대사 → Ending_tut5(thankYou)
        else
            Debug.LogError("[P53Ending] TutorialGuide를 찾지 못해 엔딩 대사를 재생할 수 없음.", this);
        return true;
    }
}
