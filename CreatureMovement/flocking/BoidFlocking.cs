using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Boid))]
public class BoidFlocking : MonoBehaviour
{
    private Boid boid;

    public Transform target;

    [Header("Settings")]
    public float neighborRadius = 5f;
    public float separationRadius = 2.0f;

    public float boundaryRadius = 2.0f;

    [Header("Weights")]
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.0f;
    public float separationWeight = 1.5f;
    public float boundaryWeight = 2.0f;
    public float noiseWeight = 0.5f;
    public float noiseScale = 1.0f;
    public float maxSteerForce = 10.0f;
    private List<Boid> neighbors = new List<Boid>();

    [Header("draw")]
    public LineRender line;
    [Tooltip("곡선 세그먼트 수 (많을수록 매끈)")]
    public int curveResolution = 10;


    void Start()
    {
        boid = GetComponent<Boid>();
        //target = GameObject.Find("Target").transform;
        line = GetComponent<LineRender>();
    }

    void Update()
    {
        FindNeighbors();

        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();
        Vector3 separation = CalculateSeparation();
        Vector3 Arrive = KeepInBounds();
        Vector3 randomMove = CalculateNoise();

        Vector3 steeringForce = (alignment * alignmentWeight) +
                                (cohesion * cohesionWeight) +
                                (separation * separationWeight) +
                                (randomMove * noiseWeight) +
                                (Arrive * boundaryWeight);

        Vector3 clampedSteering = Vector3.ClampMagnitude(steeringForce, maxSteerForce);
        boid.velocity += clampedSteering * Time.deltaTime;

        // center 방향으로 LookAt
        Vector3 center = GetCenter();
        Vector3 lookDir = center - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * boid.rotSpeed);
        }
        // 멀 때 감속하던 로직 제거 — KeepInBounds의 속도 매칭 arrival이 오버슛을 알아서 잡음
    }

    void LateUpdate()
    {
        if (line == null) return;

        Transform[] neighborTransforms = new Transform[neighbors.Count];

        for (int i = 0; i < neighbors.Count; i++)
            neighborTransforms[i] = neighbors[i].transform;

        // 이웃들을 곡선(Catmull-Rom)으로 이음 — 점 2개 미만이면 직선 폴백
        if (neighborTransforms.Length >= 2)
            line.DrawCurve(neighborTransforms, curveResolution);
        else
            line.Draw(neighborTransforms);

    }

    void FindNeighbors()
    {
        neighbors.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, neighborRadius);

        foreach (Collider col in colliders)
        {
            if (col.transform == transform) continue;
            Boid b = col.GetComponent<Boid>();

            if (b == null || b.boidName != boid.boidName) continue;
            neighbors.Add(b);
        }
    }

    private Vector3 GetCenter()
    {
        if (neighbors.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (Boid neighbor in neighbors)
                center += neighbor.transform.position;
            return center / neighbors.Count;
        }
        return target != null ? target.position : transform.position;
    }

    // 군집의 평균 속도 따라감
    private Vector3 CalculateAlignment()
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;

        foreach (Boid neighbor in neighbors)
            averageVelocity += neighbor.velocity;

        averageVelocity /= neighbors.Count;
        //현재 속도 + 필요한 힘 = 목표 속도이므로 , 필요한 힘을 구하려면 목표 속도 - 현재 속도
        return (averageVelocity - boid.velocity).normalized;
    }

    private Vector3 CalculateCohesion()
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (Boid neighbor in neighbors)
            centerOfMass += neighbor.transform.position;

        centerOfMass /= neighbors.Count;

        Vector3 targetDir = centerOfMass - transform.position;
        Vector3 desiredVelocity = targetDir.normalized * boid.maxVelocity;

        return desiredVelocity - boid.velocity;
    }

    //너무 많이 다가가면 멀어지게 함
    private Vector3 CalculateSeparation()
    {
        Vector3 separationForce = Vector3.zero;

        foreach (Boid neighbor in neighbors)
        {
            Vector3 awayDir = transform.position - neighbor.transform.position;
            //방향을 magnitude하면 거리가 나옴
            float dist = awayDir.magnitude;
            //separationRadius 보다 가까이 있으면 밀어내는 힘을 더함
            if (dist < separationRadius && dist > 0)
            {
                if (dist > 0.01f)
                {
                    // 멀어지는 방향 * 1/거리(거리에 반비례해서 힘이 강해짐)
                    separationForce += awayDir.normalized / dist;
                }
            }

        }
        return separationForce;
    }

    // 타겟 추적 (arrival): 멀면 최고속으로 쫓고, boundaryRadius 안에서 선형 감속.
    // "목표 속도 - 현재 속도"를 힘으로 줘서 오버슛하면 스스로 브레이크됨(감쇠 내장).
    private Vector3 KeepInBounds()
    {
        if (target == null) return Vector3.zero;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist < 0.001f) return -boid.velocity;   // 도착 → 멈춤

        // 목표 속도: 멀면 maxVelocity, boundaryRadius 안에서 거리 비례로 감속
        float desiredSpeed = boid.maxVelocity;
        if (dist < boundaryRadius) desiredSpeed *= dist / boundaryRadius;

        Vector3 desiredVel = toTarget / dist * desiredSpeed;
        return desiredVel - boid.velocity;
    }

    private Vector3 CalculateNoise()
    {
        float idOffset = boid.GetInstanceID() * 0.1f;

        float xNoise = Mathf.PerlinNoise(Time.time * noiseScale + idOffset, 0);
        float yNoise = Mathf.PerlinNoise(Time.time * noiseScale + idOffset, 100);
        float zNoise = Mathf.PerlinNoise(Time.time * noiseScale + idOffset, 200);

        Vector3 noiseDir = new Vector3(xNoise - 0.5f, yNoise - 0.5f, zNoise - 0.5f);

        return noiseDir.normalized;
    }
}