using UnityEngine;
using UnityEngine.Rendering.Universal;

// [URP 전용] 메인(Base) 카메라에 붙이면, 지정 레이어(예: Popup)를 깊이 무시로 맨 위에 덮어 그리는
// Overlay 카메라를 만들어 Base의 Camera Stack에 추가한다. → p53 팝업 등 월드 UI가 벽에 안 가려짐.
// 사용: 1) Layer "Popup" 생성  2) 팝업 프리팹(루트+하위 전부)을 그 레이어로  3) topLayer=Popup만 지정
[RequireComponent(typeof(Camera))]
public class TopLayerCamera : MonoBehaviour
{
    [Tooltip("항상 맨 위에 그릴 레이어(예: Popup) — 반드시 팝업 전용 레이어 하나만")]
    public LayerMask topLayer;

    private Camera main;
    private Camera overlay;

    private void Start()
    {
        main = GetComponent<Camera>();

        // 안전장치: 비었거나 Everything이면 메인이 아무것도 안 그려 까매짐 → 중단.
        if (topLayer.value == 0 || topLayer.value == ~0)
        {
            Debug.LogWarning($"[TopLayerCamera] {name}: Top Layer를 'Popup' 하나만 지정하세요. 비움/Everything이면 까매짐. 중단.", this);
            enabled = false;
            return;
        }

        main.cullingMask &= ~topLayer.value;   // 메인에선 그 레이어만 제외

        var go = new GameObject("TopLayerOverlayCam");
        go.transform.SetParent(transform, false);   // 메인 따라감(위치·회전)
        overlay = go.AddComponent<Camera>();
        overlay.cullingMask = topLayer.value;
        overlay.fieldOfView = main.fieldOfView;
        overlay.orthographic = main.orthographic;
        overlay.orthographicSize = main.orthographicSize;

        // URP: Overlay 타입. (clearDepth는 스크립트로 설정 불가 — 인스펙터에서 켜야 함)
        var oData = overlay.GetUniversalAdditionalCameraData();
        oData.renderType = CameraRenderType.Overlay;

        // Base 카메라 스택에 추가
        var bData = main.GetUniversalAdditionalCameraData();
        if (!bData.cameraStack.Contains(overlay)) bData.cameraStack.Add(overlay);
    }
}
