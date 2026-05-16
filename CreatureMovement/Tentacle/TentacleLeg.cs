using UnityEngine;
using System.Collections;
using CreatureTypes;

public class TentacleLeg : MonoBehaviour
{
    [System.Serializable]
    public struct TentacleSlot
    {
        public Tentacle tentacle;
        public Transform oldTarget;
        public bool isStepping = false;
        public Vector3 targetPos;
    }

    [Header("Components")]

    public LayerMask ground;


    public Transform target;
    public TentacleSlot[] tentacles;
    public LegControl legControl;

    [Header("Settings")]
    public float maxLegLength = 5f;
    private float offset;
    private Vector3 movingDir;

    public float moveThreshold = 5f;

    void Start()
    {
        line = GetComponent<LineRender>();
        legControl = GetComponentInParent<LegControl>();

        initTentacles();
    }

    void Update()
    {
        movingDir = target.position - body.position;
        if (movingDir.magnitude > )
    }

    public void TryStepping()
    {

        // 비어있는 슬롯에 잡기 시도
        for (int i = 0; i < tentacles.Length; i++)
        {
            if (tentacles[i].isStepping) continue;
            Vector3 proxyTargetPos = FootUtil.SetTargetNearest()
            float d = Vector3.Distance(tentacles.oldTarget.position, )
            if (IsTargetedByOtherSlot(i, target)) continue;

            tentacles[i].tentacle.target = target.rootTransform;
            StartCoroutine(GrabAfterDelay(i, target));
            break;
        }
    }




    private void initTentacles()
    {
        Tentacle[] found = GetComponentsInChildren<Tentacle>();
        tentacles = new TentacleSlot[found.Length];
        for (int i = 0; i < found.Length; i++)
        {
            tentacles[i].tentacle = found[i];
            tentacles[i].oldTarget = found[i].target;
            tentacles[i].isStepping = false;
            tentacles[i].targetPos = Vector3.zero;
        }
    }


}
