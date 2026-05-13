using System;
using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

public class Room : MonoBehaviour
{
    public string roomID;
    public bool isActive = false;

    [Header("room bounds")]
    public Collider homeBound;
    public Vector3 roomSize = new Vector3(10f, 0f, 10f);

    [Header("door slots")]
    public Transform slotN;
    public Transform slotS;
    public Transform slotE;
    public Transform slotW;

    [Header("room outside")]
    public List<Door> doors = new();

    [Header("room inside")]
    public List<Creature> creatureList = new();
    public int maxCreaturesInRoom = 5;
    public Dictionary<CreatureData, int> decomposedCounts = new();
    public event Action<Creature, CreatureID> OnCreatureDecomposed;

    public (CreatureData, int) MostDecomposedAndSecond()
    {
        CreatureData best = null, second = null;
        int bestCount = 0, secondCount = 0;

        foreach (var kv in decomposedCounts)
        {
            if (kv.Value > bestCount)
            {
                second = best; secondCount = bestCount;
                best = kv.Key; bestCount = kv.Value;
            }
            else if (kv.Value > secondCount)
            {
                second = kv.Key; secondCount = kv.Value;
            }
        }

        return (best, bestCount - secondCount);
    }

    public void NotifyDecomposed(Creature target, CreatureID decomposerID)
    {
        if (target?.data != null)
        {
            if (!decomposedCounts.ContainsKey(target.data)) decomposedCounts[target.data] = 0;
            decomposedCounts[target.data]++;
            Debug.Log($"[Room {roomID}] decomposed {target.data.name}, count={decomposedCounts[target.data]}, listeners={OnCreatureDecomposed?.GetInvocationList().Length ?? 0}");
        }
        OnCreatureDecomposed?.Invoke(target, decomposerID);
    }

    void Awake()
    {
        if (homeBound == null) homeBound = GetComponent<Collider>();
    }

    public Transform GetSlot(Direction d)
    {
        switch (d)
        {
            case Direction.N: return slotN;
            case Direction.S: return slotS;
            case Direction.E: return slotE;
            case Direction.W: return slotW;
            default: return null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3 c = transform.position;
        Gizmos.DrawWireCube(c, roomSize);
    }
#endif

    void Start()
    {
        roomID = gameObject.name;
        if (RoomManager.Instance != null) RoomManager.Instance.Register(this);
        Player.Instance.roomChanged += ActiveRoom;
        foreach (Creature c in GetComponentsInChildren<Creature>()) RegisterCreature(c);

    }

    private void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p != null) Player.Instance.SetRoom(this);
    }

    //Register
    public void RegisterCreature(Creature c)
    {
        c.currentRoom = this;
        if (creatureList.Contains(c)) return;

        creatureList.Add(c);

        if (c.data.creatureID != CreatureID.Door && c.data.creatureID != CreatureID.Player)
            c.Died += OnCreatureDied;
    }

    public void ActiveRoom(Room room)
    {
        if (room.roomID == this.roomID) isActive = true;
    }

    public void UnregisterCreature(Creature c)
    {
        c.Died -= OnCreatureDied;
        if (creatureList.Contains(c)) creatureList.Remove(c);
    }

    private void OnCreatureDied(Creature c, CreatureID who)
    {
        UnregisterCreature(c);
        c.Died -= OnCreatureDied;
    }
}
