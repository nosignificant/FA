using UnityEngine;

// 화면 좌상단에 실시간 FPS/프레임타임 표시. 아무 GameObject에 붙이면 됨 (웹 빌드에서도 보임).
//  OnGUI라 Canvas 세팅 불필요. 배포 전엔 비활성/삭제하거나 showOnlyInDevelopmentBuild로 숨김.
public class FpsCounter : MonoBehaviour
{
    [Tooltip("표시 갱신 주기(초) — 숫자가 너무 빨리 바뀌지 않게 평균냄")]
    public float updateInterval = 0.5f;
    [Tooltip("글자 크기")]
    public int fontSize = 28;
    [Tooltip("켜면 Development Build / 에디터에서만 표시 (최종 배포 빌드에선 자동 숨김)")]
    public bool developmentOnly = false;

    private float _accum;      // 구간 내 1/deltaTime 합
    private int _frames;       // 구간 내 프레임 수
    private float _timeLeft;
    private float _fps;
    private float _ms;
    private GUIStyle _style;

    private void Start() => _timeLeft = updateInterval;

    private void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accum += 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f);
        _frames++;

        if (_timeLeft <= 0f)
        {
            _fps = _accum / Mathf.Max(_frames, 1);
            _ms = 1000f / Mathf.Max(_fps, 1e-5f);
            _timeLeft = updateInterval;
            _accum = 0f;
            _frames = 0;
        }
    }

    private void OnGUI()
    {
        if (developmentOnly && !Debug.isDebugBuild && !Application.isEditor) return;

        if (_style == null)
            _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };

        // 60↑ 녹색 / 30~60 노랑 / 30↓ 빨강
        _style.normal.textColor = _fps >= 60f ? Color.green : (_fps >= 30f ? Color.yellow : Color.red);

        string text = $"{_fps:0.} FPS  ({_ms:0.0} ms)";
        // 가독성용 그림자
        var shadow = _style.normal.textColor;
        _style.normal.textColor = Color.black;
        GUI.Label(new Rect(12, 10, 400, 60), text, _style);
        _style.normal.textColor = shadow;
        GUI.Label(new Rect(10, 8, 400, 60), text, _style);
    }
}
