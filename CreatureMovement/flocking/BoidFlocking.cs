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

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > boundaryRadius)
                boid.velocity = Vector3.Lerp(boid.velocity, Vector3.zero, Time.deltaTime);
        }

    }

    void LateUpdate()
    {
        if (line == null) return;

        Transform[] neighborTransforms = new Transform[neighbors.Count];

        for (int i = 0; i < neighbors.Count; i++)
            neighborTransforms[i] = neighbors[i].transform;

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

    //타겟 따라가게 하기 - 타겟 주변의 boundary를 나가면 중심방향으로, 중심 내에 있으면 감속 
    private Vector3 KeepInBounds()
    {
        if (target == null) return Vector3.zero;

        Vector3 centerOffset = target.position - transform.position;
        float dist = centerOffset.magnitude;

        //거리에 비례해서 중심 방향으로 힘을 줌 (경계 안팎 구분 없이 부드럽게)
        return centerOffset.normalized * (dist / boundaryRadius) * boid.maxVelocity * 0.5f;
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