using UnityEngine;
using System.Collections;

public static class FootUtil
{
    //레이캐스트로 땅 위치 찾기 - 아래로
    public static Vector3 SetTargetGround(Vector3 targetPos, LayerMask ground)
    {
        // 충분히 높은 곳에서 아래로 길게 쏘기
        Vector3 rayOrigin = new Vector3(targetPos.x, targetPos.y + 1000f, targetPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2000f, ground))
        {
            // 히트 지점이 body보다 너무 높이 있으면 = 머리위 ground는 무시 (점프/공중상태 고려해서 약간 여유)
            if (hit.point.y <= targetPos.y + 2f)
                return hit.point;
        }

        // ground 못찾았거나 위쪽에만 있으면 → 다리를 body 높이에 둠 (하늘로 들지 않음)
        return targetPos;
    }

    // 상하좌우 상관없이 가장 가까운 표면을 찾음
    public static Vector3 SetTargetNearest(Vector3 targetPos, LayerMask ground, float searchRadius = 20.0f)
    {
        Collider[] colliders = Physics.OverlapSphere(targetPos, searchRadius, ground);

        if (colliders.Length == 0) return targetPos;

        Vector3 bestPoint = targetPos;
        float minDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            // ClosestPoint: 해당 콜라이더 표면 중 targetPos와 제일 가까운 점을 반환
            Vector3 point = col.ClosestPoint(targetPos);

            float dist = Vector3.Distance(targetPos, point);

            if (dist < minDistance)
            {
                minDistance = dist;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    public static Vector3 GetNormal(Vector3 targetSurfacePos, Vector3 referenceUpPos, LayerMask ground)
    {
        Vector3 dirToSurface = (targetSurfacePos - referenceUpPos).normalized;
        if (dirToSurface == Vector3.zero) dirToSurface = Vector3.down;

        Vector3 rayOrigin = targetSurfacePos - (dirToSurface * 2.0f);
        if (Physics.Raycast(rayOrigin, dirToSurface, out RaycastHit hit, 5.0f, ground))
            return hit.normal;
        return (referenceUpPos - targetSurfacePos).normalized;
    }


    //앞으로 발뻗을 위치 
    public static Vector3 ForwardStride(Vector3 nowPos, Vector3 movingDir, float stepDist)
    {
        float off = Random.Range(0.8f, 1.2f);
        Vector3 stepPos = nowPos + (movingDir * stepDist * off);
        return stepPos;
    }

    //발 떼기 
    public static IEnumerator lerpMove(Transform start, Vector3 targetPos, float stepTime, float stepHeight, Vector3 surfaceNormal)
    {
        Vector3 startPos = start.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / stepTime;

            if (t > 1f) t = 1f;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            float heightCurve = Mathf.Sin(t * Mathf.PI) * stepHeight;
            start.position = currentPos + (surfaceNormal * heightCurve);

            yield return null;
        }

        start.position = targetPos;
    }

    //노말이 없는 경우 발 떼기 
    public static IEnumerator lerpMove(Transform start, Vector3 targetPos, float stepTime, float stepHeight)
    {
        return lerpMove(start, targetPos, stepTime, stepHeight, Vector3.up);
    }
}