using UnityEngine;
using CreatureTypes;

// R은 자신이 있는 방의 현재 L/A 상태를 레벨 전체에 전파한다.
// 플레이어가 락온 + F를 누를 때마다 현재 상태를 다시 읽어 전파하며,
// 레벨 선택은 플레이어가 포탈에 들어가는 순간 R이 있는 방의 상태로 확정한다.
public class SignCreature : Creature
{
    [Header("초기 신호")]
    [Tooltip("씬 시작 시 R이 있는 방의 상태(L/A)를 다른 모든 방에 전파")]
    public bool broadcastRoomStateOnStart = true;

    [Header("신호 표시")]
    [Tooltip("색을 바꿀 렌더러들. 비우면 자식 렌더러 전부 자동 수집")]
    public Renderer[] connectionRenderers;
    [Tooltip("connectionRenderers가 비어있을 때 자식 렌더러 전부 자동 수집")]
    public bool autoCollectChildRenderers = true;
    [Tooltip("색 프로퍼티. 기본 _Color (SpriteRenderer는 .color 폴백)")]
    public string colorProperty = "_Color";
    public Color idleColor = Color.black;
    [Tooltip("R이 있는 방이 L 상태일 때 색")]
    public Color connectedColor = Color.green;
    [Tooltip("R이 있는 방이 A 상태일 때 색")]
    public Color connectedColorA = new Color(0.4f, 0.7f, 1f);

    [Header("신호 연출")]
    [Tooltip("이 오브젝트 아래에서 Rotate를 찾아, R 방 상태가 L/A이면 회전")]
    public Transform rotateParent;
    private Rotate[] _rots;

    [Header("호환용")]
    [Tooltip("이전 R 확정 연출용 필드. 신호 전파 시에는 숨기지 않음")]
    public GameObject hideOnActivate;

    [System.NonSerialized] public bool HasPlayerPublishedSignal;

    // 마지막으로 전파한 값을 캐시하지 않고 R의 현재 방에서 읽는다.
    public Room.RoomActivation CurrentSignalState
    {
        get
        {
            Room sourceRoom = GetSourceRoom();
            return sourceRoom != null ? sourceRoom.Activation : Room.RoomActivation.None;
        }
    }

    private Room.RoomActivation _lastVisualState = (Room.RoomActivation)(-1);

    protected override void Awake()
    {
        base.Awake();

        if (autoCollectChildRenderers && (connectionRenderers == null || connectionRenderers.Length == 0))
            connectionRenderers = GetComponentsInChildren<Renderer>();

        if (rotateParent != null) _rots = rotateParent.GetComponentsInChildren<Rotate>(true);
        RefreshVisual();
    }

    private void Start()
    {
        if (broadcastRoomStateOnStart)
            StartCoroutine(BroadcastRoomStateAtStart());
    }

    private void Update()
    {
        if (_lastVisualState != CurrentSignalState)
            RefreshVisual();
    }

    // Room.Start의 초기 등록과 활성화 계산이 끝난 뒤 R의 방 상태를 한 번 전파한다.
    private System.Collections.IEnumerator BroadcastRoomStateAtStart()
    {
        float deadline = Time.time + 5f;
        Room sourceRoom = null;
        Room.RoomActivation state = Room.RoomActivation.None;

        while (Time.time < deadline)
        {
            sourceRoom = GetSourceRoom();
            state = sourceRoom != null ? sourceRoom.Activation : Room.RoomActivation.None;
            if (RoomManager.Instance != null && state != Room.RoomActivation.None)
                break;
            yield return null;
        }

        if (RoomManager.Instance == null || sourceRoom == null || state == Room.RoomActivation.None)
        {
            Debug.LogWarning($"[SignCreature] {name}: 초기 신호를 보낼 L/A 상태의 방을 찾지 못했습니다.", this);
            yield break;
        }

        BroadcastStateToOtherRooms(sourceRoom, state);
    }

    // F: R이 있는 방의 '현재' 상태를 읽어 다른 방에 재전파한다.
    public bool PublishRoomState()
    {
        Room sourceRoom = GetSourceRoom();
        Room.RoomActivation state = sourceRoom != null ? sourceRoom.Activation : Room.RoomActivation.None;
        if (sourceRoom == null || (state != Room.RoomActivation.L && state != Room.RoomActivation.A))
        {
            Popup.Instance?.BurstMessage(this, "no state", Color.gray);
            return false;
        }

        BroadcastStateToOtherRooms(sourceRoom, state);
        HasPlayerPublishedSignal = true;
        Popup.Instance?.BurstMessage(this, state == Room.RoomActivation.A ? "signal A" : "signal L",
            state == Room.RoomActivation.A ? connectedColorA : connectedColor);
        return true;
    }

    public static CreatureID ChoiceForState(Room.RoomActivation state)
    {
        return state == Room.RoomActivation.A ? CreatureID.A : CreatureID.L;
    }

    private void BroadcastStateToOtherRooms(Room sourceRoom, Room.RoomActivation state)
    {
        if (RoomManager.Instance != null)
            foreach (Room room in RoomManager.Instance.rooms.Values)
                if (room != null && room != sourceRoom)
                    room.ForceActivation(state);

        // tint components outside a Room hierarchy can still reflect the new level-wide state.
        foreach (WallActivationTint tint in FindObjectsOfType<WallActivationTint>())
            if (tint != null) tint.Apply(state);

        RefreshVisual();
    }

    private Room FindRoomAtCurrentPosition()
    {
        if (RoomManager.Instance == null) return null;
        Vector3 pos = rootTransform != null ? rootTransform.position : transform.position;
        foreach (Room room in RoomManager.Instance.rooms.Values)
            if (room != null && room.homeBound != null && room.homeBound.bounds.Contains(pos))
                return room;
        return null;
    }

    private Room GetSourceRoom()
    {
        if (currentRoom != null && currentRoom.homeBound != null)
        {
            Vector3 pos = rootTransform != null ? rootTransform.position : transform.position;
            if (currentRoom.homeBound.bounds.Contains(pos)) return currentRoom;
        }

        return FindRoomAtCurrentPosition() ?? currentRoom;
    }

    private void RefreshVisual()
    {
        Room.RoomActivation state = CurrentSignalState;
        _lastVisualState = state;

        if (_rots != null)
            foreach (Rotate r in _rots)
                if (r != null) r.isSelfRotate = state == Room.RoomActivation.L || state == Room.RoomActivation.A;

        Color color = state == Room.RoomActivation.A ? connectedColorA
            : state == Room.RoomActivation.L ? connectedColor
            : idleColor;

        if (connectionRenderers == null) return;
        foreach (Renderer renderer in connectionRenderers)
        {
            if (renderer == null) continue;
            Material material = renderer.material;
            if (!string.IsNullOrEmpty(colorProperty) && material.HasProperty(colorProperty))
                material.SetColor(colorProperty, color);
            else
                material.color = color;
        }
    }
}
