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

    //event actions
    public event Action<Room> roomChanged;
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
