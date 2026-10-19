using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 카메라 DoF(focusDistance) 중앙 관리. 흩어져 있던 초점 설정을 한곳에 모음.
// 우선순위: 상태창/일시정지(statusFocus) > 생물 조종(possessFocus) > 외부 base override > defaultFocus.
// 외부(튜토리얼 등)는 SetBaseFocus/ClearBaseFocus로 '평소값'만 덮어씀(상태창·조종보다 우선순위 낮음).
public class FocusController : MonoBehaviour
{
    public static FocusController Instance;

    [Header("DoF Volume")]
    [Tooltip("DoF(Bokeh) 오버라이드가 있는 Volume. 비우면 PlayerLockOn 카메라에서 자동 탐색")]
    public Volume dofVolume;

    [Header("focusDistance 값")]
    [Tooltip("상태창/일시정지 — 전체 흐림 (0=카메라 바로 앞)")]
    public float statusFocus = 0f;
    [Tooltip("생물 조종 중 — 약한 흐림")]
    public float possessFocus = 15f;
    [Tooltip("평소(상태창·조종 아닐 때) 기본값")]
    public float defaultFocus = 25f;
    [Tooltip("focusDistance 전환 속도(단위/초). 0 이하면 즉시")]
    public float blurFadeSpeed = 60f;

    [System.Serializable]
    public struct RoomFocus
    {
        [Tooltip("이 방(roomID)에 있을 때")]
        public string roomID;
        [Tooltip("평소값 대신 쓸 focusDistance")]
        public float focus;
    }

    [Header("방별 focus (평소값 덮어쓰기)")]
    [Tooltip("플레이어가 이 방에 있으면 목록의 focus를 평소값으로 사용. 목록에 없는 방은 defaultFocus")]
    public RoomFocus[] roomFocusOverrides;

    private DepthOfField _dof;
    private bool _dofReady;
    private CreaturePossess _possess;

    private bool _hasBaseOverride;   // 외부(튜토리얼 등)가 평소 focusDistance를 덮어씀
    private float _baseOverride;

    // 평소(상태창/조종 아닐 때) focusDistance 기준값을 외부에서 설정/해제.
    public void SetBaseFocus(float value) { _baseOverride = value; _hasBaseOverride = true; }
    public void ClearBaseFocus() { _hasBaseOverride = false; }

    private void Awake() => Instance = this;
    private void Start() => ResolveDoF();

    // DoF Volume을 찾아 DepthOfField 오버라이드 확보. 비어있으면 PlayerLockOn 카메라에서 자동 탐색.
    private void ResolveDoF()
    {
        if (_dofReady) return;

        Volume vol = dofVolume;
        if (vol == null)
        {
            Camera cam = (Player.Instance != null && Player.Instance.pl != null) ? Player.Instance.pl.cam : null;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                vol = cam.GetComponent<Volume>();
                if (vol == null) vol = cam.GetComponentInChildren<Volume>();
            }
        }

        if (vol != null && vol.profile != null && vol.profile.TryGet(out _dof))
            _dofReady = true;
    }

    // 상태에 따라 focusDistance를 매 프레임 목표값으로 수렴.
    private void Update()
    {
        if (!_dofReady) { ResolveDoF(); if (!_dofReady) return; }

        bool paused = Pause.Instance != null && Pause.Instance.IsOpen;
        // 평소값: 외부 override(SetBaseFocus) > 방별 테이블 > defaultFocus
        float target = _hasBaseOverride ? _baseOverride : RoomBaseFocus();
        if (StatuesMenu.AnyOpen || paused) target = statusFocus;          // 상태창·일시정지 우선(가장 강한 흐림)
        else if (IsPossessing()) target = possessFocus;

        float cur = _dof.focusDistance.value;
        _dof.focusDistance.value = (blurFadeSpeed <= 0f)
            ? target
            : Mathf.MoveTowards(cur, target, blurFadeSpeed * Time.unscaledDeltaTime);
    }

    // 플레이어가 있는 방의 focus (테이블에 있으면 그 값, 없으면 defaultFocus)
    private float RoomBaseFocus()
    {
        Room room = Player.Instance != null ? Player.Instance.currentRoom : null;
        if (room != null && roomFocusOverrides != null)
            for (int i = 0; i < roomFocusOverrides.Length; i++)
                if (roomFocusOverrides[i].roomID == room.roomID)
                    return roomFocusOverrides[i].focus;
        return defaultFocus;
    }

    private bool IsPossessing()
    {
        if (_possess == null) _possess = FindObjectOfType<CreaturePossess>();
        return _possess != null && _possess.IsPossessing;
    }
}
