using System.Collections;
using UnityEngine;

// 방 활성화 상태(None/L/A)에 따라 서로 다른 오브젝트 묶음을 켜고 끈다.
// 예: 관(coffin)이 L이면 열린 모습, A면 닫힌 모습 등 상태별 비주얼 교체.
// 색만 바꾸는 WallActivationTint와 달리, 이건 GameObject 자체를 활성/비활성한다.
public class Coffin : MonoBehaviour
{
    [Header("대상 방")]
    [Tooltip("상태를 따라갈 방. 비우면 부모의 Room → 그래도 없으면 이 위치가 속한 방을 자동 탐색")]
    public Room room;

    [Header("상태별 오브젝트 (해당 상태일 때 켜짐)")]
    [Tooltip("방이 None(미활성)일 때 켤 오브젝트들")]
    public GameObject[] noneObjects;
    [Tooltip("방이 L일 때 켤 오브젝트들")]
    public GameObject[] lObjects;
    [Tooltip("방이 A일 때 켤 오브젝트들")]
    public GameObject[] aObjects;

    [Tooltip("켜면 현재 상태 세트만 켜고 나머지 세트는 모두 끔. 끄면 해당 세트만 켜고 다른 세트는 건드리지 않음")]
    public bool deactivateOthers = true;

    [Tooltip("room 자동 탐색 시 RoomManager 준비를 기다리는 최대 시간(초)")]
    public float resolveTimeout = 5f;

    [Header("스토리 (Tab 락온 + F) — 구 M 대체")]
    [Tooltip("켜면 이 코핀을 Tab으로 락온 후 F를 눌러 스토리 한 단계를 획득할 수 있음. " +
             "(락온되려면 이 오브젝트에 Creature 컴포넌트가 있어야 하고, 그 CreatureData.advancesStory가 켜져 있어야 함)")]
    public bool storyOnInteract = false;
    [Tooltip("스토리 팝업/단계용 Creature. 비우면 같은 오브젝트의 Creature 자동 사용")]
    public Creature storyCreature;
    private bool _storyUsed;   // 한 번 획득하면 이후 F는 무시 (항상 일회성)
    private bool _consumed;   // 스토리를 획득한 뒤로는 계속 비활성(None) 모습 유지

    // PlayerInputManager가 F 입력 시 호출. 스토리를 소비했으면 true(→ 빙의로 안 넘어감).
    // L/A로 활성화된 상태에서만 실제 획득되고, 그 외엔 F를 소비만 한다(빙의 방지).
    public bool TryTriggerStory()
    {
        if (!storyOnInteract) return false;
        if (_storyUsed) return true;   // 한 번 획득하면 이후 F는 소비만(다시 획득 불가)

        // L/A로 활성화된 상태에서만 F 가능 (None이면 아무 일 없음)
        Room r = _room != null ? _room : ResolveRoom();
        var state = r != null ? r.Activation : Room.RoomActivation.None;
        if (state != Room.RoomActivation.L && state != Room.RoomActivation.A) return true;

        Creature c = storyCreature != null ? storyCreature : GetComponent<Creature>();
        if (c == null)
        {
            Debug.LogWarning($"[Coffin] {name}: storyOnInteract인데 Creature가 없어 스토리를 띄울 수 없습니다.", this);
            return false;
        }

        Player.Instance?.TryAdvanceFromPossess(c);   // 단계 +1 + 현재 단계 대사 팝업 (advancesStory 필요)
        _storyUsed = true;

        _consumed = true;        // 이후 방이 L/A여도 코핀은 계속 None 비주얼
        ShowNoneVisual();        // deactivateOthers 설정과 무관하게 L/A 세트를 끄고 None 세트만 켬
        return true;
    }

    private Room _room;
    private Room.RoomActivation _lastState = (Room.RoomActivation)(-1);

    private void OnEnable() => StartCoroutine(Bind());

    private void OnDisable()
    {
        if (_room != null) _room.OnActivationChanged -= OnActivationChanged;
        _room = null;
        _lastState = (Room.RoomActivation)(-1);
    }

    // 방을 찾을 때까지(또는 타임아웃) 기다렸다가 이벤트 구독 + 현재 상태 1회 적용.
    private IEnumerator Bind()
    {
        float deadline = Time.time + resolveTimeout;
        Room r = ResolveRoom();
        while (r == null && Time.time < deadline)
        {
            yield return null;
            r = ResolveRoom();
        }
        if (r == null)
        {
            Debug.LogWarning($"[Coffin] {name}: 따라갈 방을 찾지 못했습니다. room을 직접 할당해 주세요.", this);
            yield break;
        }

        _room = r;
        _room.OnActivationChanged += OnActivationChanged;
        Apply(_room.Activation);   // 현재 상태 즉시 반영
    }

    private void OnActivationChanged(Room.RoomActivation state) => Apply(state);

    private void Apply(Room.RoomActivation state)
    {
        if (_consumed) state = Room.RoomActivation.None;   // 스토리 획득한 코핀은 계속 비활성 모습
        if (state == _lastState) return;
        _lastState = state;

        SetSet(lObjects,    state == Room.RoomActivation.L);
        SetSet(aObjects,    state == Room.RoomActivation.A);
        SetSet(noneObjects, state == Room.RoomActivation.None);
    }

    // on이 true면 세트를 켠다. false면 deactivateOthers일 때만 끈다.
    private void SetSet(GameObject[] set, bool on)
    {
        if (set == null) return;
        if (!on && !deactivateOthers) return;
        for (int i = 0; i < set.Length; i++)
            if (set[i] != null) set[i].SetActive(on);
    }

    // 스토리 소비(F) 후 코핀을 None 비주얼로 강제 전환.
    // deactivateOthers가 꺼져 있어도 L/A 세트는 반드시 끄고 None 세트만 켠다(기존 L/A가 남아 None처럼 안 보이는 문제 방지).
    private void ShowNoneVisual()
    {
        _lastState = Room.RoomActivation.None;
        ForceSet(lObjects, false);
        ForceSet(aObjects, false);
        ForceSet(noneObjects, true);
    }

    private static void ForceSet(GameObject[] set, bool on)
    {
        if (set == null) return;
        for (int i = 0; i < set.Length; i++)
            if (set[i] != null) set[i].SetActive(on);
    }

    // room(인스펙터) → 부모 Room → 위치가 속한 방 순으로 해석.
    private Room ResolveRoom()
    {
        if (room != null) return room;
        var parent = GetComponentInParent<Room>();
        if (parent != null) return parent;
        return FindRoomByPosition();
    }

    private Room FindRoomByPosition()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return null;
        Vector3 pos = transform.position;
        foreach (var kv in RoomManager.Instance.rooms)
        {
            var r = kv.Value;
            if (r == null || r.homeBound == null) continue;
            if (r.homeBound.bounds.Contains(pos)) return r;
        }
        return null;
    }
}
