
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Weed2 : MonoBehaviour
{

    public Weedpart2[] parts;
    public Weedpart2 head; // 머리가 땅에 붙음 
    public Weedpart2 root; // 뿌리가 몸에 붙음 

    [Header("position")]
    public Transform parent;
    private Vector3 targetPos;
    public Transform rootTarget;

    [Header("offsets")]

    public float stepDist = 10.0f;
    private float offset;

    [Header("debug")]

    public GameObject targetPosPrefab;
    private GameObject targetPosInstance;

    private float rootToTargetDist;

    [Header("moving dir")]
    private Vector3 movingDir;
    private bool isMoving = false;

    void Start()
    {
        if (targetPosPrefab != null)
        {
            targetPosInstance = Instantiate(targetPosPrefab);
        }
        targetPos = head.SetTargetGround(Vector3.zero);
        head.transform.position = targetPos;
    }

    void Update()
    {
        //몸통은 타겟을 따라감 
        root.rootFollowTarget(rootTarget);
        //발을 뻗을 방향
        movingDir = (root.transform.position - rootTarget.transform.position).normalized;
        //몸정렬
        bodyFABRIK();

        //타겟을 구했으니 타겟으로 옮길지 말지를 정해야함 
        //머리 - 뿌리 거리, 방향
        rootToTargetDist = Vector3.Distance(root.transform.position, targetPos);

        //x축이나 z축이 일정 거리 이상 멀어지면 새 타겟을 구한다.
        //if (Mathf.Abs(diff.x) > 10 || Mathf.Abs(diff.z) > 10)
        if (rootToTargetDist > 8)
        {

            if (!isMoving)
                StartCoroutine(MoveFoot(whereToGo()));
        }
    }
    void bodyFABRIK()
    {
        if (!isMoving)
        {
            head.transform.position = Vector3.Lerp(head.transform.position, targetPos, Time.deltaTime * 10f);
        }

        offset = rootToTargetDist / (parts.Length - 1);

        for (int i = parts.Length - 2; i > 0; i--)
        {
            Weedpart2 current = parts[i];
            Weedpart2 lower = parts[i + 1];

            //정렬되어야할 방향
            Vector3 HeadToRootDir = (root.transform.position - head.transform.position).normalized;

            //내 바로 아래 놈 위치
            Vector3 dir = (current.transform.position - lower.transform.position).normalized;
            //방향 정렬
            Vector3 finalDir = Vector3.Lerp(dir, HeadToRootDir, 0.5f).normalized;

            //약간 곡선으로 만들기
            float t = (float)i / (parts.Length - 1);

            float curve = Mathf.Sin(t * Mathf.PI);
            Vector3 sagVector = (Vector3.down) + (movingDir);

            //최종 위치
            Vector3 finalPos = lower.transform.position + (finalDir * offset);
            finalPos += sagVector * curve;
            //이동
            current.transform.position = Vector3.Lerp(current.transform.position, finalPos, Time.deltaTime * 20f);
            current.transform.LookAt(lower.transform);
        }
    }

    Vector3 whereToGo()
    {
        float off = Random.Range(0f, 1f);

        Vector3 predictPos = root.transform.position + (movingDir * stepDist * off);
        if (movingDir.magnitude < 0.1f)
            predictPos += Random.insideUnitSphere;
        return predictPos;
    }
    IEnumerator MoveFoot(Vector3 predictPos)
    {
        isMoving = true;

        Vector3 oldPos = head.transform.position;
        Vector3 newPos = head.SetTargetGround(predictPos);
        if (Vector3.Distance(oldPos, newPos) < 1.0f)
        {
            isMoving = false;
            yield break;
        }
        targetPos = newPos;

        Vector3 startPos = head.transform.position;
        float t = 0f;
        float stepTime = 4f;
        float stepHeight = 3f;

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / stepTime;
            //lerp가 뭐지 
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            //발 드는 높이
            float heightCurve = Mathf.Sin(t * Mathf.PI) * stepHeight;

            //발 떼고 이동
            head.transform.position = currentPos + Vector3.up * heightCurve;
            targetPosInstance.transform.position = currentPos + Vector3.up * heightCurve;

            yield return null;
        }
        //발 땅에 갖다 놓음
        head.transform.position = targetPos;
        targetPosInstance.transform.position = targetPos;
        isMoving = false;

        yield return null;

    }

}