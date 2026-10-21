using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 신호 생물(R): 플레이어가 락온 + F로 '이 생물이 있는 방의 현재 상태(L/A)'를 레벨 확정(클리어)한다.
// 확정되면 활성화 연출(회전·색·지정 오브젝트 숨김) + 레벨 전체 방 상태를 그 L/A로 고정.
// (구 SignalReceiver — 발신기·슬롯 시스템 제거, 클리어 전용으로 단순화)
public class SignCreature : Creature
{
    [Header("활성화 표시 (확정 시)")]
    [Tooltip("색을 바꿀 렌더러들. 비우면 자식 렌더러 전부 자동 수집")]
    public Renderer[] connectionRenderers;
    [Tooltip("connectionRenderers가 비어있을 때 자식 렌더러 전부 자동 수집")]
    public bool autoCollectChildRenderers = true;
    [Tooltip("색 프로퍼티. 기본 _Color (SpriteRenderer는 .color 폴백)")]
    public string colorProperty = "_Color";
    public Color idleColor = Color.black;         // 확정 전
    [Tooltip("L 상태로 확정 시 색")]
    public Color connectedColor = Color.green;    // L 확정
    [Tooltip("A 상태로 확정 시 색")]
    public Color connectedColorA = new Color(0.4f, 0.7f, 1f);   // A 확정

    // 확정 상태의 표시 색 (A/L에 따라 다름). 확정 전이면 idleColor.
    private Color ActiveColor => IsConfirmed
        ? (ConfirmedState == Room.RoomActivation.A ? connectedColorA : connectedColor)
        : idleColor;

    [Header("확정 시 회전")]
    [Tooltip("이 오브젝트 아래에서 Rotate를 찾아, 확정되면 isSelfRotate=true(회전)")]
    public Transform rotateParent;
    private Rotate[] _rots;

    [Header("확정 시 숨길 오브젝트")]
    [Tooltip("확정(활성화)되면 이 오브젝트를 숨김(SetActive false)")]
    public GameObject hideOnActivate;

    // ── 레벨 클리어(확정) ────────────────────────────────────────────────
    [System.NonSerialized] public bool IsConfirmed;
    [System.NonSerialized] public Room.RoomActivation ConfirmedState;

    protected override void Awake()
    {
        base.Awake();

        if (autoCollectChildRenderers && (connectionRenderers == null || connectionRenderers.Length == 0))
            connectionRenderers = GetComponentsInChildren<Renderer>();

        // rotateParent 아래 Rotate 전부 수집 (refPrefab 0~3 등 여러 개 동시 회전)
        if (rotateParent != null) _rots = rotateParent.GetComponentsInChildren<Rotate>(true);

        RefreshVisual();
    }

    // 확정 상태(IsConfirmed)에 따라 회전·색·숨김 갱신
    private void RefreshVisual()
    {
        if (_rots != null)
            foreach (var r in _rots)
                if (r != null) r.isSelfRotate = IsConfirmed;

        if (connectionRenderers != null)
        {
            Color c = ActiveColor;
            foreach (var r in connectionRenderers)
            {
                if (r == null) continue;
                var mat = r.material;   // 인스턴스 (공유 에셋 안 건드림)
                if (!string.IsNullOrEmpty(colorProperty) && mat.HasProperty(colorProperty))
                    mat.SetColor(colorProperty, c);
                else
                    mat.color = c;
            }
        }

        // 활성화되면 지정 오브젝트 숨김
        if (hideOnActivate != null) hideOnActivate.SetActive(!IsConfirmed);
    }

    // 플레이어가 락온 + F → 이 생물이 속한 방의 현재 상태(L/A)로 레벨 확정.
    public void ConfirmClear()
    {
        if (IsConfirmed) return;

        Room room = currentRoom;
        Room.RoomActivation state = room != null ? room.Activation : Room.RoomActivation.None;
        if (state == Room.RoomActivation.None)         // None은 확정 불가
        {
            Popup.Instance?.BurstMessage(this, "no state", Color.gray);
            return;
        }

        IsConfirmed = true;
        ConfirmedState = state;

        // 1) 레벨 선택 기록 (L/A). byLevelChoice 벽이 읽으므로 tint 갱신 전에.
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
        Popup.Instance?.BurstMessage(this, "confirmed", ActiveColor);
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
}
