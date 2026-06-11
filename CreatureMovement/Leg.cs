using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Leg : MonoBehaviour
{
    public LayerMask ground;

    [Header("Components")]
    public Transform foot;
    public Transform top;
    public Transform[] parts;
    public Transform tipTarget;
    public Transform target;
    public LegHead lh;

    [Header("Settings")]
    public float stride = 2f;
    private float offset;
    private float topToTargetDist;

    [Header("Moving")]
    private bool isMoving = false;
    private Vector3 targetPos;
    private float maxBodyLength;

    public bool isWeed;
    public float weedMaxLength = 5f;

    [Header("Draw")]

    public LineRender line;
    public bool drawToTarget;
    private bool isDrawArrayInitialized = false;
    private Transform[] drawPoints;

    [Header("Debug")]
    public bool drawTipTargetGizmo = true;
    public bool drawNextStepGizmo = true;


    void Start()
    {
        maxBodyLength = stride * 0.5f;

        line = GetComponent<LineRender>();
        lh = GetComponentInParent<LegHead>();

        //초기 위치
        if (tipTarget != null)
        {
            Vector3 initPos = tipTarget.position;
            initPos = FootUtil.SetTargetNearest(initPos, ground);

            targetPos = initPos;
            tipTarget.position = targetPos;

            if (target != null) top.position = target.position;
        }
    }

    void Update()
    {
        if (target == null || foot == null || top == null || tipTarget == null) { return; }
        if (parts == null || parts.Length < 2) { Debug.Log("파츠가 없음"); return; }
        MoveFootandClampedTop();

        bodyFABRIK();

        // 타겟이 발끝에서 stride 이상 멀어지면 발을 뗌
        float distFootToTarget = Vector3.Distance(foot.position, target.position);
        if (!isMoving && distFootToTarget > stride && !isWeed)
            StartCoroutine(MoveFoot());

    }

    //선 그리기
    void LateUpdate()
    {
        if (line != null && isDrawArrayInitialized)
            line.Draw(drawPoints);

        else if (line != null)
            line.Draw(parts);

    }

    void MoveFootandClampedTop()
    {
        //발은 항상 tipTarget을 따라다닌다.
        foot.position = Vector3.Lerp(foot.position, tipTarget.position, Time.deltaTime * 15f);

        // top은 target에 바로 붙음
        top.position = target.position;


        Vector3 dir = top.position - foot.position;
        if (dir.magnitude > weedMaxLength)
            top.position = foot.position + dir.normalized * weedMaxLength;

    }

    IEnumerator MoveFoot()
    {
        Vector3 dirToTarget = (target.position - foot.position).normalized;
        Vector3 destPos = foot.position + (dirToTarget * stride);
        destPos = foot.position + new Vector3(
             dirToTarget.x * stride + Random.Range(-2f, 2f),
             foot.position.y + 100f,
             dirToTarget.z * stride + Random.Range(-2f, 2f)
         );

        targetPos = FootUtil.SetTargetGround(destPos, ground);

        if (lh != null)
            if (!lh.CheckValidFootPos(targetPos, this)) { yield break; }

        isMoving = true;

        Vector3 targetNormal = FootUtil.GetNormal(targetPos, top.position, ground);

        float stepTime = 0.2f;
        float stepHeight = 3f;

        yield return StartCoroutine(FootUtil.lerpMove(tipTarget, targetPos, stepTime, stepHeight, targetNormal));

        tipTarget.position = targetPos;

        yield return new WaitForSeconds(0.05f);

        isMoving = false;
    }




    void bodyFABRIK()
    {
        topToTargetDist = Vector3.Distance(top.position, tipTarget.position);
        offset = topToTargetDist / (parts.Length - 1);

        for (int i = parts.Length - 2; i >= 0; i--)
        {
            Transform current = parts[i];
            Transform lower = parts[i + 1];

            //머리 위에서 목적지까지의 방향과 현재 방향을 lerp
            Vector3 footTotopDir = (top.position - tipTarget.position).normalized;
            Vector3 dir = (current.position - lower.position).normalized;
            Vector3 finalDir = Vector3.zero;
            if (isWeed)
            {
                float t = 1f - (float)i / (Mathf.Max(parts.Length - 2, 1));

                float bendT = Mathf.Pow(t, 0.8f) * 0.7f;

                Vector3 toTarget = (target.position - lower.position).normalized;
                finalDir = Vector3.Slerp(Vector3.up, toTarget, bendT).normalized;
            }
            else finalDir = Vector3.Lerp(dir, footTotopDir, 1f).normalized;

            //이 둘을 lerp한 위치에 lower을 offset만큼 이동시킨다 
            Vector3 finalPos = lower.transform.position + (finalDir * offset);

            current.position = Vector3.Lerp(current.position, finalPos, Time.deltaTime * 20f);

            if (isWeed) current.LookAt(lower.transform, Vector3.up);
            else current.LookAt(lower.transform);
        }
    }


    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;

        //따라닐 오브젝트와 다리 연결하기 위한 코드
        if (drawToTarget && drawPoints == null && parts != null && parts.Length > 0)
        {
            drawPoints = new Transform[parts.Length + 1];
            //배열 초기화
            for (int i = 0; i < parts.Length; i++)
                drawPoints[i + 1] = parts[i];
        }

        //0번 인덱스에 inChild 넣음 
        if (drawPoints != null)
        {
            drawPoints[0] = target;
            isDrawArrayInitialized = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawNextStepGizmo && targetPos != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
        }
    }

}