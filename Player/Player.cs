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

        Stage++;
        Debug.Log($"[Story] 단계 +1 → {Stage} ({c.data.creatureName} 빙의)");
        OnStageChanged?.Invoke(Stage);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        if (pl == null) pl = GetComponent<PlayerLockOn>();
        if (pc == null) pc = GetComponent<Creature>();

        Instance = this;
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
