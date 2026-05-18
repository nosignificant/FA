using UnityEngine;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
    public static Player Instance;

    [Header("reference")]

    public PlayerLockOn pl;
    public PlayerControl pctrl;
    public Creature pc;
    public PlayerLearnedUI plUI;
    public Room currentRoom;


    [Header("stat")]

    public int spawnerToken = 0;
    public string roomID = "aaaa";

    [Header("bool")]
    public bool isTracking = false;
    public bool isInteract = false;
    public bool canTracking = true;

    private CreatureData originalData;
    private CreatureData data;

    public List<CreatureData> learnedForms = new();

    //event actions
    public event Action<Room> roomChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        if (pl == null) pl = GetComponent<PlayerLockOn>();
        if (pc == null) pc = GetComponent<Creature>();

        if (plUI == null) Debug.Log("[playerLearnedUI] canvas에서 찾아와서 할당해주세요 ㅠㅠ");
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

    void Update()
    {
        // StoryUI가 열려있으면 Player 입력 전부 양보
        if (StoryUI.Instance != null && StoryUI.Instance.IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!isTracking && canTracking)
            {
                // 처음 락온
                bool locked = pl != null && pl.TryLock();
                if (locked) isTracking = true;
            }
            else if (isTracking)
            {
                // 이미 추적 중 → 다음 후보로 순환
                pl?.CycleNext();
            }
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ObservationUI가 이 프레임 ESC를 닫기용으로 소비했으면 lockOn 안 건드림
            if ((ObservationUI.Instance != null && ObservationUI.Instance.IsOpen)
                || ObservationUI.EscConsumedThisFrame)
                return;

            if (isTracking)
            {
                isTracking = false;
                pl?.Unlock();
                return;
            }
        }
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
