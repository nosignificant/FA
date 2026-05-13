using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Weed3 : MonoBehaviour
{
    public LayerMask ground;

    [Header("Components")]
    public Transform foot;
    public Transform top;
    public Transform[] parts;
    public Transform tipTarget;
    public Transform target;


    [Header("Settings")]
    public float stride = 2f;
    private float offset;
    private float topToTargetDist;

    [Header("Moving")]
    private Vector3 movingDir;
    private bool isMoving = false;
    private Vector3 targetPos;
    private float maxBodyLength;

    [Header("Draw")]

    public LineRender line;
    private bool isDrawArrayInitialized = false;
    private Transform[] drawPoints;


    void Start()
    {
        maxBodyLength = stride * 1.1f;

        line = GetComponent<LineRender>();

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

        MoveFootandClampedTop();

        bodyFABRIK();

        float currentBodyLen = Vector3.Distance(foot.position, top.position);

        // 현재 몸 길이가 보폭을 넘으면 발을 뗌
        if (!isMoving && currentBodyLen > stride)
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

        //움직이는 기준은 항상 타겟과 발 사이 
        Vector3 vectorToTarget = target.position - foot.position;
        //발을 기준으로 최대 거리만큼 clamp
        Vector3 clampedVector = Vector3.ClampMagnitude(vectorToTarget, maxBodyLength);
        //clamp된 위치를 구하고 거기로 top을 lerp
        Vector3 constrainedTopPos = foot.position + clampedVector;
        top.position = Vector3.Lerp(top.position, constrainedTopPos, Time.deltaTime * 5f);
    }

    IEnumerator MoveFoot()
    {
        isMoving = true;

        Vector3 dirToTarget = (target.position - foot.position).normalized;
        //목표 지점은 머리 기준이 아닌 발 기준으로 
        Vector3 destPos = foot.position + (dirToTarget * stride);

        targetPos = FootUtil.SetTargetNearest(destPos, ground);

        //목표가 현재 위치랑 너무 가까우면 return
        if (Vector3.Distance(destPos, targetPos) < 0.01f)
        {
            isMoving = false;
            yield break;
        }

        Vector3 targetNormal = FootUtil.GetNormal(targetPos, top.position, ground);

        float stepTime = 0.2f;
        float stepHeight = 10f;

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
            Vector3 finalDir = Vector3.Lerp(dir, footTotopDir, 0.5f).normalized;

            //이 둘을 lerp한 위치에 lower을 offset만큼 이동시킨다 
            Vector3 finalPos = lower.transform.position + (finalDir * offset);

            current.position = Vector3.Lerp(current.position, finalPos, Time.deltaTime * 20f);

            current.LookAt(lower.transform);
        }
    }
    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;
        if (target != null) tipTarget.position = target.position;

        if (drawPoints == null && parts != null && parts.Length > 0)
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

    public void SetTipTarget(Transform newTarget)
    {
        this.tipTarget = newTarget;
    }

}