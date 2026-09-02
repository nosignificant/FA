using System;
using System.Collections;
using UnityEngine;
using CreatureTypes;

public class Door : MonoBehaviour
{
    [Header("References")]
    public Room roomA;
    public Room roomB;
    public Creature self;
    public GameObject light;
    public Rotate rot;
    public Rotate rot2;

    [Header("Door")]
    public Collider playerBlockCollider;
    public float moveDist = 10f;
    public Transform upperDoor;
    public Transform lowerDoor;

    public enum ConditionMode
    {
        Local,       // 이 문에 붙은 방에서 watchingCreature가 우세하면 열림
        LevelCount,  // 레벨 전체에서 watchingCreature가 우세한 방이 levelCountN개 이상이면 열림
        SignalGate,  // 발신기 신호들을 논리게이트(AND/OR/NOT/EQUALS)로 판정
    }

    [Header("Condition")]
    public ConditionMode conditionMode = ConditionMode.Local;
    public CreatureData watchingCreature;
    [Tooltip("LevelCount 모드: watchingCreature가 우세한 방이 레벨 안에 이 개수 이상이면 열림")]
    public int levelCountN = 3;

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
    private bool conditionA = false;
    private bool conditionB = false;
    private float originalUpperY;
    private float originalLowerY;
    private Coroutine moveCo;

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

        if (upperDoor != null) originalUpperY = upperDoor.position.y;
        if (lowerDoor != null) originalLowerY = lowerDoor.position.y;
    }

    void Start()
    {
        if (roomA == null) Debug.LogWarning($"[Door {name}] roomA 미설정");
        if (roomB == null) Debug.LogWarning($"[Door {name}] roomB 미설정 (인스펙터/RoomEditor에서 할당 필요)");

        if (roomA != null) roomA.RegisterCreature(self);
        if (roomB != null && roomB != roomA) roomB.RegisterCreature(self);

        roomA?.RegisterDoor(this);
        roomB?.RegisterDoor(this);

        // 조명을 현재 isOpen 상태에 맞춰 초기화 (닫힌 채 시작 시 조명 꺼짐)
        if (light != null) light.SetActive(isOpen);
        ApplyRotate(isOpen);

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

        // LevelCount·SignalGate는 roomA/roomB 이벤트만으로 부족(다른 방/문·신호 변화에 반응해야 함) → 주기 확인
        if (conditionMode != ConditionMode.Local)
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
                if (watchingCreature == null) return;
                shouldOpen = CountDominantRooms(watchingCreature) >= levelCountN;
                break;

            case ConditionMode.SignalGate:
                shouldOpen = EvaluateGate();
                break;

            default: // Local — 양쪽 방 중 한쪽이라도 우세면 열림 (문은 두 방 사이)
                if (watchingCreature == null) return;   // 조건 없는 문(튜토리얼 등)은 자동 개폐 안 함
                conditionA = roomA != null && CheckCondition(roomA.MostNumerousSpecies());
                conditionB = roomB != null && CheckCondition(roomB.MostNumerousSpecies());
                shouldOpen = conditionA || conditionB;
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
            case ConditionMode.Local:
                return roomA != null ? roomA.roomID : "-";
            case ConditionMode.LevelCount:
                return watchingCreature != null ? watchingCreature.creatureName : "-";
            case ConditionMode.SignalGate:
                return gate == GateType.EQUALS ? InputRoom(InA) : InputSpecies(InA);
            default: return "-";
        }
    }

    // status 칸 (연산/동사)
    public string ConditionLabel()
    {
        switch (conditionMode)
        {
            case ConditionMode.LevelCount: return "dom";
            case ConditionMode.SignalGate:
                switch (gate)
                {
                    case GateType.NOT:    return "Not";
                    case GateType.OR:     return "OR";
                    case GateType.EQUALS: return "equals";
                    default:              return "AND";
                }
            default: return "is";   // Local
        }
    }

    // target 칸 (조건2)
    public string GateTargetLabel()
    {
        switch (conditionMode)
        {
            case ConditionMode.Local:
                return watchingCreature != null ? watchingCreature.creatureName : "-";
            case ConditionMode.LevelCount:
                return $"{levelCountN}+ rooms";
            case ConditionMode.SignalGate:
                if (gate == GateType.NOT) return "";
                return gate == GateType.EQUALS ? InputRoom(InB) : InputSpecies(InB);
            default: return "-";
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

    // 방에 가장 많은 종족이 이 문이 요구하는 종족과 같은가 (H·HH는 같은 종족으로 취급)
    private bool CheckCondition(CreatureData best)
    {
        return best != null && watchingCreature != null
            && CreatureFamily.Same(best.creatureID, watchingCreature.creatureID);
    }

    // 레벨 전체에서 이 문이 요구하는 종족이 우세한 방의 개수
    private int CountDominantRooms(CreatureData species)
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;

        int count = 0;
        foreach (var r in RoomManager.Instance.rooms.Values)
        {
            if (r == null) continue;
            var best = r.MostNumerousSpecies();
            if (best != null && CreatureFamily.Same(best.creatureID, species.creatureID)) count++;
        }
        return count;
    }

    private void ApplyRotate(bool open)
    {
        if (rot != null) rot.isSelfRotate = open;
        if (rot2 != null) rot2.isSelfRotate = open;
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
        //회로 연결한 척 하기
        ApplyRotate(open);
        if (RoomManager.Instance != null && Player.Instance?.currentRoom != null)
            RoomManager.Instance.UpdateActiveRooms(Player.Instance.currentRoom);

        // 열린 문 구성이 실제로 바뀌었으면 종별 카운트 갱신 알림
        if (changed && DoorManager.Existing != null) DoorManager.Existing.NotifyDoorChanged();
    }

    private IEnumerator MoveDoor(bool open)
    {
        float upperTarget = open ? originalUpperY + moveDist : originalUpperY;
        float lowerTarget = open ? originalLowerY - moveDist : originalLowerY;

        float elapsed = 0f;
        float duration = 1.0f;

        Vector3 upperStart = upperDoor.position;
        Vector3 lowerStart = lowerDoor.position;
        Vector3 upperEnd = new Vector3(upperDoor.position.x, upperTarget, upperDoor.position.z);
        Vector3 lowerEnd = new Vector3(lowerDoor.position.x, lowerTarget, lowerDoor.position.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            upperDoor.position = Vector3.Lerp(upperStart, upperEnd, t);
            lowerDoor.position = Vector3.Lerp(lowerStart, lowerEnd, t);
            yield return null;
        }

        upperDoor.position = upperEnd;
        lowerDoor.position = lowerEnd;
    }
}
