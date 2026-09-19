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

    public enum OpenMode
    {
        SlideDown,  // 일반 문: 오브젝트(rightDoor)가 아래로 내려가며 열림
        SlideSide,  // 양옆 문: rightDoor/leftDoor 두 짝이 좌우로 열림
    }

    [Header("Door")]
    public Collider playerBlockCollider;
    [Tooltip("열림 방식: 아래로 내려가기 / 양옆으로")]
    public OpenMode openMode = OpenMode.SlideDown;
    [Tooltip("열릴 때 이동 거리 (아래로/좌우로)")]
    public float moveDist = 10f;

    [Header("SlideDown 용 (아래로 내려가는 문 오브젝트)")]
    public Transform downDoor;

    [Header("SlideSide 용 (양옆 두 짝)")]
    [FormerlySerializedAs("upperDoor")] public Transform rightDoor;
    [FormerlySerializedAs("lowerDoor")] public Transform leftDoor;

    public enum ConditionMode
    {
        RoomState,   // 양쪽 방 중 하나가 requiredState(L/A)로 활성화되면 열림
        LevelCount,  // 레벨 전체에서 requiredState로 활성화된 방이 levelCountN개 이상이면 열림
        SignalGate,  // 발신기 신호들을 논리게이트(AND/OR/NOT/EQUALS)로 판정
    }

    [Header("Condition")]
    public ConditionMode conditionMode = ConditionMode.RoomState;

    [Tooltip("RoomState/LevelCount 모드가 요구하는 방 활성화 상태 (L/A)")]
    public Room.RoomActivation requiredState = Room.RoomActivation.A;

    [Tooltip("LevelCount 모드: requiredState로 활성화된 방이 레벨 안에 이 개수 이상이면 열림")]
    public int levelCountN = 3;

    [Header("Condition Graphics (선택)")]
    [Tooltip("조건 종류별로 켤 그래픽. 비워두면 아무것도 안 함. 시작 시·조건 바뀔 때 해당하는 것만 켜짐.")]
    public GameObject levelCountGraphic;
    public GameObject signalGateGraphic;

    [Header("RoomState 그래픽 (requiredState별 다른 스프라이트)")]
    [Tooltip("RoomState 모드 + requiredState=L 일 때 켤 그래픽")]
    public GameObject roomStateGraphicL;
    [Tooltip("RoomState 모드 + requiredState=A 일 때 켤 그래픽")]
    public GameObject roomStateGraphicA;

    [Header("RoomState 그래픽 색 틴트")]
    [Tooltip("켜면 L/A 그래픽을 각각 지정한 색으로 칠함")]
    public bool tintRoomStateGraphic = true;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color, SpriteRenderer는 .color)")]
    public string roomStateColorProperty = "";
    public Color lColor = new Color(0.6f, 1f, 0.3f);   // L 그래픽(스프라이트) 색
    public Color aColor = new Color(0.9f, 0.2f, 0.2f); // A 그래픽(스프라이트) 색

    [Header("문짝(3D, 양옆 오브젝트) 색 틴트 — 스프라이트와 별개")]
    [Tooltip("켜면 left/right 문짝을 requiredState(L/A)에 맞는 색으로 칠함")]
    public bool tintDoorPanels = false;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color)")]
    public string doorPanelColorProperty = "";
    public Color lDoorColor = new Color(0.6f, 1f, 0.3f);   // L일 때 문짝 색
    public Color aDoorColor = new Color(0.9f, 0.2f, 0.2f); // A일 때 문짝 색

    [Header("SignalGate")]
    [Tooltip("이 문이 읽을 수신기. 있으면 슬롯0/1을 입력으로 사용")]
    public SignalReceiver receiver;
    public GateType gate = GateType.AND;
    [Tooltip("receiver가 없을 때 직접 지정하는 입력 A (NOT·게이트배선 등)")]
    public SignalInput inputA = new SignalInput();
    [Tooltip("receiver가 없을 때 직접 지정하는 입력 B")]
    public SignalInput inputB = new SignalInput();

    [Tooltip("LevelCount·SignalGate 재평가 주기(초). 다른 방/문 변화에 반응해야 해서 주기적으로 확인")]
    public float checkInterval = 0.25f;

    [Header("State")]
    public bool isOpen = false;
    [Tooltip("항상 열린 통로. 조건·다른 문과 무관하게 계속 열려 있음 (isOpen 런타임 상태와 별개)")]
    public bool alwaysOpen = false;
    private Vector3 originalRightPos;
    private Vector3 originalLeftPos;
    private Vector3 originalDownPos;
    private Coroutine moveCo;

    // 조건 종류(conditionMode)에 맞는 그래픽만 켜기. 슬롯 비면 무시.
    // RoomState면 requiredState(L/A)에 맞는 스프라이트만 켜고 나머지는 끔.
    public void ApplyConditionGraphic()
    {
        bool isRoomState = conditionMode == ConditionMode.RoomState;
        if (levelCountGraphic != null) levelCountGraphic.SetActive(conditionMode == ConditionMode.LevelCount);
        if (signalGateGraphic != null) signalGateGraphic.SetActive(conditionMode == ConditionMode.SignalGate);

        bool wantL = isRoomState && requiredState == Room.RoomActivation.L;
        bool wantA = isRoomState && requiredState == Room.RoomActivation.A;
        if (roomStateGraphicL != null) roomStateGraphicL.SetActive(wantL);
        if (roomStateGraphicA != null) roomStateGraphicA.SetActive(wantA);

        // 켜진 그래픽(스프라이트)을 각 상태색으로 틴트
        if (tintRoomStateGraphic)
        {
            if (wantL && roomStateGraphicL != null) TintGraphic(roomStateGraphicL, lColor, roomStateColorProperty);
            if (wantA && roomStateGraphicA != null) TintGraphic(roomStateGraphicA, aColor, roomStateColorProperty);
        }

        // 문짝(3D) 색 — 스프라이트와 별개. requiredState에 맞는 색으로. 본체 Renderer만(자식 제외).
        if (tintDoorPanels && isRoomState)
        {
            Color dc = requiredState == Room.RoomActivation.A ? aDoorColor : lDoorColor;
            if (rightDoor != null) TintSelf(rightDoor.gameObject, dc, doorPanelColorProperty);
            if (leftDoor  != null) TintSelf(leftDoor.gameObject,  dc, doorPanelColorProperty);
        }
    }

    // 자식 포함 전부 칠하기 (스프라이트 그래픽용)
    private void TintGraphic(GameObject go, Color col, string prop)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        ActivationColor.Apply(rs, col, prop);
    }

    // 이 오브젝트의 Renderer만 칠하기 (자식 제외)
    private void TintSelf(GameObject go, Color col, string prop)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) ActivationColor.Apply(new[] { r }, col, prop);
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
        if (downDoor != null) originalDownPos = downDoor.position;

        ApplyOpenModeVisual();   // 모드에 맞는 오브젝트만 켜기
    }

    // openMode에 맞는 문 오브젝트만 활성화 (SlideDown=downDoor / SlideSide=right·leftDoor)
    private void ApplyOpenModeVisual()
    {
        bool down = openMode == OpenMode.SlideDown;
        if (downDoor  != null) downDoor.gameObject.SetActive(down);
        if (rightDoor != null) rightDoor.gameObject.SetActive(!down);
        if (leftDoor  != null) leftDoor.gameObject.SetActive(!down);
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

        // 조명을 현재 isOpen 상태에 맞춰 초기화 (닫힌 채 시작 시 조명 꺼짐)
        if (light != null) light.SetActive(isOpen);

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

        // LevelCount·SignalGate는 roomA/roomB 이벤트만으로 부족(다른 방/문·신호 변화에 반응해야 함) → 주기 확인.
        // RoomState는 양쪽 방의 OnCreatureCountChanged(활성화 변경 포함)로 충분.
        if (conditionMode == ConditionMode.LevelCount || conditionMode == ConditionMode.SignalGate)
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
        if (alwaysOpen) return;   // 항상 열린 통로는 조건과 무관하게 계속 열림

        bool shouldOpen;

        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
                shouldOpen = CountActiveRooms(requiredState) >= levelCountN;
                break;

            case ConditionMode.SignalGate:
                shouldOpen = EvaluateGate();
                break;

            default: // RoomState — 양쪽 방 중 하나가 requiredState로 활성화되면 열림
                bool aMatch = roomA != null && roomA.Activation == requiredState;
                bool bMatch = roomB != null && roomB.Activation == requiredState;
                shouldOpen = aMatch || bMatch;
                break;
        }

        if (shouldOpen != isOpen) DoorCloseAndOpen(shouldOpen);   // 상태 바뀔 때만
    }

    // 유효 수신기: 문에 직접 지정한 게 있으면 그것, 없으면 이 문이 속한 방(roomA)의 수신기(R)
    private SignalReceiver EffectiveReceiver
        => receiver != null ? receiver : (roomA != null ? roomA.signalReceiver : null);

    // SignalGate 입력: 수신기가 있으면 슬롯, 없으면 직접 지정한 inputA/B
    private SignalInput InA => EffectiveReceiver != null ? EffectiveReceiver.slot0 : inputA;
    private SignalInput InB => EffectiveReceiver != null ? EffectiveReceiver.slot1 : inputB;

    private bool EvaluateGate()
    {
        switch (gate)
        {
            case GateType.NOT:
                return !InA.IsOn();

            case GateType.OR:
                return InA.IsOn() || InB.IsOn();

            case GateType.EQUALS:
                var a = InA.Species();
                var b = InB.Species();
                return a != null && b != null && CreatureFamily.Same(a.creatureID, b.creatureID);

            default: // AND
                return InA.IsOn() && InB.IsOn();
        }
    }

    // 문 HUD: name=조건1 / status=연산 / target=조건2 로 좌→우로 읽힘
    //   Local:      이방 / 우세   / H
    //   LevelCount: H   / 우세방  / 3↑
    //   AND/OR:     H   / AND     / A
    //   EQUALS:     방1 / 일치    / 방2
    //   NOT:        H   / 아님    / (빈칸)

    // name 칸 (조건1)
    public string Operand1Label()
    {
        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
                return requiredState.ToString();
            case ConditionMode.SignalGate:
                return gate == GateType.EQUALS ? InputRoom(InA) : InputSpecies(InA);
            default: // RoomState
                return roomA != null ? roomA.roomID : "-";
        }
    }

    // status 칸 (연산/동사)
    public string ConditionLabel()
    {
        switch (conditionMode)
        {
            case ConditionMode.LevelCount: return "rooms ≥";   // "rooms ≥"
            case ConditionMode.SignalGate:
                switch (gate)
                {
                    case GateType.NOT:    return "Not";
                    case GateType.OR:     return "OR";
                    case GateType.EQUALS: return "equals";
                    default:              return "AND";
                }
            default: return "active";   // RoomState
        }
    }

    // target 칸 (조건2)
    public string GateTargetLabel()
    {
        switch (conditionMode)
        {
            case ConditionMode.LevelCount:
                return levelCountN.ToString();
            case ConditionMode.SignalGate:
                if (gate == GateType.NOT) return "";
                return gate == GateType.EQUALS ? InputRoom(InB) : InputSpecies(InB);
            default: // RoomState
                return requiredState.ToString();   // "L" 또는 "A"
        }
    }

    private string InputSpecies(SignalInput inp)
    {
        if (inp == null) return "-";
        if (inp.source == SignalInput.Source.Door) return inp.door != null ? inp.door.name : "문";
        return inp.target != null ? inp.target.creatureName : "?";
    }

    private string InputRoom(SignalInput inp)
    {
        if (inp == null) return "-";
        if (inp.room != null) return inp.room.roomID;
        if (inp.compareRoom != null) return inp.compareRoom.roomID;
        return "?";
    }

    // 레벨 전체에서 이 상태(L/A)로 활성화된 방의 개수
    private int CountActiveRooms(Room.RoomActivation state)
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;

        int count = 0;
        foreach (var r in RoomManager.Instance.rooms.Values)
        {
            if (r == null) continue;
            if (r.Activation == state) count++;
        }
        return count;
    }


    public void DoorCloseAndOpen(bool open)
    {
        bool changed = isOpen != open;
        isOpen = open;
        // 열리면 트리거로(플레이어 통과), 닫히면 솔리드로(막음). 콜라이더 자체는 항상 켜둠.
        if (playerBlockCollider != null)
            playerBlockCollider.isTrigger = open;
        if (moveCo != null) StopCoroutine(moveCo);   // 이동 애니만 중단 (TickEvaluate는 유지)
        moveCo = StartCoroutine(MoveDoor(open));

        //불 켜기
        if (light != null) light.SetActive(open);
        if (RoomManager.Instance != null && Player.Instance?.currentRoom != null)
            RoomManager.Instance.UpdateActiveRooms(Player.Instance.currentRoom);

        // 열린 문 구성이 실제로 바뀌었으면 종별 카운트 갱신 알림
        if (changed && DoorManager.Existing != null) DoorManager.Existing.NotifyDoorChanged();
    }

    private IEnumerator MoveDoor(bool open)
    {
        float duration = 1.0f;

        if (openMode == OpenMode.SlideDown)
        {
            // 일반 문: downDoor를 아래로
            if (downDoor == null) yield break;
            Vector3 end = open ? originalDownPos + Vector3.down * moveDist : originalDownPos;
            Vector3 start = downDoor.position;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                downDoor.position = Vector3.Lerp(start, end, e / duration);
                yield return null;
            }
            downDoor.position = end;
        }
        else
        {
            // 양옆 문: 로컬 오른쪽 축 기준 반대 방향으로
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
}
