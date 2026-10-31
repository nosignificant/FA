using UnityEngine;

// 카메라를 지정 비율(기본 16:9)로 고정한다. 창 비율이 달라도 같은 프레이밍을 유지하고
// 남는 영역은 검은 띠(레터박스/필러박스)로 둔다. 메인 카메라에 붙이면 됨.
// 바깥 영역을 깔끔한 검정으로 두려면 아래 bgCamera를 함께 두거나 cameraClear를 켜라(주석 참고).
[RequireComponent(typeof(Camera))]
public class LetterboxCamera : MonoBehaviour
{
    [Tooltip("목표 가로:세로 비율 (16:9 = 1.7777)")]
    public float targetAspect = 16f / 9f;

    [Tooltip("켜면 바깥(띠) 영역을 검정으로 채우는 보조 카메라를 자동 생성")]
    public bool createBlackBackground = true;

    private Camera cam;
    private int _w, _h;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (createBlackBackground) MakeBlackBg();
        Apply();
    }

    private void Update()
    {
        if (Screen.width != _w || Screen.height != _h) Apply();
    }

    private void Apply()
    {
        _w = Screen.width; _h = Screen.height;
        float windowAspect = (float)_w / Mathf.Max(1, _h);
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)   // 창이 더 세로로 김 → 위아래 띠
            cam.rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
        else                    // 창이 더 가로로 김 → 좌우 띠
        {
            float scaleWidth = 1f / scaleHeight;
            cam.rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }
    }

    // 전체 화면을 검정으로 먼저 지우는 보조 카메라 (띠 영역이 깔끔한 검정이 되게)
    private void MakeBlackBg()
    {
        var go = new GameObject("LetterboxBG");
        go.transform.SetParent(transform, false);
        var bg = go.AddComponent<Camera>();
        bg.clearFlags = CameraClearFlags.SolidColor;
        bg.backgroundColor = Color.black;
        bg.cullingMask = 0;                 // 아무것도 안 그림 — 검정 clear만
        bg.depth = cam.depth - 1;           // 메인보다 먼저
        bg.rect = new Rect(0f, 0f, 1f, 1f); // 전체 화면
    }
}
