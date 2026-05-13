using UnityEngine;

public class KabChild : MonoBehaviour
{
    private Kabsch kabManager;

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float stopDistance = 0.5f;

    [Header("Formation Settings")]
    public float formationRadius = 5.0f;
    private Vector3 baseFormationOffset;

    [Header("Idle Settings (New)")]
    public float idleStrength = 2.0f;
    public float idleSpeed = 1.0f;

    [Header("Separation Settings")]
    public float separationDist = 3.0f;
    public float separationWeight = 3.0f;

    [Header("Noise Settings")]
    public float noiseStrength = 0.5f;
    private float noiseOffset;

    void Start()
    {
        kabManager = GetComponentInParent<Kabsch>();
        noiseOffset = Random.Range(0f, 100f);

        Vector2 randomCircle = Random.insideUnitCircle * formationRadius;
        baseFormationOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void Update()
    {
        if (kabManager == null || kabManager.target == null) return;

        MoveToTargetLogic();
        SeparateLogic();
    }

    void MoveToTargetLogic()
    {
        // 1. 제자리 유동(Drift) 계산: 시간(Time)에 따라 천천히 변하는 오프셋
        // Sin, Cos를 써서 둥글둥글하게 움직이게 함 (noiseOffset으로 각자 다르게)
        float driftX = Mathf.Sin(Time.time * idleSpeed + noiseOffset) * idleStrength;
        float driftZ = Mathf.Cos(Time.time * idleSpeed * 0.7f + noiseOffset) * idleStrength; // 0.7f를 곱해 불규칙한 타원 궤도
        Vector3 idleDrift = new Vector3(driftX, 0, driftZ);

        // ★ 최종 목표 = 타겟위치 + 내 원래 자리 + 살랑살랑 움직임(Drift)
        Vector3 finalDestination = kabManager.target.position + baseFormationOffset + idleDrift;

        float dist = Vector3.Distance(transform.position, finalDestination);

        // 2. 방향 구하기
        Vector3 dirToDest = (finalDestination - transform.position).normalized;

        // 3. 이동 노이즈 (가면서 덜덜 떨리는 느낌)
        float noiseX = Mathf.PerlinNoise(Time.time + noiseOffset, 0) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(0, Time.time + noiseOffset) - 0.5f;
        Vector3 noiseDir = new Vector3(noiseX, 0, noiseZ) * noiseStrength;

        // 4. 이동 (stopDistance 체크를 조금 느슨하게 하거나, 계속 움직이게 함)
        // 목표 지점이 계속 도망다니므로(Drift), 큐브도 계속 따라다니게 됨
        if (dist > stopDistance)
        {
            Vector3 moveDir = (dirToDest + noiseDir).normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
    }

    void SeparateLogic()
    {
        // (기존과 동일)
        Transform[] siblings = kabManager.refChild;
        foreach (Transform sibling in siblings)
        {
            if (sibling == transform) continue;
            float dist = Vector3.Distance(transform.position, sibling.position);

            if (dist < separationDist)
            {
                Vector3 pushDir = (transform.position - sibling.position).normalized;
                float force = (separationDist - dist) / separationDist;
                transform.position += pushDir * force * separationWeight * Time.deltaTime;
            }
        }
    }
}