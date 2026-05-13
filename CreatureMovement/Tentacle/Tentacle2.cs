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
    public float uprightForce = 10f;   // 위로 세우려는 힘
    public float jointDistance = 1f;   // 파츠 간 거리
    public float springMultiplier = 3f; // foot쪽이 몇 배 강한지


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
        // 각 파츠에 위로 세우려는 힘
        for (int i = 0; i < partRbs.Length; i++)
        {
            if (partRbs[i] == null || partRbs[i].isKinematic) continue;
            partRbs[i].AddForce(Vector3.up * uprightForce, ForceMode.Force);
        }

        if (hangingRb != null)
            hangingRb.AddForce(Vector3.up * uprightForce, ForceMode.Force);
    }

    void LateUpdate()
    {
        line.DrawCurve(parts);
    }


}
