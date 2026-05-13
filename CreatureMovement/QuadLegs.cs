using UnityEngine;
using System.Collections;
public class QuadLegs : MonoBehaviour
{
    [Header("Transform")]
    public Transform followingTarget;
    public Transform body;
    public Transform[] tipTargets;


    [Header("float")]
    public float followTriggerDist = 1f;
    public float stride = 5f;
    public float moveSpeed = 3f;
    public float lateralDist = 1.5f;      // 발-몸통 옆 거리
    public float stepTime = 0.2f;         // 발 이동 시간 (초, 작을수록 빠름)
    public float stepHeight = 2f;         // 발 들어올리는 높이

    public bool doesNeedToRot = true;

    [Header("rotation")]
    public float rotationThreshold = 25f; // 이 각도 넘으면 회전 먼저
    public float miniStepAngle = 30f;     // 한 번에 회전하는 각도
    public float maxLegAngle = 60f;       // 발이 원래 각도에서 벗어날 수 있는 최대 각도

    public float balanceThres = 45f;

    public LayerMask ground;

    [Header("private")]
    private bool isMoving = false;
    private bool isReturning = false;
    private Coroutine moveCoroutine;
    private Transform groundTarget;
    private float[] theta;
    private Vector3 movingDir;
    public Rigidbody rb;

    void Start()
    {
        rb = body.GetComponent<Rigidbody>();

        theta = new float[tipTargets.Length];
        for (int i = 0; i < tipTargets.Length; i++)
        {
            //몸 - 기본 tipTarget사이의 방향 정보
            Vector3 offset = tipTargets[i].position - body.position;

            //몸이 정면이라고 할 때, 정면에서 해당 발 방향까지의 각도 정보
            theta[i] = Vector2.SignedAngle(
                new Vector2(offset.x, offset.z),
                new Vector2(body.forward.x, body.forward.z)
                );
        }
    }

    void Update()
    {
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

        if (doesNeedToRot)
        {
            float angle = Vector3.SignedAngle(body.forward, movingDir, Vector3.up);
            if (Mathf.Abs(angle) > rotationThreshold)
            {
                moveCoroutine = StartCoroutine(RotateInSteps());
                return;
            }
        }

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
                Vector3 target = GetStancePos(i, body.forward);
                target = FootUtil.SetTargetNearest(target, ground);
                tipTargets[i].position = Vector3.MoveTowards(tipTargets[i].position, target, Time.deltaTime * moveSpeed);
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
            // 현재 몸 위치 기준으로 발 목표 계산
            float phi = Vector2.SignedAngle(new Vector2(body.forward.x, body.forward.z), Vector2.up);
            float psi = (theta[i] + phi) * Mathf.Deg2Rad;
            Vector3 footDir = new Vector3(Mathf.Sin(psi), 0, Mathf.Cos(psi));

            //이동방향쪽으로 dot
            float proj = Mathf.Max(0, Vector3.Dot(movingDir.normalized, footDir));
            Vector3 target = body.position + footDir * (lateralDist + stride * proj);
            target = FootUtil.SetTargetNearest(target, ground);

            // 발 이동 시작 (기다리지 않음)
            StartCoroutine(FootUtil.lerpMove(tipTargets[i], target, stepTime, stepHeight));

            // 몸통 이동 완료까지 대기 → 완료 후 다음 발
            yield return StartCoroutine(RotBody(movingDir));
            yield return StartCoroutine(LevelBody());
        }

        isMoving = false;
    }


    // theta 기준 발의 stance 위치 (forward offset 없이 lateral만)
    Vector3 GetStancePos(int i, Vector3 forward)
    {
        // Vector2.up = 월드z축, 몸통의 forward가 월드 기준으로 몇 도 회전한거냐
        float phi = Vector2.SignedAngle(new Vector2(forward.x, forward.z), Vector2.up);

        //몸통 앞(월드 기준) + 발 offset
        float psi = (theta[i] + phi) * Mathf.Deg2Rad;

        //현재 몸 위치 + 발이 지금 위치해야하는 각도
        return body.position + new Vector3(Mathf.Sin(psi), 0, Mathf.Cos(psi)) * lateralDist;
    }

    IEnumerator RotateInSteps()
    {
        isMoving = true;
        float stepSpeed = moveSpeed * 5f;
        float stepHeight = 1f;

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
                targets[i] = GetStancePos(i, stepDir);
                targets[i] = FootUtil.SetTargetNearest(targets[i], ground);
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
                        Vector3 target = targets[i];
                        float planarDist = new Vector2(
                            tipTargets[i].position.x - target.x,
                            tipTargets[i].position.z - target.z).magnitude;

                        Vector3 dest = planarDist > 0.3f
                            ? target + Vector3.up * stepHeight
                            : target;

                        tipTargets[i].position = Vector3.MoveTowards(
                            tipTargets[i].position, dest, Time.deltaTime * stepSpeed);

                        if (Vector3.Distance(tipTargets[i].position, target) > 0.05f)
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

        // 회전 방향도 수평으로 고정 (위/아래 안 바라보게)
        Vector3 flatDir = dir;
        flatDir.y = 0;
        if (flatDir.sqrMagnitude < 0.0001f) flatDir = body.forward;
        Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized);

        Vector3 startPos = body.position;

        // 수평 방향으로만 이동 (Y는 잠금 → 중력에 맡김)
        Vector3 toTarget = followingTarget.position - startPos;
        toTarget.y = 0;
        float moveDist = Mathf.Min(stride / 2f, toTarget.magnitude);
        Vector3 movePos = startPos + (toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero) * moveDist;
        movePos.y = startPos.y;

        while (t < 1f)
        {
            t = Mathf.Min(t + Time.deltaTime * moveSpeed, 1f);
            body.rotation = Quaternion.Slerp(startRot, targetRot, t);
            Vector3 next = Vector3.Lerp(startPos, movePos, t);
            next.y = body.position.y;  // 매 프레임 Y는 현재 값 유지 (중력 적용 그대로)
            body.position = next;
            yield return null;
        }
    }

    IEnumerator LevelBody()
    {
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
            t += Time.deltaTime * 3f;
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);
            if (rb != null)
            {
                rb.angularVelocity = Vector3.zero;
                rb.MoveRotation(rot);
            }
            else
                body.rotation = rot;
            yield return new WaitForFixedUpdate();
        }
    }

    bool NeedToBalance()
    {
        Vector3 euler = body.rotation.eulerAngles;
        float xDiff = Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f));
        float zDiff = Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f));
        return xDiff > balanceThres || zDiff > balanceThres;
    }
}
