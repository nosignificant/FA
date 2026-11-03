using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using CreatureTypes;

public class Door : MonoBehaviour
{
    [Header("References")]
    public Room roomA;
    public Room roomB;
    public Creature self;
    public GameObject light;

    [Header("Door")]
    public Collider playerBlockCollider;
    [Tooltip("열릴 때 좌우로 이동하는 거리")]
    public float moveDist = 10f;

    [Header("양옆 두 짝 (좌우로 열림)")]
    [FormerlySerializedAs("upperDoor")] public Transform rightDoor;
    [FormerlySerializedAs("lowerDoor")] public Transform leftDoor;

    public enum ConditionMode
    {
        RoomState,   // 양쪽 방 중 하나가 requiredState(L/A)로 활성화되면 열림
        LevelCount,  // 레벨 전체에서 requiredState로 활성화된 방이 levelCountN개 이상이면 열림
        LevelTotal,  // 레벨 전체에서 L+A(활성화된 방 전체) 합이 levelCountN개 이상이면 열림
        ReceiverState, // R(receiver)이 있는 방이 requiredState(L/A)가 되는 즉시 열림. R의 방 상태를 그대로 따라감(방송 없음)
    }

    [Header("Condition")]
    public ConditionMode conditionMode = ConditionMode.RoomState;

    [Tooltip("RoomState/LevelCount/ReceiverState 모드가 요구하는 방 활성화 상태 (L/A)")]
    public Room.RoomActivation requiredState = Room.RoomActivation.A;

    [Tooltip("ReceiverState 모드: 이 문이 따라가는 R(receiver). 이 R이 있는 방이 requiredState가 되면 열림. 비우면 씬에서 자동 탐색")]
    public SignCreature receiver;

    [Tooltip("LevelCount 모드: requiredState로 활성화된 방이 레벨 안에 이 개수 이상이면 열림")]
    public int levelCountN = 3;

    [Header("복합 조건 (useComposite 켜면 conditionMode 대신, 아래 켠 조건을 모두 AND)")]
    [Tooltip("켜면 위 conditionMode 대신 아래 bool 조건들을 모두 만족해야 열림. 끄면 기존 enum 그대로")]
    public bool useComposite = false;

    [Tooltip("조건: 이 문의 방(roomA/roomB 중 하나)이 roomStateValue 상태여야 함")]
    public bool needRoomState = false;
    public Room.RoomActivation roomStateValue = Room.RoomActivation.A;

    [Tooltip("조건: 레벨 전체에서 countState인 방이 levelCountN개 이상")]
    public bool needLevelCount = false;
    [Tooltip("needLevelCount가 세는 상태(기본 L). 아래 levelCountN과 함께 쓰임")]
    public Room.RoomActivation countState = Room.RoomActivation.L;

    [Tooltip("조건: R(receiver)이 있는 방이 receiverStateValue 상태여야 함 (위 receiver 필드 사용, 비우면 자동 탐색)")]
    public bool needReceiverState = false;
    public Room.RoomActivation receiverStateValue = Room.RoomActivation.A;

    [Header("HUD 라벨 직접 작성 (선택)")]
    [Tooltip("켜면 아래 3칸(이름/동사/목적어)을 모드 자동 라벨 대신 그대로 HUD에 표시")]
    public bool useCustomLabel = false;
    public string customName = "";    // name 칸 (조건1)
    public string customVerb = "";    // status 칸 (연산/동사)
    public string customObject = "";  // target 칸 (조건2)

    [Header("Condition Graphics (선택)")]
    [Tooltip("LevelCount 전용: '조건(요구 개수 levelCountN)' 표시 부모들. 각 부모 자식을 앞에서부터 levelCountN개 켜고 나머지 끔(정적). 문 양면이면 2개. 비우면 안 씀.")]
    public GameObject[] levelCountReqParents;
    [Tooltip("LevelCount 전용: '현재 L/A인 방 개수' 표시 부모들. 각 부모 자식을 현재 카운트만큼 켜고 나머지 끔(실시간). 문 양면이면 2개. 비우면 안 씀.")]
    public GameObject[] levelCountCurParents;

    [Tooltip("ReceiverState('R is -') 조건일 때 켤 스프라이트/그래픽들. 문 양면이면 2개. 비우면 안 씀.")]
    public GameObject[] receiverStateGraphics;

    [Header("RoomState 그래픽 (requiredState별 다른 스프라이트)")]
    [Tooltip("requiredState=L 일 때 켤 그래픽들. 문 양면이면 2개.")]
    public GameObject[] roomStateGraphicsL;
    [Tooltip("requiredState=A 일 때 켤 그래픽들. 문 양면이면 2개.")]
    public GameObject[] roomStateGraphicsA;

    [Header("RoomState 그래픽 색 틴트")]
    [Tooltip("켜면 L/A 그래픽을 각각 지정한 색으로 칠함")]
    public bool tintRoomStateGraphic = true;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color, SpriteRenderer는 .color)")]
    public string roomStateColorProperty = "";
    public Color lColor = new Color(0.6f, 1f, 0.3f);   // L 그래픽(스프라이트) 색
    public Color aColor = new Color(0.9f, 0.2f, 0.2f); // A 그래픽(스프라이트) 색

    [Tooltip("LevelCount 재평가 주기(초). 다른 방/문 변화에 반응해야 해서 주기적으로 확인")]
    public float checkInterval = 0.25f;

    [Header("State")]
    public bool isOpen = false;
    [Tooltip("항상 열린 통로. 조건·다른 문과 무관하게 계속 열려 있음 (isOpen 런타임 상태와 별개)")]
    public bool alwaysOpen = false;
    [Tooltip("처음 열린 뒤 조건이 바뀌어도 계속 열린 상태 유지")]
    public bool stayOpenAfterFirstOpen = false;
    private Vector3 originalRightPos;
    private Vector3 originalLeftPos;
    private Coroutine moveCo;

    // 조건 종류(conditionMode)에 맞는 그래픽만 켜기. 슬롯 비면 무시.
    // RoomState/ReceiverState/LevelCount면 requiredState(L/A)에 맞는 RoomState Graphic만 켜고 나머지는 끔.
    public void ApplyConditionGraphic()
    {
        bool isLevelCount  = !useComposite && conditionMode == ConditionMode.LevelCount;
        // 단일 방 상태 스프라이트(L/A)를 쓰는가: 복합이면 needRoomState, 아니면 RoomState/ReceiverState 모드
        bool roomStatePart = useComposite ? needRoomState
            : (conditionMode == ConditionMode.RoomState || conditionMode == ConditionMode.ReceiverState);
        bool stateMode = roomStatePart || isLevelCount;   // L/A 스프라이트·색 쓰는 모드

        // ReceiverState('R is -') 전용 그래픽: 이 모드일 때만 켬
        bool isReceiverState = !useComposite && conditionMode == ConditionMode.ReceiverState;
        SetActiveAll(receiverStateGraphics, isReceiverState);

        // 표시 상태: 복합=roomStateValue / RoomState·ReceiverState·LevelCount=requiredState
        Room.RoomActivation dispState = useComposite ? roomStateValue : requiredState;

        bool wantL = stateMode && dispState == Room.RoomActivation.L;
        bool wantA = stateMode && dispState == Room.RoomActivation.A;
        SetActiveAll(roomStateGraphicsL, wantL);
        SetActiveAll(roomStateGraphicsA, wantA);

        // 켜진 그래픽(스프라이트)을 각 상태색으로 틴트
        if (tintRoomStateGraphic)
        {
            if (wantL) TintAll(roomStateGraphicsL, lColor, roomStateColorProperty);
            if (wantA) TintAll(roomStateGraphicsA, aColor, roomStateColorProperty);

            // ReceiverState 전용 그래픽 / LevelCount 개수 자식도 같은 상태색(dispState)으로 틴트
            if (stateMode && dispState != Room.RoomActivation.None)
            {
                Color stateCol = dispState == Room.RoomActivation.A ? aColor : lColor;
                if (isReceiverState) TintAll(receiverStateGraphics, stateCol, roomStateColorProperty);
                if (isLevelCount)
                {
                    TintAll(levelCountReqParents, stateCol, roomStateColorProperty);
                    TintAll(levelCountCurParents, stateCol, roomStateColorProperty);
                }
            }
        }

        // 개수 게이지: LevelTotal은 요청에 따라 요구치·현재 둘 다 안 띄움. 그 외 개수 모드만 표시.
        bool showGauge = ShowCurrentCountGauge();
        UpdateLevelCountParents(levelCountReqParents, showGauge ? levelCountN : 0);
        UpdateLevelCountParents(levelCountCurParents, showGauge ? CurrentModeCount() : 0);
    }

    // 배열 전체 SetActive
    private void SetActiveAll(GameObject[] arr, bool on)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) arr[i].SetActive(on);
    }

    // 배열 전체를 같은 색으로 틴트 (각각 자식 포함)
    private void TintAll(GameObject[] arr, Color col, string prop)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) TintGraphic(arr[i], col, prop);
    }

    // 자식 포함 전부 칠하기 (스프라이트 그래픽용)
    private void TintGraphic(GameObject go, Color col, string prop)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        ActivationColor.Apply(rs, col, prop);
    }

    public Room GetOtherRoom(Room from)
    {
        if (from == roomA) return roomB;
        if (from == roomB) return roomA;
        return null;
    }

    void Awake()
    {
        // roomA가 비어있으면 부모에서 자동 할당
        if (roomA == null) roomA = GetComponentInParent<Room>();
        if (self == null) self = GetComponent<Creature>();

        if (rightDoor != null) originalRightPos = rightDoor.position;
        if (leftDoor != null) originalLeftPos = leftDoor.position;
    }

    void Start()
    {
        if (roomA == null) Debug.LogWarning($"[Door {name}] roomA 미설정");
        if (roomB == null) Debug.LogWarning($"[Door {name}] roomB 미설정 (인스펙터/RoomEditor에서 할당 필요)");

        if (roomA != null) roomA.RegisterCreature(self);
        if (roomB != null && roomB != roomA) roomB.RegisterCreature(self);

        roomA?.RegisterDoor(this);
        roomB?.RegisterDoor(this);

        ApplyConditionGraphic();   // 조건 종류에 맞는 그래픽만 켜기

        // 항상 열린 통로는 '문'처럼 취급 안 함 → 락온 불가, 불도 안 켬
        if (alwaysOpen && self != null) self.lockable = false;

        // 조명을 현재 isOpen 상태에 맞춰 초기화 (alwaysOpen이면 불 안 켬)
        if (light != null) light.SetActive(isOpen && !alwaysOpen);

        // isOpen을 켜둔 채 시작하거나 alwaysOpen이면 열린 상태로 시작
        if (isOpen || alwaysOpen) DoorCloseAndOpen(true);
    }

    private void OnEnable()
    {
        if (roomA == null) roomA = GetComponentInParent<Room>();
        // 생물 수가 바뀔 때마다 문 조건 재평가 (분해가 아니라 '방에 가장 많은 종' 기준)
        if (roomA != null) roomA.OnCreatureCountChanged += Reevaluate;
        if (roomB != null) roomB.OnCreatureCountChanged += Reevaluate;

        DoorManager.Instance.Register(this);

        EvaluateConditions();

        // LevelCount는 roomA/roomB 이벤트만으로 부족(다른 방/문 변화에 반응해야 함) → 주기 확인.
        // ReceiverState는 R의 '다른 방' 상태를 따라가므로 roomA/roomB 이벤트로 못 잡음 → 주기 확인.
        // RoomState는 양쪽 방의 OnCreatureCountChanged(활성화 변경 포함)로 충분.
        if (conditionMode == ConditionMode.LevelCount
            || conditionMode == ConditionMode.LevelTotal
            || conditionMode == ConditionMode.ReceiverState
            || (useComposite && (needLevelCount || needReceiverState)))
            StartCoroutine(TickEvaluate());
    }

    private System.Collections.IEnumerator TickEvaluate()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            EvaluateConditions();
            yield return wait;
        }
    }

    private void OnDisable()
    {
        if (roomA != null) roomA.OnCreatureCountChanged -= Reevaluate;
        if (roomB != null) roomB.OnCreatureCountChanged -= Reevaluate;

        if (DoorManager.Existing != null) DoorManager.Existing.Unregister(this);   // 정리 중 재생성 방지
    }

    private void Reevaluate() => EvaluateConditions();

    private void EvaluateConditions()
    {
        // '현재 L/A 방 개수' 게이지는 항상 갱신. 표시 모드 아니면(또는 LevelTotal) 0칸(전부 끔).
        UpdateLevelCountParents(levelCountCurParents, ShowCurrentCountGauge() ? CurrentModeCount() : 0);

        if (alwaysOpen) return;   // 항상 열린 통로는 조건과 무관하게 계속 열림
        // 튜토리얼 방(어느 쪽이든)의 문은 조건 자동 개폐 안 함 (TutorialGuide가 수동 제어)
        if ((roomA != null && roomA.isTutorial) || (roomB != null && roomB.isTutorial)) return;

        // 복합 조건: 켠 bool들을 모두 AND. 하나도 안 켜면 항상 열림(빈 AND = true).
        if (useComposite)
        {
            bool ok = true;
            if (needRoomState)
                ok &= (roomA != null && roomA.Activation == roomStateValue)
                   || (roomB != null && roomB.Activation == roomStateValue);
            if (needLevelCount)
                ok &= CountActiveRooms(countState) >= levelCountN;
            if (needReceiverState)
                ok &= ResolveReceiver() != null && receiver.CurrentSignalState == receiverStateValue;
            if (ok != isOpen) DoorCloseAndOpen(ok);
            return;
        }

        bool shouldOpen;

        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
                shouldOpen = CountActiveRooms(requiredState) >= levelCountN;
                break;

            case ConditionMode.LevelTotal:
                shouldOpen = CountActiveTotal() >= levelCountN;
                break;

            case ConditionMode.ReceiverState: // R이 있는 방이 requiredState(L/A)면 열림 — 방 상태를 그대로 따라감
                shouldOpen = ResolveReceiver() != null
                          && receiver.CurrentSignalState == requiredState;
                break;

            default: // RoomState — 양쪽 방 중 하나가 requiredState로 활성화되면 열림
                bool aMatch = roomA != null && roomA.Activation == requiredState;
                bool bMatch = roomB != null && roomB.Activation == requiredState;
                shouldOpen = aMatch || bMatch;
                break;
        }

        if (shouldOpen != isOpen) DoorCloseAndOpen(shouldOpen);   // 상태 바뀔 때만
    }

    // 문 HUD: name=조건1 / status=연산 / target=조건2 로 좌→우로 읽힘
    //   RoomState:  open / at      / A
    //   LevelCount: A    / rooms ≥ / 3
    //   복합(useComposite): open / at / A and L≥7 and R=A (켠 조건들을 " and "로 연결)

    // 복합 조건: 켠 bool들을 " and "로 이어 3번째 칸에 표시
    private string CompositeTarget()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (needRoomState) parts.Add(roomStateValue.ToString());
        if (needLevelCount) parts.Add(countState + "≥" + levelCountN);
        if (needReceiverState) parts.Add("R=" + receiverStateValue);
        return parts.Count == 0 ? "-" : string.Join(" and ", parts);
    }

    // name 칸 (조건1)
    public string Operand1Label()
    {
        if (useCustomLabel) return customName;
        if (useComposite) return "open";
        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
                return requiredState.ToString();
            case ConditionMode.LevelTotal:
                return "total";   // "total / rooms ≥ / n"
            case ConditionMode.ReceiverState:
                return "R";   // "R / is / A"
            default: // RoomState (사용자 요청: 방이름 대신 "open" → "open / at / A"로 읽힘)
                return "open";
        }
    }

    // status 칸 (연산/동사)
    public string ConditionLabel()
    {
        if (useCustomLabel) return customVerb;
        if (useComposite) return "at";
        switch (conditionMode)
        {
            case ConditionMode.LevelCount: return "rooms ≥";   // "rooms ≥"
            case ConditionMode.LevelTotal: return "rooms ≥";   // "total rooms ≥ n"
            case ConditionMode.ReceiverState: return "is";   // "R is 상태"
            default: return "at";   // RoomState (사용자 요청: "open / at / A" 형식, 방이름 안 씀)
        }
    }

    // target 칸 (조건2)
    public string GateTargetLabel()
    {
        if (useCustomLabel) return customObject;
        if (useComposite) return CompositeTarget();   // "A and L≥7"
        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
            case ConditionMode.LevelTotal:
                return levelCountN.ToString();
            default: // RoomState
                return requiredState.ToString();   // "L" 또는 "A"
        }
    }

    // ReceiverState: receiver 비어있으면 씬에서 R 하나 자동 할당. 없으면 null.
    private SignCreature ResolveReceiver()
    {
        if (receiver == null) receiver = FindObjectOfType<SignCreature>();
        return receiver;
    }

    // 개수 조건 모드인가 (LevelCount/LevelTotal 또는 복합 needLevelCount)
    private bool IsCountMode() => useComposite ? needLevelCount
        : (conditionMode == ConditionMode.LevelCount || conditionMode == ConditionMode.LevelTotal);

    // 현재 개수 게이지를 표시할 모드인가. LevelTotal은 요청에 따라 현재 count를 띄우지 않음.
    private bool ShowCurrentCountGauge() => IsCountMode()
        && !(!useComposite && conditionMode == ConditionMode.LevelTotal);
    // 그 모드가 세는 상태 (복합=countState, 아니면 requiredState)
    private Room.RoomActivation CountStateForMode() => useComposite ? countState : requiredState;

    // 개수 조건 모드의 현재 카운트 (LevelTotal이면 L+A 합, 아니면 해당 상태 개수)
    private int CurrentModeCount()
    {
        if (!useComposite && conditionMode == ConditionMode.LevelTotal) return CountActiveTotal();
        return CountActiveRooms(CountStateForMode());
    }

    // 레벨 전체에서 L 또는 A로 활성화된 방의 총 개수
    private int CountActiveTotal()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;
        int count = 0;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            if (r.Activation == Room.RoomActivation.L || r.Activation == Room.RoomActivation.A) count++;
        }
        return count;
    }

    // 여러 부모 각각에 대해, 자식을 앞에서부터 count개 켜고 나머지는 끔.
    private void UpdateLevelCountParents(GameObject[] parents, int count)
    {
        if (parents == null) return;
        for (int p = 0; p < parents.Length; p++)
        {
            var parent = parents[p];
            if (parent == null) continue;
            Transform t = parent.transform;
            for (int i = 0; i < t.childCount; i++)
                t.GetChild(i).gameObject.SetActive(i < count);
        }
    }

    // 레벨 전체에서 이 상태(L/A)로 활성화된 방의 개수
    private int CountActiveRooms(Room.RoomActivation state)
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;

        int count = 0;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            if (r.Activation == state) count++;
        }
        return count;
    }


    public void DoorCloseAndOpen(bool open)
    {
        // 이 옵션이 켜진 문은 처음 열리는 순간부터 조건 재평가로 닫히지 않게 한다.
        if (open && stayOpenAfterFirstOpen)
            alwaysOpen = true;

        bool changed = isOpen != open;
        isOpen = open;
        // 열리면 트리거로(플레이어 통과), 닫히면 솔리드로(막음). 콜라이더 자체는 항상 켜둠.
        if (playerBlockCollider != null)
            playerBlockCollider.isTrigger = open;
        if (moveCo != null) StopCoroutine(moveCo);   // 이동 애니만 중단 (TickEvaluate는 유지)
        moveCo = StartCoroutine(MoveDoor(open));

        //불 켜기 (alwaysOpen 통로는 불 안 켬)
        if (light != null) light.SetActive(open && !alwaysOpen);
        if (RoomManager.Instance != null && Player.Instance?.currentRoom != null)
            RoomManager.Instance.UpdateActiveRooms(Player.Instance.currentRoom);

        // 열린 문 구성이 실제로 바뀌었으면 종별 카운트 갱신 알림
        if (changed && DoorManager.Existing != null) DoorManager.Existing.NotifyDoorChanged();
    }

    public void SetAlwaysOpen()
    {
        alwaysOpen = true;
        stayOpenAfterFirstOpen = false;

        if (!isOpen)
            DoorCloseAndOpen(true);
        else
        {
            if (playerBlockCollider != null) playerBlockCollider.isTrigger = true;
            if (light != null) light.SetActive(false);
        }
    }

    private IEnumerator MoveDoor(bool open)
    {
        float duration = 1.0f;

        // 양옆 두 짝이 로컬 오른쪽 축 기준 반대 방향으로 열림
        Vector3 side = transform.right;
        Vector3 rightEnd = open ? originalRightPos + side * moveDist : originalRightPos;
        Vector3 leftEnd  = open ? originalLeftPos  - side * moveDist : originalLeftPos;
        Vector3 rightStart = rightDoor != null ? rightDoor.position : Vector3.zero;
        Vector3 leftStart  = leftDoor  != null ? leftDoor.position  : Vector3.zero;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float t = e / duration;
            if (rightDoor != null) rightDoor.position = Vector3.Lerp(rightStart, rightEnd, t);
            if (leftDoor  != null) leftDoor.position  = Vector3.Lerp(leftStart, leftEnd, t);
            yield return null;
        }
        if (rightDoor != null) rightDoor.position = rightEnd;
        if (leftDoor  != null) leftDoor.position  = leftEnd;
    }
}
