using UnityEngine;
using System.Collections;

public class Jansen : MonoBehaviour
{
    public JansenLinkage[] foots;
    public Transform target;
    public Transform body;
    public float followThres = 1f;
    public float moveSpeed = 5f;
    public float rotSpeed = 5f;
    [Tooltip("이 각도 이상 기울어지면 수평 보정")]
    public float balanceThreshold = 5f;
    [Tooltip("이동 시작 전 정렬 기준 dot (1=완전정렬, 0=90도)")]
    public float alignDot = 0.95f;
    [Tooltip("이동 중 이 dot 이하로 벌어지면 멈추고 재정렬")]
    public float realignDot = 0.7f;

    [Tooltip("발 사이 크랭크 위상 간격 (도). 360/발수 로 설정하면 균등 분배.")]
    public float phaseOffset = 90f;
    public LayerMask ground;

    private bool isMoving = false;
    private bool isStopping = false;
    private bool isAligning = false;
    public Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InitFoots();
    }

    void InitFoots()
    {
        for (int i = 0; i < foots.Length; i++)
        {
            if (foots[i] == null) continue;
            int index = i;
            StartCoroutine(StartFootDelayed(index, phaseOffset / 360f * (1f / Mathf.Max(foots[index].crankSpeed, 1f)) * index));
        }
    }

    IEnumerator StartFootDelayed(int i, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (foots[i] == null) yield break;
        foots[i].crankAngle = 0f;
        foots[i].moveStart = true;
    }

    void Update()
    {
        if (target == null) return;

        float d = Vector3.Distance(transform.position, target.position);
        bool shouldMove = d > followThres;

        Vector3 groundedTarget = FootUtil.SetTargetGround(target.position, ground);
        Vector3 toTarget = (groundedTarget - transform.position);
        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        float dot = flatDir != Vector3.zero ? Vector3.Dot(transform.forward, flatDir) : 1f;

        if (!shouldMove)
        {
            // 도착
            if (isMoving && !isStopping)
            {
                isStopping = true;
                StartCoroutine(FinishCycleAndStop());
            }
            else if (!isStopping)
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        // 회전 항상 수행
        if (flatDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir);
            Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSpeed);
            if (rb != null) rb.MoveRotation(newRot);
            else transform.rotation = newRot;
        }

        if (!isMoving && !isAligning)
        {
            // 이동 시작 전 정렬 대기
            if (dot >= alignDot)
            {
                // 정렬 완료 → 이동 시작
                isStopping = false;
                isMoving = true;
                SetFootMoving(true);
            }
            else
            {
                isAligning = true;
            }
        }
        else if (isAligning)
        {
            // 정렬 중
            if (dot >= alignDot)
            {
                isAligning = false;
                isStopping = false;
                isMoving = true;
                SetFootMoving(true);
            }
        }
        else if (isMoving && !isStopping)
        {
            // 이동 중 방향 벌어지면 재정렬
            if (dot < realignDot)
            {
                isStopping = true;
                isMoving = false;
                StartCoroutine(FinishCycleAndStop(realign: true));
            }
            else
            {
                // 정상 이동
                Vector3 moveDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                if (rb != null)
                    rb.MovePosition(rb.position + moveDir * moveSpeed * Time.deltaTime);
                else
                    transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
        }
    }

    void SetFootMoving(bool moving) { } // 발은 항상 회전, 사용 안 함

    IEnumerator FinishCycleAndStop(bool realign = false)
    {
        float[] startAngles = new float[foots.Length];
        for (int i = 0; i < foots.Length; i++)
            startAngles[i] = foots[i] != null ? foots[i].crankAngle : 0f;

        yield return new WaitUntil(() =>
        {
            for (int i = 0; i < foots.Length; i++)
            {
                if (foots[i] == null) continue;
                if (foots[i].crankAngle - startAngles[i] < 360f) return false;
            }
            return true;
        });

        SetFootMoving(false);
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (realign)
        {
            // 재정렬 후 다시 이동
            isStopping = false;
            isAligning = true;
        }
        else
        {
            // 완전 정지
            isMoving = false;
            isStopping = false;
            if (body != null && NeedToBalance())
                StartCoroutine(LevelBody());
        }
    }

    bool NeedToBalance()
    {
        float tilt = Quaternion.Angle(body.rotation,
            Quaternion.LookRotation(Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized));
        return tilt > balanceThreshold;
    }

    IEnumerator LevelBody()
    {
        while (NeedToBalance())
        {
            if (rb != null) rb.angularVelocity = Vector3.zero;

            Quaternion startRot = body.rotation;
            Vector3 flatForward = body.forward;
            flatForward.y = 0;
            flatForward.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(flatForward);

            float t = 0f;
            while (t < 1f)
            {
                if (rb != null) rb.angularVelocity = Vector3.zero;
                t += Time.deltaTime * 3f;
                if (rb != null) rb.MoveRotation(Quaternion.Slerp(startRot, targetRot, t));
                else body.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
