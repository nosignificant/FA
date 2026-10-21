using UnityEngine;

// 뱀/지렁이 몸통: 머리(head)가 움직이면 각 마디가 '바로 앞 마디'를 spacing 거리로 따라감.
// 물리 없이 LateUpdate에서 위치만 보정 → 가볍고 끊김 없음. head만 다른 로직(이동)으로 움직이면 됨.
public class SnakeFollow : MonoBehaviour
{
    [Tooltip("선두(이동 주체). 이게 움직이면 뒤 마디들이 따라옴")]
    public Transform head;
    [Tooltip("뒤따르는 마디들 — head 바로 뒤부터 순서대로")]
    public Transform[] segments;

    [Header("Head 구동 (TargetControl이 넘겨줌)")]
    [Tooltip("head가 향해 이동할 목표. TargetControl.SetSnakeTarget이 movementTarget을 넣어줌")]
    public Transform target;
    [Tooltip("켜면 head를 target 쪽으로 직접 이동. 끄면 head는 외부 로직이 움직임(꼬리만 담당)")]
    public bool driveHead = true;
    [Tooltip("head 이동 속도")]
    public float headSpeed = 5f;
    [Tooltip("head가 진행 방향으로 도는 부드러움")]
    public float headTurn = 5f;
    [Tooltip("target에 이 거리 이내면 멈춤")]
    public float stopDistance = 1f;
    [Tooltip("목표로 갈 때 좌우로 휘며 헤엄치는 정도(0이면 직선). swayFrequency 속도로 흔들림")]
    public float headSway = 0.3f;
    [Tooltip("헤엄 경로의 상하 휨(0이면 좌우만). lockY면 무시")]
    public float headSwayVertical = 0f;

    [Tooltip("마디 간 유지 거리")]
    public float spacing = 1f;
    [Tooltip("따라오는 부드러움(클수록 바짝, 0이면 즉시 스냅). 지연·관성 느낌")]
    public float follow = 12f;
    [Tooltip("각 마디가 앞 마디를 바라보게 회전")]
    public bool lookAtPrev = true;
    [Tooltip("평면(y 고정) 뱀이면 켬 — 위아래로 안 출렁")]
    public bool lockY = false;

    [Header("유영 흔들림 (fish wiggle)")]
    [Tooltip("켜면 몸을 따라 파도치는 좌우/상하 흔들림")]
    public bool wiggle = true;
    [Tooltip("좌우 흔들림 세기(폭)")]
    public float swayAmplitude = 0.5f;
    [Tooltip("초당 흔들림 횟수")]
    public float swayFrequency = 1.5f;
    [Tooltip("마디당 위상차 — 클수록 몸을 따라 파도가 촘촘히 흐름")]
    public float swayPhasePerSegment = 0.6f;
    [Tooltip("꼬리로 갈수록 흔들림이 커짐(물고기 꼬리처럼)")]
    public bool growToTail = true;
    [Tooltip("상하 흔들림 세기(0이면 좌우만). lockY면 무시")]
    public float verticalAmplitude = 0f;

    void Update()
    {
        // head를 target 쪽으로 헤엄치게 (driveHead off면 외부에서 head를 움직임)
        if (!driveHead || head == null || target == null) return;

        Vector3 to = target.position - head.position;
        float dist = to.magnitude;
        if (dist <= stopDistance || dist < 0.0001f) return;

        Vector3 dir = to / dist;

        // 헤엄치듯 좌우(+상하)로 휘며 전진 — 목표 방향에 수직 사인파를 섞어 직선 대신 곡선 경로
        if (headSway != 0f || headSwayVertical != 0f)
        {
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            side = side.sqrMagnitude < 0.0001f ? Vector3.right : side.normalized;
            float phase = Time.time * swayFrequency * Mathf.PI * 2f;
            dir += side * (Mathf.Sin(phase) * headSway);
            if (!lockY) dir += Vector3.up * (Mathf.Sin(phase + Mathf.PI * 0.5f) * headSwayVertical);
            dir.Normalize();
        }

        head.position += dir * headSpeed * Time.deltaTime;
        head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(dir), Time.deltaTime * headTurn);
    }

    void LateUpdate()
    {
        if (head == null || segments == null) return;

        Transform prev = head;
        for (int i = 0; i < segments.Length; i++)
        {
            Transform seg = segments[i];
            if (seg == null || seg == prev || seg == head) continue;   // head/앞마디와 동일하면 스킵(자기추종 방지)

            // 앞 마디로부터 spacing 거리 '뒤'(현재 방향 유지) 지점을 목표로
            Vector3 dir = seg.position - prev.position;
            if (lockY) dir.y = 0f;
            float m = dir.magnitude;
            if (m < 0.0001f) { prev = seg; continue; }   // 겹침 → 이동 안 함(뒤로 밀리는 드리프트 방지)
            Vector3 target = prev.position + dir / m * spacing;
            if (lockY) target.y = seg.position.y;

            // 유영 흔들림: 진행 방향에 수직으로 사인파(마디마다 위상차 → 몸 따라 파도)
            if (wiggle && swayAmplitude != 0f)
            {
                Vector3 travel = prev.position - seg.position;
                if (lockY) travel.y = 0f;
                if (travel.sqrMagnitude > 0.0001f)
                {
                    Vector3 fwd = travel.normalized;
                    Vector3 side = Vector3.Cross(fwd, Vector3.up);
                    side = side.sqrMagnitude < 0.0001f ? Vector3.right : side.normalized;

                    float tail = growToTail ? (i + 1f) / segments.Length : 1f;   // 꼬리로 갈수록 크게
                    float phase = Time.time * swayFrequency * Mathf.PI * 2f - i * swayPhasePerSegment;

                    target += side * (Mathf.Sin(phase) * swayAmplitude * tail);
                    if (!lockY && verticalAmplitude != 0f)
                        target += Vector3.up * (Mathf.Sin(phase + Mathf.PI * 0.5f) * verticalAmplitude * tail);
                }
            }

            seg.position = follow > 0f
                ? Vector3.Lerp(seg.position, target, Time.deltaTime * follow)
                : target;

            if (lookAtPrev)
            {
                Vector3 look = prev.position - seg.position;
                if (lockY) look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                    seg.rotation = Quaternion.LookRotation(look);
            }

            prev = seg;
        }
    }
}
