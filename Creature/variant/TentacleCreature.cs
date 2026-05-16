using System;
using System.Collections;
using UnityEngine;
using CreatureTypes;

public class TentacleCreature : Creature
{
    public TentacleGrab2 tentacleGrab;
    public Synthesis synthesis;

    [Header("Synthesize Behavior")]
    public float synthesisCooldown = 13f;
    public bool isSynthesizing = false;

    protected override void Awake()
    {
        base.Awake();
        if (synthesis == null) synthesis = gameObject.AddComponent<Synthesis>();
        if (tentacleGrab == null) tentacleGrab = GetComponentInChildren<TentacleGrab2>();
    }
}