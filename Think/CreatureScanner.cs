using System.Collections.Generic;
using UnityEngine;
using System.Collections;


[DisallowMultipleComponent]
public class CreatureScanner : MonoBehaviour
{
    [Header("Settings")]
    public Creature self;
    public float scanRadius = 15f;
    public float scanInterval = 5f;

    public bool drawScannerGizmo = true;

    private readonly List<Creature> nearby = new List<Creature>(64);
    public IReadOnlyList<Creature> Results => nearby;
    public List<Creature> debugCreatures;



    private void Awake()
    {
        if (self == null) self = GetComponent<Creature>();
    }
    private void Start()
    {
        StartCoroutine(ScanRoutine());
        debugCreatures = new List<Creature>(Results);

    }

    private IEnumerator ScanRoutine()
    {
        var wait = new WaitForSeconds(scanInterval);
        while (true)
        {
            ScanCreature();
            yield return wait;
        }
    }
    private void ScanCreature()
    {
        nearby.Clear();

        if (self == null) { Debug.LogWarning("[Scanner] self is null"); return; }

        var room = self.currentRoom;
        if (room == null) { Debug.LogWarning($"[Scanner] {self.name} currentRoom is null"); return; }

        float sqRadius = scanRadius * scanRadius;
        if (!self.rootTransform) { Debug.LogWarning($"[Scanner] {self.name} rootTransform is null"); return; }
        Transform myPos = self.rootTransform;

        //방 안에 있는 생물만 탐색
        foreach (var c in room.creatureList)
        {
            if (c == null || c == self) continue;
            if ((c.transform.position - myPos.position).sqrMagnitude <= sqRadius)
                nearby.Add(c);
        }

        debugCreatures = new List<Creature>(nearby);
    }

    private void OnDrawGizmos()
    {
        if (drawScannerGizmo)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(self.rootTransform.position, scanRadius);
        }

    }

    public void ForDebugList()
    {
        List<Creature> cs = new();
        foreach (Creature n in nearby) cs.Add(n);
        foreach (Creature n in cs) Debug.Log(n.data.creatureName + cs.Count);
    }
}