using UnityEngine;

public class Kabsch2 : MonoBehaviour
{
    [Header("Settings")]
    public int iteration = 9;
    public float rotSmoothSpeed = 5.0f;

    [Header("Targets")]
    public Transform target;

    [Header("Objects")]

    public Transform[] refChild;
    public Transform[] inChild;

    [Header("Debug Visualizer")]
    public GameObject centerPrefab;
    public Transform parent;

    public LineRender line;
    public GameObject centerInstance;
    public KabschSpawner kabschSpawner;

    // 내부 변수들
    private Vector3[] originalInLocalPos;
    [HideInInspector] public Vector3 avgRefPos;
    private Vector3 avgInPos;
    private Vector3[] currentRefPoints;
    private Quaternion currentSmoothedRot = Quaternion.identity;

    private void Awake()
    {
        kabschSpawner = GetComponent<KabschSpawner>();
        line = GetComponent<LineRender>();
        if (centerPrefab != null)
            centerInstance = Instantiate(centerPrefab, parent != null ? parent : transform);
    }

    private void OnEnable()
    {
        if (kabschSpawner != null)
            kabschSpawner.Spawned += OnSpawned;
    }

    private void OnDisable()
    {
        if (kabschSpawner != null)
            kabschSpawner.Spawned -= OnSpawned;
    }

    private void OnSpawned(Transform[] refs, Transform[] ins)
    {
        refChild = refs;
        inChild = ins;

        originalInLocalPos = new Vector3[inChild.Length];
        currentRefPoints = new Vector3[refChild.Length];

        InitOriginalShape();
    }

    private void Start()
    {
        // 인스펙터에서 직접 설정된 경우 spawner 없이 초기화
        if (kabschSpawner == null && refChild != null && refChild.Length > 0
            && inChild != null && inChild.Length > 0)
        {
            originalInLocalPos = new Vector3[inChild.Length];
            currentRefPoints = new Vector3[refChild.Length];
            InitOriginalShape();
        }
    }

    void InitOriginalShape()
    {
        if (inChild.Length == 0) return;

        Vector3 sumIn = Vector3.zero;
        for (int i = 0; i < inChild.Length; i++) sumIn += inChild[i].position;

        avgInPos = sumIn / inChild.Length;
        for (int i = 0; i < inChild.Length; i++) originalInLocalPos[i] = inChild[i].position - avgInPos;
    }

    void Update()
    {
        if (refChild == null || refChild.Length == 0) return;

        CalculateRefCenter();

        if (centerInstance != null) centerInstance.transform.position = avgRefPos;

        // 1. 최적 회전 계산
        Quaternion targetRot = SolveKabsch();

        currentSmoothedRot = Quaternion.Slerp(currentSmoothedRot, targetRot, Time.deltaTime * rotSmoothSpeed);

        ApplyToInChildren(currentSmoothedRot);
    }
    void LateUpdate()
    {
        if (line != null) line.Draw(inChild);
    }


    void CalculateRefCenter()
    {
        Vector3 sumRef = Vector3.zero;
        for (int i = 0; i < refChild.Length; i++)
        {
            currentRefPoints[i] = refChild[i].position;
            sumRef += currentRefPoints[i];
        }
        avgRefPos = sumRef / refChild.Length;
    }

    void ApplyToInChildren(Quaternion rot)
    {
        for (int i = 0; i < inChild.Length; i++)
        {
            Vector3 targetPos = avgRefPos + (rot * originalInLocalPos[i]);
            inChild[i].position = Vector3.Lerp(inChild[i].position, targetPos, Time.deltaTime * 10f);
        }
    }

    Quaternion SolveKabsch()
    {
        if (refChild.Length != inChild.Length) return Quaternion.identity;
        Vector3[] cov = TransposeAndCov(originalInLocalPos, currentRefPoints, Vector3.zero, avgRefPos);
        Quaternion q = Quaternion.identity;
        extractRotation(cov, ref q);
        return q;
    }

    Vector3[] TransposeAndCov(Vector3[] inRel, Vector3[] refAbs, Vector3 inCenterRel, Vector3 refCenterAbs)
    {
        Vector3[] cov = new Vector3[3];
        for (int k = 0; k < inRel.Length; k++)
        {
            Vector3 ptIn = inRel[k];
            Vector3 ptRef = refAbs[k] - refCenterAbs;
            cov[0][0] += ptIn.x * ptRef.x; cov[0][1] += ptIn.x * ptRef.y; cov[0][2] += ptIn.x * ptRef.z;
            cov[1][0] += ptIn.y * ptRef.x; cov[1][1] += ptIn.y * ptRef.y; cov[1][2] += ptIn.y * ptRef.z;
            cov[2][0] += ptIn.z * ptRef.x; cov[2][1] += ptIn.z * ptRef.y; cov[2][2] += ptIn.z * ptRef.z;
        }
        return cov;
    }

    void extractRotation(Vector3[] cov, ref Quaternion q)
    {
        Vector3 omega = new Vector3();
        for (int i = 0; i < iteration; i++)
        {
            Vector3 xDir = q * Vector3.right; Vector3 yDir = q * Vector3.up; Vector3 zDir = q * Vector3.forward;
            Vector3 crossSum = Vector3.Cross(xDir, cov[0]) + Vector3.Cross(yDir, cov[1]) + Vector3.Cross(zDir, cov[2]);
            float dotSum = Mathf.Abs(Vector3.Dot(xDir, cov[0]) + Vector3.Dot(yDir, cov[1]) + Vector3.Dot(zDir, cov[2])) + 1e-6f;
            omega = crossSum / dotSum;
            float w = omega.magnitude;
            if (w < 1e-6f) break;
            q = Quaternion.AngleAxis(w * Mathf.Rad2Deg, omega.normalized) * q;
            q = q.normalized;
        }
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}