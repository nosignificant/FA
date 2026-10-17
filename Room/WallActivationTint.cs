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

    [Header("레벨 선택으로 틴트 (ChoiceProgress 기반)")]
    [Tooltip("켜면 방 상태 대신 '지정 레벨의 클리어 선택(A/L)' 색으로 고정 (한 번 칠함, 방 상태 무시)")]
    public bool byLevelChoice = false;
    [Tooltip("몇 번째 레벨 기록을 볼지 (1=level1, 2=level2...). ChoiceProgress 기록 순서")]
    public int choiceLevelIndex = 1;

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

    private void Start()
    {
        // 레벨 선택 기반이면 시작 시 기록된 상태로 한 번 칠함 (Room의 상태 변경 호출은 Apply에서 무시/덮어씀)
        if (byLevelChoice) Apply(Room.RoomActivation.None);
    }

    // ChoiceProgress에서 지정 레벨의 선택(A/L)을 RoomActivation으로. 기록 없으면 None.
    private Room.RoomActivation RecordedLevelState()
    {
        var all = ChoiceProgress.All();
        int i = choiceLevelIndex - 1;
        if (i < 0 || i >= all.Count) return Room.RoomActivation.None;
        var c = all[i];
        if (c == CreatureTypes.CreatureID.A) return Room.RoomActivation.A;
        if (c == CreatureTypes.CreatureID.L) return Room.RoomActivation.L;
        return Room.RoomActivation.None;
    }

    public void Apply(Room.RoomActivation state)
    {
        if (byLevelChoice) state = RecordedLevelState();   // 방 상태 대신 레벨 기록 사용
        var rs = Resolve();

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
