using UnityEngine;

public class CCDIK : MonoBehaviour
{
    [Header("CCDIK")]
    public Transform target;
    public Transform tip;
    public CCDIKjoint[] joints;
    public LayerMask ground;

    public bool setTargetMovementTarget = false;

    public TargetControl tc;

    void Awake()
    {
        if (tc == null) tc = GetComponent<TargetControl>();
        if (tc == null) tc = GetComponentInParent<TargetControl>();
    }

    void Start()
    {
        if (tc != null && setTargetMovementTarget) SetTarget(tc.movementTarget);
    }
    void Update()
    {
        foreach (CCDIKjoint joint in joints)
            joint.evalute(tip, target);
    }

    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;
    }

    private void OnEnable()
    {
        if (tc != null && setTargetMovementTarget)
            tc.TargetChanged += SetTarget;
    }

    private void OnDisable()
    {
        if (tc != null)
            tc.TargetChanged -= SetTarget;
    }
}