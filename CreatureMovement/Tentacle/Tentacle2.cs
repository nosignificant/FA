using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Tentacle2 : TentacleBase
{
    [Header("Components")]
    public Transform top;
    public Transform[] parts;
    public Rigidbody hangingRb;

    [Header("Grab 호환")]
    [Tooltip("잡은 생물이 붙는 끝단. 비우면 parts[0](foot) 자동")]
    public Transform foot;
    [Tooltip("조준 대상(TentacleGrab2/TargetControl이 설정). 있으면 끝(hangingRb)을 이쪽으로 당겨 뻗음")]
    public Transform target;
    [Tooltip("target 쪽으로 끝을 당기는 힘")]
    public float reachForce = 50f;

    [Header("이동 추종")]
    [Tooltip("촉수 밑동(top)을 이 대상에 매 프레임 딱 붙임(= AA 몸체). 비우면 top 현재 위치 사용. top이 AA를 안 따라오면 여기에 AA 몸체를 할당")]
    public Transform baseAnchor;
    [Tooltip("top(앵커)이 움직인 만큼 파츠를 그대로 실어 창작물을 따라가게 함. 끄면 스프링으로만(느려서 늘어짐)")]
    public bool carryWithTop = true;
    private Vector3 _lastTopPos;
    private bool _carryInit;

    // TentacleBase 공통 인터페이스
    public override Transform Foot => foot;
    public override Transform Target { get => target; set => target = value; }

    [Header("Physics Settings")]
    public float springForce = 50f;
    public float damper = 5f;
    public float uprightForce = 10f;   // 위로 세우려는 힘 (idle 늘어짐 방지용, 낮게 두는 게 좋음)
    public float jointDistance = 1f;   // 파츠 간 거리
    public float springMultiplier = 3f; // foot쪽이 몇 배 강한지
    [Tooltip("파츠를 base→끝 직선 쪽으로 당겨 타겟을 향해 곧게 뻗게 함 (위로 볼록해지는 것 방지). 0이면 끔")]
    public float straightenForce = 30f;


    [Header("Draw")]
    public LineRender line;

    private Rigidbody[] partRbs;

    void Start()
    {
        line = GetComponent<LineRender>();

        // 잡기용 끝단 기본값: foot(parts[0])
        if (foot == null && parts != null && parts.Length > 0) foot = parts[0];

        partRbs = new Rigidbody[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partRbs[i] = parts[i].GetComponent<Rigidbody>();

        // top은 kinematic으로 고정
        Rigidbody topRb = top.GetComponent<Rigidbody>();
        if (topRb != null) topRb.isKinematic = true;

        // 파츠끼리 SpringJoint로 연결 (top → parts[end] → ... → parts[0] → hangingRb)
        // parts[5]=top, parts[0]=foot 순서
        for (int i = parts.Length - 2; i >= 0; i--)
        {
            float t = 1f - (float)i / (parts.Length - 2); // top=0, foot=1
            float spring = Mathf.Lerp(springForce, springForce * springMultiplier, t);

            SpringJoint joint = parts[i].gameObject.AddComponent<SpringJoint>();
            joint.connectedBody = partRbs[i + 1];
            joint.spring = spring;
            joint.damper = damper;
            joint.maxDistance = jointDistance;
            joint.autoConfigureConnectedAnchor = true;
        }

        // foot에 hangingRb 연결
        if (hangingRb != null)
        {
            SpringJoint joint = parts[0].gameObject.AddComponent<SpringJoint>();
            joint.connectedBody = hangingRb;
            joint.spring = springForce;
            joint.damper = damper;
            joint.maxDistance = jointDistance;
        }
    }

    void FixedUpdate()
    {
        // 밑동을 AA 몸체에 강제로 붙임 (top이 AA를 안 따라오는 경우 대비)
        if (baseAnchor != null && top != null) top.position = baseAnchor.position;

        // 창작물(top) 이동분을 파츠에 그대로 실어 따라가게 함 (동적 rb는 부모 이동을 안 따라오므로 수동 carry)
        if (carryWithTop && top != null)
        {
            if (!_carryInit) { _lastTopPos = top.position; _carryInit = true; }
            Vector3 topDelta = top.position - _lastTopPos;
            _lastTopPos = top.position;
            if (topDelta.sqrMagnitude > 0f)
            {
                for (int i = 0; i < partRbs.Length; i++)
                {
                    var rb = partRbs[i];
                    if (rb != null && !rb.isKinematic) rb.position += topDelta;
                }
                if (hangingRb != null && !hangingRb.isKinematic) hangingRb.position += topDelta;
            }
        }

        // base(top) → 끝(hangingRb/foot) 직선. 파츠를 이 선으로 당겨 타겟 방향으로 곧게 뻗게 함.
        Vector3 baseP = top.position;
        Vector3 tipP = hangingRb != null ? hangingRb.position
                     : (parts.Length > 0 ? parts[0].position : baseP);
        Vector3 seg = tipP - baseP;
        float len = seg.magnitude;
        Vector3 dir = len > 0.0001f ? seg / len : Vector3.up;

        for (int i = 0; i < partRbs.Length; i++)
        {
            var rb = partRbs[i];
            if (rb == null || rb.isKinematic) continue;

            // 직선에서 벗어난 만큼 직선 쪽으로 당김 (위로 볼록해지는 것 방지)
            if (straightenForce > 0f)
            {
                Vector3 rel = rb.position - baseP;
                float along = Mathf.Clamp(Vector3.Dot(rel, dir), 0f, len);
                Vector3 onLine = baseP + dir * along;
                rb.AddForce((onLine - rb.position) * straightenForce, ForceMode.Force);
            }

            // 약한 위로 세우기 (idle 시 아래로 늘어짐 방지)
            rb.AddForce(Vector3.up * uprightForce, ForceMode.Force);
        }

        if (hangingRb != null)
            hangingRb.AddForce(Vector3.up * uprightForce, ForceMode.Force);

        // 조준: target이 있으면 끝(hangingRb)을 그쪽으로 당겨 뻗음 (잡기 시 대상에게 닿도록)
        if (target != null && hangingRb != null)
            hangingRb.AddForce((target.position - hangingRb.position) * reachForce, ForceMode.Force);
    }

    void LateUpdate()
    {
        line.DrawCurve(parts);
    }


}
