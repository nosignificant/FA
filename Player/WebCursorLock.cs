using UnityEngine;

// 웹(WebGL)에서 마우스가 캔버스 밖으로 새는 문제 해결.
//  브라우저는 '사용자 클릭' 없이는 포인터 잠금(Pointer Lock)을 안 걸어주고,
//  Esc/탭 전환 시 잠금을 강제로 풀어버린다. → 잠금이 풀린 플레이 중엔 "클릭하여 계속" 오버레이를
//  띄우고, 클릭하는 순간 다시 잠근다. (표준 웹 FPS 방식)
//
//  PC/에디터에선 OS가 알아서 잠그므로 이 스크립트는 자동으로 비활성(간섭 안 함).
//  오버레이는 전체화면 UI 패널(클릭 받는 Image + "클릭하여 플레이" 안내)을 할당.
public class WebCursorLock : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private const bool IsWeb = true;
#else
    private const bool IsWeb = false;
#endif

    [Tooltip("잠금이 풀렸을 때 띄울 전체화면 오버레이 (클릭하면 재잠금). 없어도 클릭 재잠금은 동작")]
    public GameObject clickOverlay;

    private void Start()
    {
        // 웹이 아니면 할 일 없음 — 오버레이 끄고 스스로 비활성
        if (!IsWeb)
        {
            if (clickOverlay != null) clickOverlay.SetActive(false);
            enabled = false;
            return;
        }
        if (clickOverlay != null) clickOverlay.SetActive(false);
    }

    private void Update()
    {
        // 지금 '잠겨 있어야 하는' 상태인가 = 플레이 중(메뉴/일시정지가 아님)
        bool menuOpen = (Pause.Instance != null && Pause.Instance.IsOpen) || StatuesMenu.AnyOpen;
        bool wantLock = !menuOpen;

        if (!wantLock)
        {
            // 메뉴 중엔 커서 자유 — 오버레이 숨김 (게임 쪽에서 커서 표시를 관리)
            if (clickOverlay != null && clickOverlay.activeSelf) clickOverlay.SetActive(false);
            return;
        }

        // 플레이 중인데 잠금이 안 걸려 있음(최초 진입 or Esc로 풀림) → 오버레이 띄우고 클릭 대기
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (clickOverlay != null && !clickOverlay.activeSelf) clickOverlay.SetActive(true);
            Cursor.visible = true;   // 클릭할 수 있게 커서 보이기

            // 클릭하는 '그 순간'에만 브라우저가 잠금을 허용함
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (clickOverlay != null) clickOverlay.SetActive(false);
            }
        }
        else
        {
            // 정상 잠금 상태 — 오버레이 숨김
            if (clickOverlay != null && clickOverlay.activeSelf) clickOverlay.SetActive(false);
        }
    }
}
