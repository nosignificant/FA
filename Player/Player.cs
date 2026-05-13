using UnityEngine;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
    public static Player Instance;

    [Header("reference")]

    public PlayerLockOn pl;
    public PlayerAttack pa;

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
        if (pa == null) pa = GetComponent<PlayerAttack>();
        if (pc == null) pc = GetComponent<Creature>();

        if (plUI == null) Debug.Log("[playerLearnedUI] canvas에서 찾아와서 할당해주세요 ㅠㅠ");
        Instance = this;
    }

    void Start()
    {
        // 시작 시 플레이어가 이미 방 안에 있으면 자동 등록
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

        if (Input.GetMouseButtonDown(0))
        {
            pa.TryHitAtScreenCenter();
        }

        //c버튼
        if (Input.GetKeyDown(KeyCode.C))
        {
            isSelectingTransform = !isSelectingTransform;

            if (isSelectingTransform)
            {
                canTracking = false;
                PlayerControl.SetPlayerMove(false);
                plUI.SetVisible(true);
            }
            else
            {
                canTracking = true;
                PlayerControl.SetPlayerMove(true);
                plUI.SetVisible(false);
            }
        }

        if (isSelectingTransform)
        {
            if (Input.GetKeyDown(KeyCode.W))
                plUI.IndexUpDown(-1);

            if (Input.GetKeyDown(KeyCode.S))
                plUI.IndexUpDown(1);

            if (Input.GetKeyDown(KeyCode.E))
            {
                isSelectingTransform = false;
                canTracking = true;
                PlayerControl.SetPlayerMove(true);
                plUI.SetVisible(false);
                // TransformInto(plUI.GetSelectedData());
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInteract)
            {
                StopInteract();
                return;
            }
            if (isTracking)
            {
                isTracking = false;
                pl?.Unlock();
                return;
            }
        }
    }


    void TryStartInteract()
    {
        // if (interactSpawner == null) return;

        // // 학습
        // if (interactSpawner.spawnData != null &&
        //     !learnedForms.Contains(interactSpawner.spawnData))
        // {
        //     learnedForms.Add(interactSpawner.spawnData);
        //     //learnedPopUpUI;
        // }

        // isInteract = true;
        // PlayerControl.SetPlayerMove(false);
        // if (sqUI != null) sqUI.SetVisible(true);
    }

    void StopInteract()
    {
        // isInteract = false;
        // PlayerControl.SetPlayerMove(true);
        // if (sqUI != null) sqUI.SetVisible(false);
    }


    public void GivePlayerToken(int amount)
    {
        // spawnerToken += amount;
        // tokenChanged?.Invoke(spawnerToken);
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
