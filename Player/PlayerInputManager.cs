using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode lockOnKey = KeyCode.Tab;
    public KeyCode possessKey = KeyCode.F;
    public KeyCode observeKey = KeyCode.C;
    public KeyCode observeConfirmKey = KeyCode.E;

    private CreaturePossess possess;

    // possess 참조를 필요할 때 안전하게 얻음 (Awake 순서로 Player.Instance가 아직 없을 수 있어 지연 해석)
    private CreaturePossess Possess
    {
        get
        {
            if (possess == null && Player.Instance != null)
                possess = Player.Instance.GetComponent<CreaturePossess>();
            return possess;
        }
    }

    private void Update()
    {
        HandleLockOn();
        HandlePossess();
        HandleObservation();
        HandleEscape();
        SyncPossessUI();
    }

    // F: 빙의 토글
    private void HandlePossess()
    {
        if (Input.GetKeyDown(possessKey) && Possess != null) Possess.TogglePossess();
    }

    // Tab: 락온 시작 / 순환
    private void HandleLockOn()
    {
        if (!Input.GetKeyDown(lockOnKey)) return;

        var p = Player.Instance;
        if (p == null) return;

        if (!p.isTracking && p.canTracking)
        {
            if (p.pl != null && p.pl.TryLock()) p.isTracking = true;
        }
        else if (p.isTracking)
        {
            p.pl?.CycleNext();
        }
    }

    // C: 관찰창 토글 / (열렸을 때) Space 이동, E 락온 확정
    private void HandleObservation()
    {
        var obs = ObservationUI.Instance;
        if (obs == null) return;

        if (Input.GetKeyDown(observeKey)) { obs.OnOff(true); return; }

        if (obs.IsOnOff)
        {
            if (Input.GetKeyDown(observeKey)) obs.Move(1);
            if (Input.GetKeyDown(observeConfirmKey)) obs.LockOnSelected();
        }
    }

    // 메뉴 네비게이션 + ESC 우선순위 체인
    private void HandleEscape()
    {
        var menu = Pause.Instance;

        // 메뉴 열려있을 때만 W/S/Return 네비게이션
        if (menu != null && menu.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) menu.Move(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) menu.Move(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) menu.Confirm();
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 우선순위: 메뉴 닫기 > 관찰창 닫기 > 락온 해제 > 메뉴 열기
        if (menu != null && menu.IsOpen)
            menu.Close();
        else if (ObservationUI.Instance != null && ObservationUI.Instance.IsOnOff)
            ObservationUI.Instance.OnOff(false);
        else if (Player.Instance != null && Player.Instance.isTracking)
            Player.Instance.Unlock();
        else if (menu != null)
            menu.Open();
    }

    // 조종 중 표시 UI 동기화 (구 Player.Update의 UI_f 토글)
    private void SyncPossessUI()
    {
        var p = Player.Instance;
        if (p == null || p.UI_f == null || Possess == null) return;
        p.UI_f.SetActive(Possess.IsPossessing);
    }
}
