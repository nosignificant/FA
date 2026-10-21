using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode lockOnKey = KeyCode.Tab;
    public KeyCode possessKey = KeyCode.F;

    [Header("Codex Keys")]
    public KeyCode codexToggleKey = KeyCode.C;

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
        // 상태창은 이동 가능한 오버레이라 다른 입력을 막지 않음 (병렬 처리)
        HandleCodex();

        HandleLockOn();
        HandlePossess();
        HandleEscape();
        SyncPossessUI();
    }

    // 상태창 입력. C로 토글, 열려 있으면 마우스 휠로 story 목록 스크롤.
    private void HandleCodex()
    {
        var codex = StatuesMenu.Instance;
        if (codex == null) return;

        if (Input.GetKeyDown(codexToggleKey)) codex.Toggle();

        if (!StatuesMenu.AnyOpen) return;

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f) codex.Scroll(wheel);
    }

    // F: 락온 대상이 수신기(R)면 '레벨 확정(클리어)', 아니면 빙의 토글
    private void HandlePossess()
    {
        if (!Input.GetKeyDown(possessKey)) return;

        var p = Player.Instance;
        Creature target = (p != null && p.pl != null) ? p.pl.targetCreature : null;
        if (target is SignCreature rcv)
        {
            rcv.ConfirmClear();
            return;
        }

        if (Possess != null) Possess.TogglePossess();
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

        // 우선순위: 메뉴 닫기 > 락온 해제 > 메뉴 열기
        if (menu != null && menu.IsOpen)
            menu.Close();
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
