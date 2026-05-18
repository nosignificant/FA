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

            //타겟까지 방향
            Vector3 toTargetDir = (target.position - top.position).normalized;
            Vector3 perpDir = Vector3.Cross(toTargetDir, top.right).normalized;
            finalDir = Vector3.Slerp(top.forward, perpDir, bendT).normalized;

            //이 둘을 lerp한 위치에 lower을 offset만큼 이동시킨다
            Vector3 finalPos = lower.transform.position + (finalDir * offset);

            current.position = Vector3.Lerp(current.position, finalPos, Time.deltaTime * 20f);

            current.LookAt(lower.transform);
        }
    }

    //IEnumerators -- 

    private IEnumerator BendCurveLoop(float min, float max, float duration)
    {
        while (true)
        {
            // min → max
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                bendCurve = Mathf.Lerp(min, max, t);
                yield return null;
            }

            // max → min
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                bendCurve = Mathf.Lerp(max, min, t);
                yield return null;
            }
        }
    }

    private IEnumerator BendStrengthLoop(float min, float max, float duration)
    {
        while (true)
        {
            // min → max
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                bendStrength = Mathf.Lerp(min, max, t);
                yield return null;
            }

            // max → min
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                bendStrength = Mathf.Lerp(max, min, t);
                yield return null;
            }
        }
    }
}
