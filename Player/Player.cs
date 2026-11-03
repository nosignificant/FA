using UnityEngine;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
    public static Player Instance;

    [Header("reference")]

    public PlayerLockOn pl;
    public PlayerControl pctrl;
    public GameObject UI_f;
    public Creature pc;
    public Room currentRoom;


    [Header("stat")]
    public string roomID = "aaaa";

    [Header("bool")]
    public bool isTracking = false;
    public bool canTracking = true;

    private CreatureData originalData;
    private CreatureData data;

    public int Stage { get; private set; }

    //event actions
    public event Action<Room> roomChanged;
    public event Action<int> OnStageChanged;   // 스토리 단계 오를 때 발행

    // advancesStory 종을 빙의하면 단계 +1 (CreaturePossess가 호출)
    public void TryAdvanceFromPossess(Creature c)
    {
        if (c == null || c.data == null || !c.data.advancesStory) return;

        // 방에 속한 스토리 생물(M 등)은 그 방이 L/A로 활성화됐을 때만 진행 (Coffin과 동일 규칙).
        // 방이 None(미활성)이면 F를 눌러도 단계 안 올림. (방 없는 생물은 그대로 허용)
        if (c.currentRoom != null
            && c.currentRoom.Activation != Room.RoomActivation.L
            && c.currentRoom.Activation != Room.RoomActivation.A)
            return;

        Stage++;
        StoryProgress.SaveStage(Stage);   // 씬 넘어가도 유지
        Debug.Log($"[Story] 단계 +1 → {Stage} ({c.data.creatureName} 빙의)");
        OnStageChanged?.Invoke(Stage);

        // 그 생물 위치에 현재 단계 대사 줄들을 팝업으로 뿌림
        Popup.Instance?.BurstStoryLines(c);

        // F 성공한 M은 방 상태와 무관하게 None 비주얼로 고정 (소비 표시)
        c.GetComponentInChildren<CreatureActivationTint>(true)?.ConsumeToNone();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        if (pl == null) pl = GetComponent<PlayerLockOn>();
        if (pc == null) pc = GetComponent<Creature>();

        Instance = this;
        Stage = StoryProgress.LoadStage();   // 이전 씬까지의 진행 복원
    }

    void Start()
    {
        foreach (var room in FindObjectsOfType<Room>())
        {
            var col = room.GetComponent<Collider>();
            if (col != null && col.bounds.Contains(transform.position))
            {
                SetRoom(room);
                break;
            }
        }
    }

    // 락온 해제 (PlayerInputManager의 ESC 처리에서 호출)
    public void Unlock()
    {
        isTracking = false;
        pl?.Unlock();
    }

    //room setting
    public void SetRoom(Room r)
    {
        if (r == null) return;
        if (currentRoom != null && currentRoom != r && pc != null)
            currentRoom.UnregisterCreature(pc);

        this.currentRoom = r;
        this.roomID = r.roomID;
        if (pc != null) r.RegisterCreature(pc);
        roomChanged?.Invoke(currentRoom);
        Debug.Log("player room set" + roomID);
    }
}
