using System.Collections;
using UnityEngine;

// 생물이 각성(dormant 아님)일 때 자기 방 활성화색(L/A)을 입고, 휴면이면 기본색으로.
// H/S 같은 도구 생물에 붙여 "지금 깨어있음"을 색으로 보여줌.
[RequireComponent(typeof(Creature))]
public class CreatureActivationTint : MonoBehaviour
{
    [Tooltip("색 바꿀 렌더러들 (비우면 자기+자식의 모든 Renderer 자동)")]
    public Renderer[] renderers;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color)")]
    public string colorProperty = "";

    [Header("방 상태(L/A) 색으로 쓰기")]
    [Tooltip("켜면 각성/휴면 색 대신 '자기 방의 L/A 상태' 색을 씀")]
    public bool tintByRoomState = false;
    public Color lColor = new Color(0.6f, 1f, 0.3f);   // 방 L일 때
    public Color aColor = new Color(0.9f, 0.2f, 0.2f); // 방 A일 때
    public Color roomNoneColor = Color.white;          // 방 None일 때

    [Header("휴면 색 (tintByRoomState 꺼져있을 때)")]
    [Tooltip("휴면(dormant) 시 색. 각성(dormant 아님) 시엔 프리팹 원래 색으로 복원")]
    public Color dormantColor = Color.gray;
    private Color[] _origCol;   // 프리팹 원래 베이스 색 (렌더러별, 각성 복원용)

    [Header("halftone dot 색 (_DotColor)")]
    [Tooltip("점(dot) 색도 같이 바꿀지")]
    public bool tintDots = true;
    public string dotColorProperty = "_DotColor";
    // 각성/휴면 모드용 점 색
    public Color activeDotColor = Color.black;
    public Color dormantDotColor = Color.gray;
    // 방 상태 모드용 점 색
    public Color lDotColor = Color.black;
    public Color aDotColor = Color.black;
    public Color noneDotColor = Color.gray;

    [Header("outline 색 (_OutlineColor, MatteDither 등)")]
    [Tooltip("외곽선 색도 같이 바꿀지")]
    public bool tintOutline = false;
    public string outlineColorProperty = "_OutlineColor";
    // 각성/휴면 모드용
    public Color activeOutline = Color.black;
    public Color dormantOutline = Color.black;
    // 방 상태 모드용
    public Color lOutline = Color.black;
    public Color aOutline = Color.black;
    public Color noneOutline = Color.black;

    [Header("텍스처 스왑 (dormant=없음 / 활성=texture)")]
    [Tooltip("켜면 휴면 땐 텍스처 제거, 각성 땐 activeTexture 입힘")]
    public bool swapTexture = false;
    [Tooltip("텍스처 프로퍼티. URP Lit=_BaseMap, Unlit/Built-in=_MainTex")]
    public string textureProperty = "_BaseMap";
    [Tooltip("각성 시 입힐 텍스처. 비우면 프리팹에 원래 있던 텍스처를 그대로 씀")]
    public Texture activeTexture;
    private Texture[] _origTex;   // 프리팹 원래 텍스처 (렌더러별)

    public float interval = 0.3f;

    private Creature self;
    private Think2 think;
    private Renderer[] _resolved;
    private int _lastKey = int.MinValue;
    private int _lastTexState = -1;   // -1=아직 적용 안 함 (처음 1회 강제 적용)

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
    }

    private void Start()
    {
        if (swapTexture) CaptureOriginalTextures();   // 텍스처 제거 전에 원래 텍스처 저장
        if (!tintByRoomState) CaptureOriginalColors();   // 각성 시 복원할 프리팹 원래 색 저장
        StartCoroutine(Loop());
    }

    // 프리팹 머티리얼의 원래 베이스 색을 렌더러별로 저장 (각성 시 복원용)
    private void CaptureOriginalColors()
    {
        var rs = Resolve();
        _origCol = new Color[rs.Length];
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null) { _origCol[i] = Color.white; continue; }
            if (r is SpriteRenderer sr) { _origCol[i] = sr.color; continue; }
            var mat = r.sharedMaterial;   // 공유 머티리얼에서 원본 색만 읽음(인스턴스 안 만듦)
            string prop = mat != null ? ActivationColor.ResolveProp(mat, colorProperty) : null;
            _origCol[i] = (mat != null && prop != null && mat.HasProperty(prop)) ? mat.GetColor(prop) : Color.white;
        }
    }

    // 각성 시: 각 렌더러를 저장해둔 프리팹 원래 베이스 색으로 복원
    private void RestoreOriginalColors(Renderer[] rs)
    {
        if (_origCol == null || rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null || i >= _origCol.Length) continue;
            if (r is SpriteRenderer sr) { sr.color = _origCol[i]; continue; }
            var mat = r.material;   // 인스턴스
            if (mat == null) continue;
            string prop = ActivationColor.ResolveProp(mat, colorProperty);
            if (prop != null && mat.HasProperty(prop)) mat.SetColor(prop, _origCol[i]);
        }
    }

    // 프리팹 머티리얼에 이미 있던 텍스처를 렌더러별로 저장 (각성 시 복원용)
    private void CaptureOriginalTextures()
    {
        var rs = Resolve();
        _origTex = new Texture[rs.Length];
        string prop = string.IsNullOrEmpty(textureProperty) ? "_BaseMap" : textureProperty;
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null || r is SpriteRenderer) continue;
            var mat = r.sharedMaterial;   // 공유 머티리얼에서 원본 텍스처만 읽음(인스턴스 안 만듦)
            if (mat != null && mat.HasProperty(prop)) _origTex[i] = mat.GetTexture(prop);
        }
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(interval);
        while (self != null && !self.IsDead)
        {
            int key;
            Color baseCol, dotCol, outlineCol;
            bool dormant = think == null || think.dormant;

            if (tintByRoomState)
            {
                var st = self.currentRoom != null ? self.currentRoom.Activation : Room.RoomActivation.None;
                key = 10 + (int)st;   // 방상태 모드 (각성/휴면 키와 안 겹치게)
                baseCol    = st == Room.RoomActivation.A ? aColor : st == Room.RoomActivation.L ? lColor : roomNoneColor;
                dotCol     = st == Room.RoomActivation.A ? aDotColor : st == Room.RoomActivation.L ? lDotColor : noneDotColor;
                outlineCol = st == Room.RoomActivation.A ? aOutline : st == Room.RoomActivation.L ? lOutline : noneOutline;
            }
            else
            {
                key = dormant ? 0 : 1;
                baseCol    = dormantColor;   // 휴면색 (각성 시엔 아래서 프리팹 원래 색 복원)
                dotCol     = dormant ? dormantDotColor : activeDotColor;
                outlineCol = dormant ? dormantOutline : activeOutline;
            }

            if (key != _lastKey)   // 바뀔 때만 적용
            {
                var rs = Resolve();
                if (!tintByRoomState && !dormant)
                    RestoreOriginalColors(rs);                       // 각성 = 프리팹 원래 베이스 색
                else
                    ActivationColor.Apply(rs, baseCol, colorProperty);   // 베이스(_Color)
                if (tintDots && !string.IsNullOrEmpty(dotColorProperty))
                    ActivationColor.Apply(rs, dotCol, dotColorProperty);   // 점(_DotColor)
                if (tintOutline && !string.IsNullOrEmpty(outlineColorProperty))
                    ActivationColor.Apply(rs, outlineCol, outlineColorProperty);   // 외곽선(_OutlineColor)
                _lastKey = key;
            }

            // 텍스처 스왑: 휴면=없음(null) / 각성=activeTexture (dormant 변화 시에만 적용)
            if (swapTexture)
            {
                bool dormantNow = think == null || think.dormant;
                int texState = dormantNow ? 0 : 1;
                if (texState != _lastTexState)
                {
                    ApplyTexture(dormantNow);
                    _lastTexState = texState;
                }
            }

            yield return wait;
        }
    }

    // 휴면=텍스처 제거(null) / 각성=activeTexture(지정 시) 또는 프리팹 원래 텍스처
    private void ApplyTexture(bool dormant)
    {
        var rs = Resolve();
        if (rs == null) return;
        string prop = string.IsNullOrEmpty(textureProperty) ? "_BaseMap" : textureProperty;
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null || r is SpriteRenderer) continue;   // 스프라이트는 머티리얼 텍스처 방식 아님
            var mat = r.material;   // 인스턴스
            if (mat == null || !mat.HasProperty(prop)) continue;

            Texture tex = dormant ? null
                        : (activeTexture != null ? activeTexture
                           : (_origTex != null && i < _origTex.Length ? _origTex[i] : null));
            mat.SetTexture(prop, tex);
        }
    }

    private Renderer[] Resolve()
    {
        if (_resolved != null) return _resolved;
        _resolved = (renderers != null && renderers.Length > 0)
            ? renderers
            : GetComponentsInChildren<Renderer>(true);
        return _resolved;
    }
}
