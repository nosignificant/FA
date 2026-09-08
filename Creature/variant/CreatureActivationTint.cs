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

    [Header("각성/휴면 색 (tintByRoomState 꺼져있을 때)")]
    [Tooltip("각성(dormant 아님) 시 이 생물 고유의 색 (베이스 _Color)")]
    public Color activeColor = new Color(0.3f, 0.8f, 1f);
    [Tooltip("휴면(dormant) 시 색")]
    public Color dormantColor = Color.gray;

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

    public float interval = 0.3f;

    private Creature self;
    private Think2 think;
    private Renderer[] _resolved;
    private int _lastKey = int.MinValue;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
    }

    private void Start() => StartCoroutine(Loop());

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(interval);
        while (self != null && !self.IsDead)
        {
            int key;
            Color baseCol, dotCol, outlineCol;

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
                bool dormant = think == null || think.dormant;
                key = dormant ? 0 : 1;
                baseCol    = dormant ? dormantColor : activeColor;
                dotCol     = dormant ? dormantDotColor : activeDotColor;
                outlineCol = dormant ? dormantOutline : activeOutline;
            }

            if (key != _lastKey)   // 바뀔 때만 적용
            {
                var rs = Resolve();
                ActivationColor.Apply(rs, baseCol, colorProperty);   // 베이스(_Color)
                if (tintDots && !string.IsNullOrEmpty(dotColorProperty))
                    ActivationColor.Apply(rs, dotCol, dotColorProperty);   // 점(_DotColor)
                if (tintOutline && !string.IsNullOrEmpty(outlineColorProperty))
                    ActivationColor.Apply(rs, outlineCol, outlineColorProperty);   // 외곽선(_OutlineColor)
                _lastKey = key;
            }
            yield return wait;
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
