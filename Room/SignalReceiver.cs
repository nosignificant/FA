using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 신호 수신기. 발신기 2개까지 받아 문(논리게이트)에 신호를 공급한다. Creature 하위(수신 생물 그 자체).
// 슬롯 2개(slot0/slot1)의 compare·target·compareRoom은 설계 시(인스펙터) 고정,
// 발신기가 런타임에 각 슬롯의 room만 바꿔 꽂는다.
//
// 주의: 발신기와 동일하게 CreatureData에 excludeFromRoomCount=true,
//       분해·grab 제외. 슬롯 해제(tab-f)를 위해 controllable=true + Think2 필요.
public class SignalReceiver : Creature
{
    public enum InputMode
    {
        BothFromTransmitter,   // 두 슬롯 다 발신기로 받음
        LocalPlusTransmitter,  // 슬롯0 = 이 수신기 방(자동), 슬롯1 = 발신기
    }
    [Header("입력 모드")]
    [Tooltip("LocalPlusTransmitter면 슬롯0은 이 수신기가 속한 방(hierarchy)으로 자동 고정, 발신기는 슬롯1에만 연결")]
    public InputMode inputMode = InputMode.BothFromTransmitter;

    public SignalInput slot0 = new SignalInput();
    public SignalInput slot1 = new SignalInput();

    private Room localRoom;   // LocalPlusTransmitter: 슬롯0에 넣을 이 수신기의 방

    [Header("연결 표시")]
    [Tooltip("발신기 텐타클이 향할 지점. 비우면 수신기 중심")]
    public Transform connectAnchor;
    [Tooltip("연결 상태에 따라 색을 바꿀 렌더러들 (전부 같은 색으로). 비우면 자식 렌더러 전부 자동 수집")]
    public Renderer[] connectionRenderers;
    [Tooltip("connectionRenderers가 비어있을 때, 자식의 모든 렌더러를 자동으로 대상에 넣음")]
    public bool autoCollectChildRenderers = true;
    [Tooltip("색을 바꿀 셰이더 프로퍼티 이름. 기본 _Color. 커스텀 셰이더면 다를 수 있음(_BaseColor, _Tint 등)")]
    public string colorProperty = "_Color";
    public Color idleColor = Color.black;      // 연결 안 됨 (검정)
    public Color connectedColor = Color.green;

    [Header("연결 시 회전")]
    [Tooltip("이 오브젝트 아래에서 Rotate를 찾음. 연결되면 isSelfRotate=true(회전) / 끊기면 false(정지)")]
    public Transform rotateParent;
    private Rotate _rot;

    // 각 슬롯에 물린 발신기와 연결 시각(FIFO 판정용)
    private SignalTransmitter tx0, tx1;
    private float t0, t1;

    // 발신기 텐타클이 향할 위치
    public Transform AnchorTransform => connectAnchor != null ? connectAnchor : transform;

    public SignalInput GetSlot(int i) => i == 0 ? slot0 : slot1;

    // HUD용: 슬롯 i의 받는 방 이름 (로컬 슬롯이면 "이방")
    public string SlotRoomName(int i)
    {
        var s = GetSlot(i);
        if (s == null) return "-";
        if (inputMode == InputMode.LocalPlusTransmitter && i == 0)
        {
            var lr = localRoom != null ? localRoom : currentRoom;
            return lr != null ? lr.roomID : "-";
        }
        return s.room != null ? s.room.roomID : "-";
    }

    // HUD용: 슬롯 i가 받는 방의 우세종
    public string SlotSpecies(int i)
    {
        var s = GetSlot(i);
        var sp = (s != null && s.room != null) ? s.room.MostNumerousSpecies() : null;
        return sp != null ? sp.creatureName : "-";
    }

    // HUD용: 슬롯 i가 지금 '받고 있는' 것 — [받는 방]: [그 방 우세종]
    public string SlotLabel(int i)
    {
        var s = GetSlot(i);
        if (s == null) return "-";

        string where;
        if (inputMode == InputMode.LocalPlusTransmitter && i == 0)
            where = (localRoom != null ? localRoom : currentRoom)?.roomID ?? "-";
        else if (s.room != null)
            where = s.room.roomID;
        else
            return "-";

        var sp = s.room != null ? s.room.MostNumerousSpecies() : null;
        return $"{where}: {(sp != null ? sp.creatureName : "-")}";
    }

    protected override void Awake()
    {
        base.Awake();
        localRoom = GetComponentInParent<Room>();   // 이 수신기가 속한 방 (hierarchy 기준)

        // 비어있으면 자식 렌더러 전부 수집 (그 오브젝트 밑 머티리얼 전부 같이 바뀌게)
        if (autoCollectChildRenderers && (connectionRenderers == null || connectionRenderers.Length == 0))
            connectionRenderers = GetComponentsInChildren<Renderer>();

        // 지정 오브젝트 아래에서 Rotate 찾기 (연결 상태로 회전 on/off)
        if (rotateParent != null) _rot = rotateParent.GetComponentInChildren<Rotate>(true);

        RefreshVisual();
    }

    // Creature.Update를 가리지 않도록 LateUpdate 사용. 로컬 모드면 슬롯0을 이 방으로 계속 고정.
    private void LateUpdate()
    {
        if (inputMode == InputMode.LocalPlusTransmitter && slot0 != null)
            slot0.room = localRoom != null ? localRoom : currentRoom;
    }

    private int ConnectedCount()
    {
        int n = 0;
        if (slot0 != null && slot0.room != null) n++;
        if (slot1 != null && slot1.room != null) n++;
        return n;
    }

    private void RefreshVisual()
    {
        bool connected = ConnectedCount() > 0;

        // 연결되면 회전, 끊기면 정지
        if (_rot != null) _rot.isSelfRotate = connected;

        if (connectionRenderers == null) return;

        Color c = connected ? connectedColor : idleColor;
        foreach (var r in connectionRenderers)
        {
            if (r == null) continue;
            var mat = r.material;   // 인스턴스 (공유 에셋 안 건드림)
            if (!string.IsNullOrEmpty(colorProperty) && mat.HasProperty(colorProperty))
                mat.SetColor(colorProperty, c);
            else
                mat.color = c;   // _Color 폴백
        }
    }

    // 발신기 연결. 빈 슬롯 우선, 둘 다 차면 먼저 연결된 슬롯(FIFO)을 밀어낸다.
    public void Connect(SignalTransmitter tx)
    {
        if (tx == null) return;

        // 이미 어느 슬롯에 물려있으면 room만 갱신
        if (tx0 == tx) { slot0.room = tx.SourceRoom; return; }
        if (tx1 == tx) { slot1.room = tx.SourceRoom; return; }

        int slot;
        if (inputMode == InputMode.LocalPlusTransmitter)
            slot = 1;                              // 로컬 모드: 발신기는 슬롯1 전용 (슬롯0은 이 방 고정)
        else if (tx0 == null) slot = 0;
        else if (tx1 == null) slot = 1;
        else slot = (t0 <= t1) ? 0 : 1;            // 둘 다 참 → 먼저 연결된 슬롯 밀어냄

        var old = slot == 0 ? tx0 : tx1;
        if (old != null && old != tx) old.NotifyDisconnected();

        BindSlot(slot, tx);
    }

    private void BindSlot(int slot, SignalTransmitter tx)
    {
        var input = GetSlot(slot);
        input.source = SignalInput.Source.Room;
        input.room = tx.SourceRoom;   // 발신기는 방만 바꿈. compare/target/compareRoom은 설계값 유지.

        if (slot == 0) { tx0 = tx; t0 = Time.time; }
        else { tx1 = tx; t1 = Time.time; }

        tx.SetReceiver(this, slot);
        RefreshVisual();

        Popup.Instance?.BurstMessage(this, "connected", Color.green);
    }

    // ── 레벨 클리어(확정) ────────────────────────────────────────────────
    // 플레이어가 락온 + F → 이 수신기가 속한 방의 현재 상태(L/A)로 레벨 확정.
    [System.NonSerialized] public bool IsConfirmed;
    [System.NonSerialized] public Room.RoomActivation ConfirmedState;

    public void ConfirmClear()
    {
        if (IsConfirmed) return;                       // 이미 확정됨

        Room room = currentRoom;
        Room.RoomActivation state = room != null ? room.Activation : Room.RoomActivation.None;
        if (state == Room.RoomActivation.None)         // None은 확정 불가
        {
            Popup.Instance?.BurstMessage(this, "no state", Color.gray);
            return;
        }

        IsConfirmed = true;
        ConfirmedState = state;

        // 1) 레벨 선택 기록 (L/A 중 무엇으로 클리어했는지). byLevelChoice 벽이 읽으므로 tint 갱신 전에.
        ChoiceProgress.Record(state == Room.RoomActivation.A ? CreatureID.A : CreatureID.L);

        // 2) 이 방의 L/A 입자를 전부 확정 상태 종으로 변경
        ConvertRoomParticles(room, state);

        // 3) 모든 방을 확정 상태로 고정(latch) — 방 상태 기반 tint도 함께 갱신
        if (RoomManager.Instance != null)
            foreach (var r in RoomManager.Instance.rooms.Values)
                if (r != null) r.ForceActivation(state);

        // 4) 레벨 전체 WallActivationTint를 확정 상태 색으로 (byLevelChoice 벽 포함)
        foreach (var tint in FindObjectsOfType<WallActivationTint>())
            if (tint != null) tint.Apply(state);

        RefreshVisual();
        Popup.Instance?.BurstMessage(this, "confirmed", connectedColor);
    }

    // 방 안의 L/A 입자를 targetState(L/A)에 해당하는 종으로 교체 (다른 종은 그대로)
    private void ConvertRoomParticles(Room room, Room.RoomActivation targetState)
    {
        if (room == null) return;
        CreatureID targetId = targetState == Room.RoomActivation.A ? CreatureID.A : CreatureID.L;
        GameObject prefab = room.creatureDB != null ? room.creatureDB.GetPrefab(targetId) : null;
        if (prefab == null) return;

        var snapshot = new List<Creature>(room.creatureList);   // 순회 중 리스트 변경되므로 복사
        foreach (var c in snapshot)
        {
            if (c == null || c.data == null) continue;
            CreatureID id = c.data.creatureID;
            if (id != CreatureID.L && id != CreatureID.A) continue;   // 입자만
            if (id == targetId) continue;                              // 이미 같음

            Vector3 pos = c.rootTransform != null ? c.rootTransform.position : c.transform.position;
            room.UnregisterCreature(c);
            Destroy(c.gameObject);

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
            obj.transform.SetParent(CreatureRoot.Container, true);
            Creature nc = obj.GetComponent<Creature>();
            if (nc != null) room.RegisterCreature(nc);
        }
    }

    // 발신기 쪽에서 끊음(tab-f) → 해당 슬롯 비움
    public void DisconnectTransmitter(SignalTransmitter tx)
    {
        if (tx0 == tx) { tx0 = null; slot0.room = null; }
        else if (tx1 == tx) { tx1 = null; slot1.room = null; }
        RefreshVisual();
    }

    // 수신기 쪽에서 슬롯 지정 해제(tab-f로 슬롯 선택)
    public void DisconnectSlot(int slot)
    {
        var tx = slot == 0 ? tx0 : tx1;
        if (tx != null) tx.NotifyDisconnected();
        if (slot == 0) { tx0 = null; slot0.room = null; }
        else { tx1 = null; slot1.room = null; }
        RefreshVisual();
    }
}
