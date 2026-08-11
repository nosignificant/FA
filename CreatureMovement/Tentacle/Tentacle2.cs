using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Tentacle2 : MonoBehaviour
{
    [Header("Components")]
    public Transform top;
    public Transform[] parts;
    public Rigidbody hangingRb;

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
    }

    void LateUpdate()
    {
        line.DrawCurve(parts);
    }


}
