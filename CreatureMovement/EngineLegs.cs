using UnityEngine;
using System.Collections;
public class EngineLegs : MonoBehaviour
{
    [Header("Transform")]
    public Transform followingTarget;
    public Transform body;
    public Transform[] tipTargets;


    [Header("float")]
    public float followTriggerDist = 1f;
    public float stride = 5f;
    public float stepTime = 0.1f;
    public float moveSpeed = 3f;
    public float stepSpeed = 10f;         // 발 이동 속도
    public float stepHeight = 2f;         // 발 들어올리는 높이

    [Header("rotation")]
    public float rotationThreshold = 25f; // 이 각도 넘으면 회전 먼저
    public float miniStepAngle = 30f;     // 한 번에 회전하는 각도

    public float balanceThres = 45f;

    public LayerMask ground;

    [Header("private")]
    private bool isMoving = false;
    private bool isReturning = false;
    private Coroutine moveCoroutine;
    private Transform groundTarget;
    private Vector3[] footLocalDir; // 발의 로컬 방향 벡터
    private Vector3 movingDir;
    private Rigidbody rb;

    void Start()
    {
        rb = body.GetComponent<Rigidbody>();

        footLocalDir = new Vector3[tipTargets.Length];
        for (int i = 0; i < tipTargets.Length; i++)
        {
            Vector3 offset = tipTargets[i].position - body.position;
            offset.y = 0;
            footLocalDir[i] = body.InverseTransformDirection(offset.normalized);
        }
    }

    void Update()
    {
        if (NeedToBalance()) StartCoroutine(LevelBody());
        //방향 
        movingDir = (followingTarget.position - body.position).normalized;

        //기준이 되는 타겟까지의 거리
        float Dist = Vector3.Distance(body.position, followingTarget.position);

        //거리가 followTrigger보다 멀면 
        if (Dist > followTriggerDist)
            Move();
        else
            Stop();


    }
    void Move()
    {
        if (isMoving) return;

        float angle = Vector3.SignedAngle(body.forward, movingDir, Vector3.up);

        if (Mathf.Abs(angle) > rotationThreshold)
            moveCoroutine = StartCoroutine(RotateInSteps());
        else
            moveCoroutine = StartCoroutine(moveForward());
    }

    void Stop()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        isMoving = false;
        if (!isReturning) StartCoroutine(ReturnToStance());
        StartCoroutine(LevelBody());

    }

    IEnumerator ReturnToStance()
    {
        isReturning = true;
        bool allDone = false;

        while (!allDone)
        {
            allDone = true;
            for (int i = 0; i < tipTargets.Length; i++)
            {
                Vector3 target = GetStancePos(i);
                target = FootUtil.SetTargetGround(target, ground);
                tipTargets[i].position = Vector3.MoveTowards(tipTargets[i].position, target, Time.deltaTime * stepSpeed);
                if (Vector3.Distance(tipTargets[i].position, target) > 0.05f)
                    allDone = false;
            }
            yield return null;
        }
        isReturning = false;
    }

    IEnumerator moveForward()
    {
        isMoving = true;
        yield return StartCoroutine(LevelBody());

        for (int i = 0; i < tipTargets.Length; i++)
        {

            // 1. 발 목표 계산 (발 고유 방향 + movingDir 기여만큼 stride)
            Vector3 footDir = body.TransformDirection(footLocalDir[i]);
            Vector3 target = body.position + footDir * stride + movingDir * stride * 0.5f;
            target = FootUtil.SetTargetNearest(target, ground);

            // 2. 발 먼저 빠르게 이동 (완료까지 대기)
            yield return StartCoroutine(FootUtil.lerpMove(tipTargets[i], target, stepTime, stepHeight));
            yield return new WaitForSeconds(stepTime);

            // 3. 발 완료 후 몸통 따라오기
            yield return StartCoroutine(RotBody(movingDir));
            yield return new WaitForSeconds(stepTime);

            yield return StartCoroutine(LevelBody());
        }

        isMoving = false;
    }


    Vector3 GetStancePos(int i)
    {
        Vector3 footDir = body.TransformDirection(footLocalDir[i]);
        return body.position + footDir * stride;
    }

    IEnumerator RotateInSteps()
    {
        isMoving = true;

        float totalAngle = Vector3.SignedAngle(body.forward, movingDir, Vector3.up);
        int stepCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(totalAngle) / miniStepAngle));
        float anglePerStep = totalAngle / stepCount;

        Vector3 originalForward = body.forward;

        for (int step = 0; step < stepCount; step++)
        {
            // 이번 스텝의 목표 방향
            Vector3 stepDir = Quaternion.Euler(0, anglePerStep * (step + 1), 0) * originalForward;

            // 몸통 회전
            float t = 0f;
            Quaternion startRot = body.rotation;
            Quaternion targetRot = Quaternion.LookRotation(stepDir);
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                body.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.Min(t, 1f));
                yield return null;
            }

            // 발 stance 목표 위치 계산
            Vector3[] targets = new Vector3[tipTargets.Length];
            for (int i = 0; i < tipTargets.Length; i++)
            {
                // stepDir 기준으로 임시 회전 적용해서 stance 계산
                Quaternion stepRot = Quaternion.LookRotation(stepDir);
                Vector3 footDir = stepRot * footLocalDir[i];
                targets[i] = body.position + footDir * stride;
                targets[i] = FootUtil.SetTargetGround(targets[i], ground);
            }

            // 짝수 발(k=0) → 홀수 발(k=1) 순으로 재배치
            for (int k = 0; k < 2; k++)
            {
                bool allDone = false;
                while (!allDone)
                {
                    allDone = true;
                    for (int i = k; i < tipTargets.Length; i += 2)
                    {
                        yield return StartCoroutine(FootUtil.lerpMove(tipTargets[i], targets[i], stepTime, stepHeight));

                        if (Vector3.Distance(tipTargets[i].position, targets[i]) > 0.05f)
                            allDone = false;
                    }
                    yield return null;
                }
            }
        }

        isMoving = false;
    }

    IEnumerator RotBody(Vector3 dir)
    {
        float t = 0f;
        Quaternion startRot = body.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        Vector3 startPos = body.position;
        Vector3 movePos = body.position + dir * stride;

        while (t < 1f)
        {
            t += Time.deltaTime;
            body.rotation = Quaternion.Slerp(startRot, targetRot, t * 1.4f);

            Vector3 nextPos = Vector3.Lerp(startPos, movePos, t * 1.4f);
            Vector3 moveDir = (movePos - body.position).normalized;
            moveDir.y = 0;

            if (rb != null)
                rb.AddForce(moveDir * moveSpeed, ForceMode.Force);
            else
                body.position = nextPos;

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator LevelBody()
    {
        // 한 번만 시도 (지속 기울어짐 = outer while 무한루프 방지)
        if (!NeedToBalance()) yield break;

        if (rb != null) rb.angularVelocity = Vector3.zero;

        Quaternion startRot = body.rotation;
        Vector3 flatForward = body.forward;
        flatForward.y = 0;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();
        Quaternion targetRot = Quaternion.LookRotation(flatForward);

        float t = 0f;
        while (t < 1f)
        {
            if (rb != null) rb.angularVelocity = Vector3.zero;
            t += Time.deltaTime * 3f;
            if (rb != null) rb.MoveRotation(Quaternion.Slerp(startRot, targetRot, t));
            yield return new WaitForFixedUpdate();
        }
    }


    bool NeedToBalance()
    {
        Quaternion startRot = body.rotation;
        Vector3 euler = body.rotation.eulerAngles;

        float xDiff = Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f));
        float zDiff = Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f));
        if (xDiff > balanceThres || zDiff > balanceThres) return true;
        else return false;
    }
}

