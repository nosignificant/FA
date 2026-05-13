
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Weed : MonoBehaviour
{
    // 1. 뿌리는 땅에 박혀 있음
    // 2. 제일 윗부분은 타겟을 향함
    // 3. 타겟에게 붙을 때까지 원으로 회전
    // 4. 아래 부분은 연쇄적으로 위의 움직임을 따라함
    // 5. 타겟이 닿을 만큼 충분히 가까이 오면 제일 윗부부은 타겟에게 붙음
    // 6. 일정 시간 동안 특정 위치에 붙고 떨어지면 다른 랜덤한 위치에 붙음, 더듬는 거처럼 

    public Weedpart[] parts;
    public Weedpart weedHead;
    public Transform target;
    public Weedpart root;


    public float partsOffset = 1f;

    private float dist;
    private Transform weedHeadDefaultPos;
    private Transform rootDefaultPos;

    private Coroutine moveCoroutine;

    void Start()
    {
        rootDefaultPos = root.transform;
        weedHeadDefaultPos = weedHead.transform;
    }

    void Update()
    {
        dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > 5f) Follow();
        else Wander();

    }

    void Follow()
    {
        weedHead.Rotate(root.transform, 1f);
        weedHead.FollowTarget(target.transform.position);
        MoveBodyParts();
    }

    void Wander()
    {
        weedHead.Rotate(root.transform, 1f);
        weedHead.FollowTarget(GetWanderTarget());
        MoveBodyParts();
    }

    Vector3 GetWanderTarget()
    {
        float rootDist = Vector2.Distance(
                new Vector2(weedHead.transform.position.x, weedHead.transform.position.z),
                new Vector2(root.transform.position.x, root.transform.position.z));

        float defaultDist = Vector2.Distance(
                new Vector2(weedHeadDefaultPos.position.x, weedHeadDefaultPos.position.z),
                new Vector2(root.transform.position.x, root.transform.position.z));


        if (rootDist > defaultDist * 3f)
            return weedHeadDefaultPos.position;

        else return target.transform.position;
    }
    void MoveBodyParts()
    {
        Vector3 targetPos = weedHead.transform.position
                - (weedHead.transform.forward * partsOffset);
        parts[0].FollowTarget(targetPos);

        //follow head
        for (int i = 1; i < parts.Length; i++)
        {
            Weedpart previousPart = parts[i - 1];
            targetPos = previousPart.transform.position -
                    (previousPart.transform.forward * partsOffset);
            parts[i].FollowTarget(targetPos);
        }
        root.transform.position = rootDefaultPos.position;

        //뿌리부터
        for (int i = parts.Length - 2; i >= 0; i--)
        {
            Weedpart currentPart = parts[i];      // 나 (위)
            Weedpart lowerPart = parts[i + 1];    // 내 밑 (아래 - 이미 고정됨)

            // [방향 계산]
            // 1단계에서 머리 따라갔던 내 위치(currentPart.position)를 기준으로
            // "밑에서 나를 바라보는 방향"을 계산합니다. (곡선 모양 유지)
            Vector3 dir = (currentPart.transform.position - lowerPart.transform.position).normalized;

            // [꼿꼿함(Stiffness) 추가]
            Vector3 stiffDir = Vector3.up; // 하늘 방향

            // 뿌리에 가까울수록(i가 클수록) 하늘 방향을 더 섞음
            float stiffness = (float)i / (parts.Length - 1);
            stiffness = stiffness * stiffness; // 제곱해주면 위쪽이 더 부드러움

            // 방향 섞기 (원래 가려던 방향 vs 하늘 방향)
            Vector3 finalDir = Vector3.Lerp(dir, stiffDir, stiffness).normalized;

            // [위치 강제 적용]
            // 밑에 놈 위치에서 방향대로 간격만큼 띄운 곳에 나를 '즉시' 배치
            currentPart.transform.position = lowerPart.transform.position + (finalDir * partsOffset);

            // 회전도 맞춰주면 좋음 (나중을 위해)
            currentPart.transform.LookAt(weedHead.transform);
        }
        root.transform.position = rootDefaultPos.position;

    }

}