
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    [Header("Options")]
    public KeyCode observe = KeyCode.C;
    public KeyCode observeConfirm = KeyCode.E;
    void Update()
    {
        // LockOn Input //
        // LockOn Input //
        // LockOn Input //

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //tracking 하고 있지 않으면 
            if (!isTracking && canTracking)
            {
                bool locked = pl != null && pl.TryLock();
                if (locked) isTracking = true;
            }
            else if (isTracking)
            {
                pl?.CycleNext();
            }
        }

        // observationUI Input //
        // observationUI Input //
        // observationUI Input //

        if (Input.GetKeyDown(observe)) { ObservationUI.Instance.OnOff(true); return; }
        if (Input.GetKeyDown(observe)) { ObservationUI.Instance.Move(1); }
        if (ObservationUI.Instance.IsOpen && Input.GetKeyDown(observeConfirm))
        {
            LockOnSelected();
            return;
        }

        // ESCmenu Input //
        // ESCmenu Input //
        // ESCmenu Input //

        if (EscMenu.Instance.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                EscMenu.Instance.Move(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                EscMenu.Instance.Move(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                EscMenu.Instance.Confirm();
        }

        // esc 순서: 
        // 1. observationUI를 먼저 끈다
        // 2. lockOn을 끈다 
        // 3. esc 켜져있으면 esc를 끈다
        // 4. esc가 꺼져있으면 켠다 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (EscMenu.Instance.IsOpen) { EscMenu.Instance.Open(); return; }
            //esc가 닫혀 있는 상태에서 
            else
            {
                if (ObservationUI.Instance.IsOpen) { ObservationUI.Instance.OnOff(false); }
                else
                {
                    if (Player.Instance.isTracking)
                    {
                        isTracking = false;
                        pl?.Unlock();
                        lastUnlockFrame = Time.frameCount;
                        return;
                    }
                }
            }
        }
    }
}
