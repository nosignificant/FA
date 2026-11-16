using System.Collections;
using UnityEngine;

// 튜토리얼 방에서 p53을 락온한 채 F를 누르면 엔딩. (2단계 확정: 1차 F=확정 안내 → 2차 F=확정)
//  조건(모두 충족): 지금까지 모은 L/A 방 합계 > totalThreshold  +  플레이어가 이 트리거와 같은 씬(튜토리얼) 방에 있음.
//  판정: 현재 로드된 전체 방 중 A방 vs L방 과반 → LastBranch 기록.
//   - A > L      → A 과반 (세포자살 종료 대사)
//   - 그 외(L ≥ A) → L 과반 (아래 공정 보러 대사)
//  발동 시: descendTarget(지정 오브젝트)을 아래로 내리는 연출(연출용 — 플레이어가 실제로 내려가는 건 아님) →
//           TutorialGuide.BeginP53Ending()으로 EndingText 대사 재생 → EndingMonologue2 끝에서 thankYou 씬 로드.
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

    [Header("UI")]
    [Tooltip("확정 가능할 때(튜토리얼 씬 + 방 합계 > 임계치) 켜지는 안내 UI. 1차 F로 확정 대기에 들어가면 꺼짐")]
    public GameObject canTriggerUI;

    [Header("엔딩 대사 재생")]
    [Tooltip("엔딩 대사를 재생할 TutorialGuide. 비우면 자동 탐색")]
    public TutorialGuide tutorialGuide;

    [Header("판정 연출 — 아래로 내려갈 오브젝트 (A/L 공통)")]
    [Tooltip("판정 시 아래로 내려갈 오브젝트. 비우면 연출 없음. 연출용 — 플레이어가 실제로 내려가는 건 아님")]
    public Transform descendTarget;
    [Tooltip("내려갈 거리(m)")]
    public float descendDistance = 5f;
    [Tooltip("내려가는 시간(초)")]
    public float descendDuration = 2f;

    private bool _used;            // 확정(2차 F) 완료
    private bool _awaitingConfirm; // 1차 F로 확정 안내를 띄우고 2차 F를 기다리는 중

    // --- 임시 테스트용 강제 카운트 (EndingTestHarness가 설정; null이면 실제 RoomManager 사용) ---
    [System.NonSerialized] public int? testA;
    [System.NonSerialized] public int? testL;
    private bool TestForced => testA.HasValue || testL.HasValue;
    private int CountA() => testA ?? RoomManager.Instance.CountRooms(Room.RoomActivation.A);
    private int CountL() => testL ?? RoomManager.Instance.CountRooms(Room.RoomActivation.L);
    private int TotalActivated() => TestForced ? (testA ?? 0) + (testL ?? 0)
                                               : RoomManager.Instance.CountActivatedTotal();

    // 확정 대기 중인가 (이 동안엔 Tab 락온 토글/순환을 막아 p53 락온을 유지시킨다).
    public bool AwaitingConfirm => _awaitingConfirm;

    // ESC 등으로 p53 락온을 풀면 확정 단계를 처음으로 되돌린다 (다음에 다시 1차 F부터).
    public void CancelConfirm()
    {
        if (!_awaitingConfirm) return;
        _awaitingConfirm = false;
        // canTriggerUI는 Update에서 조건 충족 시 자동으로 다시 켜짐.
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; LastBranch = Room.RoomActivation.None; }

    private void OnEnable() { if (Instance == null) Instance = this; }
    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (canTriggerUI != null) canTriggerUI.SetActive(false);
    }

    // 확정 가능할 때만 안내 UI 표시. (확정 대기/확정 후엔 끔 — 안내는 엔딩 대사가 대신)
    private void Update()
    {
        if (canTriggerUI == null) return;
        bool show = !_awaitingConfirm && CanTrigger();
        if (canTriggerUI.activeSelf != show) canTriggerUI.SetActive(show);
    }

    // 지금 엔딩 F가 가능한가 (UI/안내 표시용으로도 공개).
    public bool CanTrigger()
    {
        if (_used) return false;
        if (RoomManager.Instance == null) return false;
        if (TotalActivated() <= totalThreshold) return false;

        var cur = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (cur == null) return false;
        if (cur.gameObject.scene != gameObject.scene) return false;          // 같은(튜토리얼) 씬의 방인지
        if (!string.IsNullOrEmpty(requiredRoomID) && cur.roomID != requiredRoomID) return false;
        return true;
    }

    // PlayerInputManager가 F(락온 대상=p53) 때 호출. 엔딩 흐름을 소비했으면 true(→ 빙의로 안 넘어감), 아니면 false.
    //  1차 F(조건 충족): 확정 안내만 띄우고 대기(true로 삼켜 빙의 방지).
    //  2차 F: 확정 → 판정·연출·엔딩 대사. 이후 F는 계속 true로 삼킴.
    public bool TryTriggerEnding()
    {
        if (_used) return true;   // 이미 확정됨 — F를 삼켜 빙의 방지

        var guide = tutorialGuide != null ? tutorialGuide : FindObjectOfType<TutorialGuide>();

        if (!_awaitingConfirm)
        {
            if (!CanTrigger()) return false;   // 조건 미충족 → 평소대로 빙의 처리
            _awaitingConfirm = true;           // 1차 F: 확정 대기 진입
            if (guide != null) guide.BeginP53Confirm();   // "확정하시겠습니까" + F 한 번 더 안내
            else Debug.LogError("[P53Ending] TutorialGuide를 찾지 못해 확정 안내를 재생할 수 없음.", this);
            return true;
        }

        // 2차 F: 확정
        _used = true;
        _awaitingConfirm = false;

        int a = CountA();
        int l = CountL();
        bool isA = a > l;                       // A가 과반일 때만 A분기, 동점은 L분기
        LastBranch = isA ? Room.RoomActivation.A : Room.RoomActivation.L;
        Debug.Log($"[P53Ending] 판정 A={a} L={l} → {LastBranch} 분기");

        if (descendTarget != null) StartCoroutine(DescendRoutine());   // 지정 오브젝트 내려가는 연출 (A/L 공통)

        if (guide != null)
            guide.BeginP53Ending();              // EndingText 대사(EndingMonologue2) → thankYou 씬
        else
            Debug.LogError("[P53Ending] TutorialGuide를 찾지 못해 엔딩 대사를 재생할 수 없음.", this);
        return true;
    }

    // descendTarget을 현재 위치에서 descendDistance만큼 아래로 descendDuration 동안 내린다 (연출용).
    private IEnumerator DescendRoutine()
    {
        Vector3 start = descendTarget.position;
        Vector3 end = start + Vector3.down * descendDistance;
        float t = 0f;
        while (t < descendDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / descendDuration);   // 부드럽게 가감속
            descendTarget.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        descendTarget.position = end;
    }
}
