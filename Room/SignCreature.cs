using UnityEngine;
using CreatureTypes;

// R은 자신이 있는 방의 현재 L/A 상태를 '그대로' 반영하는 수동 센서다.
// 다른 방에 전파(방송)하지 않으며, 문이 직접 R의 방 상태(CurrentSignalState)를 읽어 여닫는다.
public class SignCreature : Creature
{
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

    // 캐시하지 않고 R의 현재 방 상태를 그대로 읽는다.
    public Room.RoomActivation CurrentSignalState
    {
        get
        {
            Room sourceRoom = GetSourceRoom();
            return sourceRoom != null ? sourceRoom.Activation : Room.RoomActivation.None;
        }
    }

    // 시각 표시용 상태: 언제나 R이 있는 방의 현재 상태를 그대로 반영.
    private Room.RoomActivation VisualState => CurrentSignalState;

    private Room.RoomActivation _lastVisualState = (Room.RoomActivation)(-1);

    protected override void Awake()
    {
        base.Awake();

        if (autoCollectChildRenderers && (connectionRenderers == null || connectionRenderers.Length == 0))
            connectionRenderers = GetComponentsInChildren<Renderer>();

        if (rotateParent != null) _rots = rotateParent.GetComponentsInChildren<Rotate>(true);
        RefreshVisual();
    }

    private void Update()
    {
        if (_lastVisualState != VisualState)
            RefreshVisual();
    }

    public static CreatureID ChoiceForState(Room.RoomActivation state)
    {
        return state == Room.RoomActivation.A ? CreatureID.A : CreatureID.L;
    }

    private Room FindRoomAtCurrentPosition()
    {
        if (RoomManager.Instance == null) return null;
        Vector3 pos = rootTransform != null ? rootTransform.position : transform.position;
        foreach (Room room in RoomManager.Instance.allRooms)
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
        Room.RoomActivation state = VisualState;
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
