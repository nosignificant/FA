using UnityEngine;
using System;

public class KabschSpawner : MonoBehaviour
{
    [Header("prefab")]
    public GameObject refPrefab;
    public GameObject inPrefab;

    [Header("component")]
    public TargetControl targetControl;
    public Kabsch2 kabsch2;
    public Transform center;

    [Header("settings")]
    public float refSpawnRadius = 100f;
    public float inSpawnRadius = 100f;
    public int spawnCount = 100;
    public float initSpeed = 2f;
    public bool followInterestTarget = true;

    public event Action<Transform[], Transform[]> Spawned;

    private Transform followTarget;

    private void Awake()
    {
        if (targetControl == null) targetControl = GetComponentInParent<TargetControl>();
        if (kabsch2 == null) kabsch2 = GetComponent<Kabsch2>();
    }

    private void OnEnable()
    {
        if (targetControl != null)
            targetControl.TargetChanged += OnTargetChanged;
    }

    private void OnDisable()
    {
        if (targetControl != null)
            targetControl.TargetChanged -= OnTargetChanged;
    }

    private void Start()
    {
        if (kabsch2 == null) kabsch2 = GetComponent<Kabsch2>();
        if (kabsch2 != null && kabsch2.centerInstance != null)
            center = kabsch2.centerInstance.transform;

        Spawn();

        if (targetControl != null)
            OnTargetChanged(targetControl.movementTarget);
    }

    private void Update()
    {
        if (!followInterestTarget || followTarget == null) return;

        transform.position = Vector3.Lerp(
            transform.position,
            followTarget.position,
            Time.deltaTime * Mathf.Max(0f, initSpeed));
    }

    private void OnTargetChanged(Transform newTarget)
    {
        followTarget = newTarget;

        if (kabsch2 != null)
            kabsch2.SetTarget(newTarget);
    }

    public void Spawn()
    {
        Transform[] refs = new Transform[spawnCount];
        Transform[] ins = new Transform[spawnCount];

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 refRandomPos = UnityEngine.Random.insideUnitSphere * refSpawnRadius;
            Vector3 inRandomPos = UnityEngine.Random.insideUnitSphere * inSpawnRadius;

            GameObject r = Instantiate(refPrefab, refRandomPos, Quaternion.identity);
            GameObject n = Instantiate(inPrefab, inRandomPos, Quaternion.identity);

            refs[i] = r.transform;
            ins[i] = n.transform;

            r.transform.parent = this.transform;
            n.transform.parent = this.transform;
        }

        Spawned?.Invoke(refs, ins);
    }
}
