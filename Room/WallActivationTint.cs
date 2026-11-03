using System.Collections;
using UnityEngine;

// 방 활성화 상태(L/A/None)에 따라 색이 바뀌는 벽/바닥용 컴포넌트.
// Room이 GetComponentsInChildren<WallActivationTint>로 모아, 상태 바뀔 때 Apply() 호출.
// (Wall 스크립트에서 색 로직을 분리한 것 — 벽·바닥·장식 아무 오브젝트에나 붙일 수 있음)
public class WallActivationTint : MonoBehaviour
{
    [Tooltip("색 바꿀 렌더러들 (비우면 이 오브젝트+자식의 모든 Renderer 자동)")]
    public Renderer[] renderers;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동감지(_BaseColor/_Color)")]
    public string colorProperty = "";

    public Color noneColor = Color.white;
    public Color lColor = new Color(0.6f, 1f, 0.3f);   // 연두
    public Color aColor = new Color(0.9f, 0.2f, 0.2f); // 빨강

    [Header("레벨 과반 상태로 틴트")]
    [Tooltip("켜면 이 방 상태 대신 '같은 레벨(씬)에서 과반을 이루는 상태(L/A)' 색을 씀. 과반이 바뀌면 자동 갱신. 동수/0이면 noneColor.")]
    public bool byLevelMajority = false;

    [Header("순차 색 변경 (웨이브)")]
    [Tooltip("켜면 자식 스프라이트/렌더러를 한 번에 안 칠하고 하나씩 순차로 칠함")]
    public bool sequential = false;
    [Tooltip("스프라이트 하나당 간격(초)")]
    public float sequentialStep = 0.08f;
    private Coroutine _seqCo;
    private readonly Renderer[] _one = new Renderer[1];

    [Header("outline 색 (MatteDither 등)")]
    [Tooltip("외곽선 색도 상태에 따라 바꿀지 (_OutlineColor 있는 셰이더용)")]
    public bool tintOutline = false;
    public string outlineColorProperty = "_OutlineColor";
    public Color noneOutline = Color.black;
    public Color lOutline = Color.black;
    public Color aOutline = Color.black;

    [Header("emission 색 (URP Lit 등)")]
    [Tooltip("발광색도 상태에 따라 바꿀지 (_EmissionColor). URP Lit이면 _EMISSION 키워드 자동 켬)")]
    public bool tintEmission = false;
    public string emissionColorProperty = "_EmissionColor";
    [ColorUsage(true, true)] public Color noneEmission = Color.black;
    [ColorUsage(true, true)] public Color lEmission = Color.black;
    [ColorUsage(true, true)] public Color aEmission = Color.black;

    private Renderer[] _resolved;

    private Renderer[] Resolve()
    {
        if (_resolved != null) return _resolved;
        _resolved = (renderers != null && renderers.Length > 0)
            ? renderers
            : GetComponentsInChildren<Renderer>(true);
        return _resolved;
    }

    // 과반 틴트일 때 구독 중인 같은 씬 방들 (변하면 다시 칠함)
    private Room[] _subscribed;

    private void OnEnable()
    {
        // 과반 틴트: 같은 레벨 방들의 상태 변화를 구독해 과반이 바뀌면 자동 재도색.
        // (area SetActive 토글로 꺼졌다 켜지면 OnDisable에서 끊긴 구독을 여기서 다시 연결)
        if (byLevelMajority) StartCoroutine(BindMajority());
    }

    private void OnDisable()
    {
        if (_subscribed == null) return;
        foreach (var r in _subscribed)
            if (r != null) r.OnActivationChanged -= OnLevelRoomChanged;
        _subscribed = null;
    }

    // RoomManager 준비를 기다렸다가 같은 씬 방들을 구독하고 초기 1회 도색.
    private IEnumerator BindMajority()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.allRooms == null)
            yield return null;
        yield return null;   // 같은 씬 방들이 모두 등록될 시간을 한 프레임 줌

        var list = new System.Collections.Generic.List<Room>();
        foreach (var r in RoomManager.Instance.allRooms)
            if (r != null && r.gameObject.scene == gameObject.scene)
            {
                r.OnActivationChanged -= OnLevelRoomChanged;   // 중복 구독 방지
                r.OnActivationChanged += OnLevelRoomChanged;
                list.Add(r);
            }
        _subscribed = list.ToArray();

        Apply(Room.RoomActivation.None);   // 현재 과반으로 1회 칠함 (state는 Apply에서 무시됨)
    }

    private void OnLevelRoomChanged(Room.RoomActivation _) => Apply(Room.RoomActivation.None);

    // 같은 레벨(씬)에서 과반을 이루는 상태. L>A면 L, A>L면 A, 동수/0이면 None.
    private Room.RoomActivation LevelMajorityState()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.allRooms == null)
            return Room.RoomActivation.None;

        int nL = 0, nA = 0;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null || r.gameObject.scene != gameObject.scene) continue;
            if (r.Activation == Room.RoomActivation.L) nL++;
            else if (r.Activation == Room.RoomActivation.A) nA++;
        }
        if (nA > nL) return Room.RoomActivation.A;
        if (nL > nA) return Room.RoomActivation.L;
        return Room.RoomActivation.None;
    }

    public void Apply(Room.RoomActivation state)
    {
        if (byLevelMajority) state = LevelMajorityState();   // 이 방 상태 대신 레벨 과반 사용
        var rs = Resolve();

        // 순차(웨이브): 자식을 하나씩 딜레이 주며 칠함
        if (sequential && sequentialStep > 0f && isActiveAndEnabled && rs != null && rs.Length > 1)
        {
            if (_seqCo != null) StopCoroutine(_seqCo);
            _seqCo = StartCoroutine(ApplySequential(rs, state));
            return;
        }

        Color col = state == Room.RoomActivation.A ? aColor
                  : state == Room.RoomActivation.L ? lColor : noneColor;
        ActivationColor.Apply(rs, col, colorProperty);   // 베이스(_Color/_BaseColor)

        if (tintOutline && !string.IsNullOrEmpty(outlineColorProperty))
        {
            Color oc = state == Room.RoomActivation.A ? aOutline
                     : state == Room.RoomActivation.L ? lOutline : noneOutline;
            ActivationColor.Apply(rs, oc, outlineColorProperty);   // 외곽선(_OutlineColor)
        }

        if (tintEmission && !string.IsNullOrEmpty(emissionColorProperty))
        {
            Color ec = state == Room.RoomActivation.A ? aEmission
                     : state == Room.RoomActivation.L ? lEmission : noneEmission;
            ApplyEmission(rs, ec);
        }
    }

    // 렌더러들을 하나씩 순차로 칠함 (sequentialStep 간격)
    private IEnumerator ApplySequential(Renderer[] rs, Room.RoomActivation state)
    {
        var wait = new WaitForSeconds(sequentialStep);
        for (int i = 0; i < rs.Length; i++)
        {
            ApplyOne(rs[i], state);
            yield return wait;
        }
        _seqCo = null;
    }

    // 렌더러 하나에 base(+outline/emission) 색 적용
    private void ApplyOne(Renderer r, Room.RoomActivation state)
    {
        if (r == null) return;
        _one[0] = r;

        Color col = state == Room.RoomActivation.A ? aColor
                  : state == Room.RoomActivation.L ? lColor : noneColor;
        ActivationColor.Apply(_one, col, colorProperty);

        if (tintOutline && !string.IsNullOrEmpty(outlineColorProperty))
            ActivationColor.Apply(_one, state == Room.RoomActivation.A ? aOutline
                                      : state == Room.RoomActivation.L ? lOutline : noneOutline, outlineColorProperty);

        if (tintEmission && !string.IsNullOrEmpty(emissionColorProperty))
            ApplyEmission(_one, state == Room.RoomActivation.A ? aEmission
                                   : state == Room.RoomActivation.L ? lEmission : noneEmission);
    }

    // 발광색 적용 + URP Lit이면 _EMISSION 키워드 켜야 실제로 발광함
    private void ApplyEmission(Renderer[] rs, Color ec)
    {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null) continue;
            var mat = r.material;
            if (mat == null || !mat.HasProperty(emissionColorProperty)) continue;
            mat.SetColor(emissionColorProperty, ec);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
