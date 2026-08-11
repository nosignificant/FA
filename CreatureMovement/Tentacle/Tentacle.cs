using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Tentacle : MonoBehaviour
{
    public LayerMask ground;

    [Header("Components")]
    public Transform foot;
    public Transform top;
    public Transform[] parts;
    public Transform tipTarget;
    public Transform target;

    [Header("Settings")]
    private float offset;
    private float topToTargetDist;

    [Header("Bend curve settings")]
    public float minBendCurve = 5f;
    public float maxBendCurve = 10f;
    float bendCurve = 5f;
    public float minBendStrength = 0.5f;
    public float maxBendStrength = 0.5f;
    public float bendStrength = 0.5f;

    [Header("Top upright (밑동 세우기)")]
    [Range(0f, 1f)]
    [Tooltip("밑동(top쪽)을 y축(위)으로 세우는 세기. 끝(foot)은 타겟으로 감")]
    public float topUprightStrength = 0.6f;
    [Tooltip("클수록 top 근처에만 세우는 힘이 집중됨")]
    public float topUprightPower = 2f;

    public bool NeedCoroutine = false;

    [Header("Moving")]
    private bool isMoving = false;
    private Vector3 targetPos;


    [Header("Draw")]

    public LineRender line;
    private Transform[] drawPoints;


    void Start()
    {
        line = GetComponent<LineRender>();

        //초기 위치
        if (tipTarget != null)
        {
            Vector3 initPos = tipTarget.position;
            initPos = FootUtil.SetTargetNearest(initPos, ground);

            targetPos = initPos;
            tipTarget.position = targetPos;
        }
        if (NeedCoroutine)
        {
            StartCoroutine(BendCurveLoop(minBendCurve, maxBendCurve, 1f));
            StartCoroutine(BendStrengthLoop(minBendStrength, maxBendStrength, 2f));
        }
    }

    void Update()
    {
        if (target == null || foot == null || top == null || tipTarget == null) { return; }
        if (parts == null || parts.Length < 2) { Debug.Log("파츠가 없음"); return; }
        tipTarget.position = Vector3.Lerp(tipTarget.position, target.position, Time.deltaTime * 15f);

        foot.position = Vector3.Lerp(foot.position, tipTarget.position, Time.deltaTime * 10f);
        bodyFABRIK();
    }

    //선 그리기
    void LateUpdate()
    {
        line.DrawCurve(parts);
    }

    void bodyFABRIK()
    {
        topToTargetDist = Vector3.Distance(top.position, tipTarget.position);
        offset = topToTargetDist / (parts.Length - 1);

        for (int i = parts.Length - 2; i >= 0; i--)
        {
            Transform current = parts[i];
            Transform lower = parts[i + 1];

            Vector3 finalDir = Vector3.zero;

            float t = 1f - (float)i / (Mathf.Max(parts.Length - 2, 1));

            float bendT = Mathf.Pow(t, bendCurve) * bendStrength;

            //타겟까지 방향 — base를 타겟 방향으로 잡아 곧게 뻗게 (top.forward 쓰면 엉뚱한 방향/위로 볼록)
            Vector3 toTargetDir = (target.position - top.position).normalized;
            // bend 축을 '위'가 아니라 수평 옆으로 (Vector3.up 기준) → 위로 볼록해지는 것 방지
            Vector3 perpDir = Vector3.Cross(toTargetDir, Vector3.up).normalized;
            if (perpDir.sqrMagnitude < 0.0001f) perpDir = top.right;   // 타겟이 수직 바로 위/아래일 때 대비
            finalDir = Vector3.Slerp(toTargetDir, perpDir, bendT).normalized;

            // 밑동(top쪽, t≈0)일수록 y축(위)으로 세움. foot쪽(t≈1)은 그대로 타겟 향함.
            float upBias = Mathf.Pow(1f - t, topUprightPower) * topUprightStrength;
            finalDir = Vector3.Slerp(finalDir, Vector3.up, upBias).normalized;

            //이 둘을 lerp한 위치에 lower을 offset만큼 이동시킨다
            Vector3 finalPos = lower.transform.position + (finalDir * offset);

            current.position = Vector3.Lerp(current.position, finalPos, Time.deltaTime * 20f);

            current.LookAt(lower.transform);
        }
    }

    //IEnumerators -- 

    // Perlin 노이즈로 min↔max 사이를 부드럽게 오가게 (촉수마다 다른 위상 → 제각기 유기적)
    private IEnumerator BendCurveLoop(float min, float max, float duration)
    {
        float seed = Random.value * 1000f;   // 촉수별 고유 위상
        float speed = 1f / Mathf.Max(0.01f, duration);
        while (true)
        {
            float n = Mathf.PerlinNoise(seed + Time.time * speed, 0f);   // 0~1 부드러운 노이즈
            bendCurve = Mathf.Lerp(min, max, n);
            yield return null;
        }
    }

    private IEnumerator BendStrengthLoop(float min, float max, float duration)
    {
        float seed = Random.value * 1000f;
        float speed = 1f / Mathf.Max(0.01f, duration);
        while (true)
        {
            float n = Mathf.PerlinNoise(0f, seed + Time.time * speed);   // 다른 축이라 curve와 독립
            bendStrength = Mathf.Lerp(min, max, n);
            yield return null;
        }
    }
}
