using UnityEngine;

// 자기 방의 우세종을 수신기로 보내는 발신기. Creature 하위.
// 조종(tab-f)해서 수신기에 연결하고, 연결된 상태에서 다시 tab-f로 조종 해제하면 발신을 끊는다.
//
// 주의: CreatureData에 excludeFromRoomCount=true(우세 카운트 오염 방지),
//       분해·grab 대상 제외(isGrabable 등) 설정 필요. 조종하려면 Think2 + controllable=true 필요.
//
// 2단계에서 tentacleGrab으로 연결선을 시각화하고, 수신기 근접(isTrigger) 판정으로 연결 처리 예정.
public class SignalTransmitter : Creature
{
    [Tooltip("이 발신기가 읽는 방 (비우면 자기 currentRoom)")]
    public Room sourceRoom;

    private SignalReceiver receiver;
    private int slot = -1;

    public Room SourceRoom => sourceRoom != null ? sourceRoom : currentRoom;
    public bool IsConnected => receiver != null;
    public SignalReceiver ConnectedReceiver => receiver;
    // 텐타클이 향할 지점 = 수신기의 연결 앵커(없으면 수신기 중심)
    public Transform ConnectedReceiverTransform => receiver != null ? receiver.AnchorTransform : null;

    // tab UI용: 보내는 곳(수신기 방 이름)과 보내는 값(내 방 우세종)
    public Room ConnectedRoom => receiver != null ? receiver.currentRoom : null;
    public CreatureData CurrentSignal => SourceRoom != null ? SourceRoom.MostNumerousSpecies() : null;

    protected override void Awake()
    {
        base.Awake();
        if (sourceRoom == null) sourceRoom = currentRoom;
    }

    // 수신기가 슬롯에 물릴 때 통지
    public void SetReceiver(SignalReceiver r, int s) { receiver = r; slot = s; }

    // 수신기에 연결 (수신기 근처에서 조종 해제 시 — 2단계에서 근접 판정과 함께 호출)
    public void ConnectTo(SignalReceiver r)
    {
        if (r == null) return;
        r.Connect(this);
    }

    // 발신 끊기
    public void Disconnect()
    {
        if (receiver != null) receiver.DisconnectTransmitter(this);
        receiver = null;
        slot = -1;
    }

    // 수신기 쪽에서 밀어냈을 때 통지받음 (텐타클을 idle로 되돌리는 것도 여기서)
    public void NotifyDisconnected()
    {
        receiver = null;
        slot = -1;
    }

    // 조종 해제(f) 시 CreaturePossess가 호출.
    //   락온 대상이 수신기면 → 그 수신기에 연결 (기존 연결은 자동 정리)
    //   아니면, 이미 연결돼 있으면 → 발신 끊기 / 그 외엔 아무 것도 안 함(그냥 조종 해제)
    public void OnPossessReleased()
    {
        SignalReceiver locked = LockedReceiver();
        if (locked != null) ConnectTo(locked);
        else if (IsConnected) Disconnect();
    }

    // 현재 락온한 대상이 수신기면 반환
    private SignalReceiver LockedReceiver()
    {
        var pl = Player.Instance != null ? Player.Instance.pl : null;
        return pl != null ? pl.targetCreature as SignalReceiver : null;
    }
}
