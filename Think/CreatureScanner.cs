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
    private bool _started;
    private Coroutine _scanCo;

    private void Start()
    {
        _started = true;
        RestartScan();
        debugCreatures = new List<Creature>(Results);
    }

    // area를 SetActive(false)로 껐다 켜면 코루틴이 죽는다. Start는 재실행 안 되므로
    // OnEnable에서 ScanRoutine 재가동 (최초 활성화는 Start가 처리).
    private void OnEnable()
    {
        if (_started) RestartScan();
    }

    private void RestartScan()
    {
        if (_scanCo != null) StopCoroutine(_scanCo);
        _scanCo = StartCoroutine(ScanRoutine());
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

        if (self == null || !self.rootTransform) return;

        //근처에 있는 모든 생물 스캔
        Collider[] hits = Physics.OverlapSphere(self.rootTransform.position, scanRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Creature c = hits[i].GetComponentInParent<Creature>();
            if (c == null || c == self || nearby.Contains(c)) continue;
            nearby.Add(c);
        }

        debugCreatures = new List<Creature>(nearby);
    }

    private void OnDrawGizmos()
    {
        if (drawScannerGizmo && self != null && self.rootTransform != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(self.rootTransform.position, scanRadius);
        }

    }

}