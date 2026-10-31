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
    private bool footTargetLocked;
    private int footTargetVersion;
    private Vector3 lockedFootPosition;
    public bool FootTargetLocked => footTargetLocked;
    [Min(0f)]
    [Tooltip("0이면 제한 없음. 양수면 발 착지점이 다리 상단(top)에서 이 수평 거리보다 멀어지지 않게 제한")]
    public float maxFootReach = 5f;

    public bool isWeed;
    public float weedMaxLength = 5f;
    [Tooltip("isWeed일 때 뻗는 끝(top)이 target에서 이 반경만큼 퍼짐 (leg마다 다른 방향)")]
    public float weedTargetSpread = 2f;
    [Tooltip("퍼진 위치가 살랑거리는 주기(초). 클수록 느리게")]
    public float weedSwayTime = 3f;
    private Vector3 _weedOffset;   // 시간에 따라 흔들리는 이 leg 고유 오프셋

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
        brain = GetComponentInParent<Think2>();
        if (ground.value == 0)
            ground = lh != null && lh.ground.value != 0 ? lh.ground : LayerMask.GetMask("Ground");

        if (isWeed) StartCoroutine(WeedOffsetLoop());   // 오프셋 살랑거림

        //초기 위치
        if (tipTarget != null)
        {
            Vector3 initPos = tipTarget.position;
            if (!isWeed && FootUtil.TryGround(initPos, ground, out Vector3 groundedInitPos))
                initPos = groundedInitPos;   // 걷는 다리는 Ground 레이어의 걸을 수 있는 표면에만 접지

            targetPos = initPos;
            tipTarget.position = targetPos;
            _footHome = targetPos;   // 발 고정 기준점 (여기서 살랑거림)

            if (target != null) top.position = target.position;
        }
    }

    private Vector3 _footHome;
    private Think2 brain;   // wander(반응 대상 없음) 판정용

    // weed 뻗는 끝의 겨냥점:
    //  - 반응(대상 있음): target(proxy)로 뻗음
    //  - wander(대상 없음): 자기 tipTarget(고정 발) + 살랑오프셋 → proxy 이동 무시, 제자리 흔들림
    private Vector3 WeedAim()
    {
        bool wander = brain == null || brain.currentTarget.creature == null;
        return wander ? tipTarget.position + _weedOffset : target.position;
    }

    void Update()
    {
        if (target == null || foot == null || top == null || tipTarget == null) { return; }
        if (parts == null || parts.Length < 2) { Debug.Log("파츠가 없음"); return; }

        if (footTargetLocked)
        {
            tipTarget.position = lockedFootPosition;
            foot.position = lockedFootPosition;
        }

        MoveFootandClampedTop();

        bodyFABRIK();

        if (footTargetLocked) return;

        // LegHead는 바닥보다 높으므로, 보폭 판단은 수평 거리만 사용한다.
        Vector3 toTarget = target.position - foot.position;
        toTarget.y = 0f;
        float distFootToTarget = toTarget.magnitude;
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

    // target 주변 오프셋을 Perlin으로 부드럽게 이동 (leg마다 다른 위상 → 제각기 살랑거림)
    private IEnumerator WeedOffsetLoop()
    {
        float sx = Random.value * 1000f, sz = Random.value * 1000f;
        float speed = 1f / Mathf.Max(0.01f, weedSwayTime);
        while (true)
        {
            float nx = Mathf.PerlinNoise(sx + Time.time * speed, 0f) * 2f - 1f;   // -1~1
            float nz = Mathf.PerlinNoise(0f, sz + Time.time * speed) * 2f - 1f;
            _weedOffset = new Vector3(nx, 0f, nz) * weedTargetSpread;
            yield return null;
        }
    }

    void MoveFootandClampedTop()
    {
        //발은 항상 tipTarget을 따라다닌다. (weed는 tipTarget이 안 움직이니 발 완전 고정)
        foot.position = Vector3.Lerp(foot.position, tipTarget.position, Time.deltaTime * 15f);

        // top(뻗는 끝): weed면 WeedAim() (반응=target / wander=자기 tipTarget+살랑), 아니면 target
        top.position = isWeed ? WeedAim() : target.position;


        Vector3 dir = top.position - foot.position;
        if (dir.magnitude > weedMaxLength)
            top.position = foot.position + dir.normalized * weedMaxLength;

    }

    IEnumerator MoveFoot()
    {
        int stepVersion = footTargetVersion;
        Vector3 dirToTarget = target.position - foot.position;
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude < 0.0001f)
        {
            isMoving = false;
            yield break;
        }
        dirToTarget.Normalize();

        Vector3 destPos = new Vector3(
            foot.position.x + dirToTarget.x * stride + Random.Range(-2f, 2f),
            foot.position.y,
            foot.position.z + dirToTarget.z * stride + Random.Range(-2f, 2f)
        );

        // 땅을 못 찾으면 발을 옮기지 않고 이전 위치 유지 (공중으로 튀는 것 방지)
        if (!FootUtil.TryGround(destPos, ground, out targetPos))
        {
            isMoving = false;
            yield break;
        }

        // NaN/Infinity로 오염되면 발을 몸(top) 밑으로 리셋해 복구 — 안 그러면 NaN이 IK 전체로 퍼짐
        if (!FootUtil.IsFinite(targetPos) || !FootUtil.IsFinite(foot.position))
        {
            Vector3 safe = FootUtil.IsFinite(top.position) ? top.position : Vector3.zero;
            tipTarget.position = safe;
            foot.position = safe;
            targetPos = safe;
            isMoving = false;
            yield break;
        }

        // 목표가 멀어도 발을 무한히 따라 보내지 않고, top 기준 최대 착지 반경 안으로 제한한다.
        if (maxFootReach > 0f)
        {
            Vector3 horizontalOffset = targetPos - top.position;
            horizontalOffset.y = 0f;
            float maxReachSqr = maxFootReach * maxFootReach;

            if (horizontalOffset.sqrMagnitude > maxReachSqr)
            {
                Vector3 limitedProbe = top.position + horizontalOffset.normalized * maxFootReach;
                limitedProbe.y = targetPos.y;

                if (!FootUtil.TryGround(limitedProbe, ground, out targetPos))
                {
                    isMoving = false;
                    yield break;
                }
            }
        }

        if (lh != null)
            if (!lh.CheckValidFootPos(targetPos, this)) { yield break; }

        isMoving = true;

        Vector3 targetNormal = FootUtil.GetNormal(targetPos, top.position, ground);

        float stepTime = 0.2f;
        float stepHeight = 3f;

        Vector3 stepStart = tipTarget.position;
        float t = 0f;
        while (t < 1f)
        {
            // An anchor can be assigned while stepping; stop before writing another airborne point.
            if (stepVersion != footTargetVersion || footTargetLocked)
            {
                isMoving = false;
                yield break;
            }

            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.001f, stepTime));
            tipTarget.position = Vector3.Lerp(stepStart, targetPos, t)
                               + targetNormal * (Mathf.Sin(t * Mathf.PI) * stepHeight);
            yield return null;
        }

        if (stepVersion != footTargetVersion || footTargetLocked)
        {
            isMoving = false;
            yield break;
        }

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

                Vector3 toTarget = (WeedAim() - lower.position).normalized;   // top과 일관 (wander=tipTarget)
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

    public void SetFootAnchor(Vector3 worldPosition)
    {
        footTargetVersion++;
        footTargetLocked = true;
        lockedFootPosition = worldPosition;
        isMoving = false;

        if (tipTarget != null) tipTarget.position = worldPosition;
        if (foot != null) foot.position = worldPosition;
    }

    public void SetFootTargetLock(bool locked)
    {
        footTargetVersion++;
        footTargetLocked = locked;
        if (!locked || tipTarget == null) return;

        lockedFootPosition = tipTarget.position;
        if (foot != null) foot.position = lockedFootPosition;
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
