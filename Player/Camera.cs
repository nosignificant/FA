using UnityEngine;

public class SC_CameraCollision : MonoBehaviour
{
    public Transform referenceTransform;
    public float collisionOffset = 1.0f; // 카메라가 물체를 뚫지 않도록 띄워주는 거리
    public float cameraSpeed = 15f; // 카메라가 제자리로 돌아가는 속도 (멀어질 때만)
    [Tooltip("카메라가 부딪힐 레이어 (벽·바닥 등). 플레이어·생물은 빼는 게 좋음")]
    public LayerMask collisionMask = ~0;

    Vector3 defaultPos;
    Vector3 directionNormalized;
    Transform parentTransform;
    float defaultDistance;

    // Start is called before the first frame update
    void Start()
    {
        defaultPos = transform.localPosition;
        directionNormalized = defaultPos.normalized;
        parentTransform = transform.parent;
        defaultDistance = Vector3.Distance(defaultPos, Vector3.zero);

        //Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // LateUpdate is called after Update
    void LateUpdate()
    {
        // 카메라의 원래 목표 위치 (CharacterController의 Local Space 기준)
        Vector3 targetLocalPos = defaultPos;

        // Raycast를 쏠 방향 (캐릭터 위치 -> 카메라 원래 위치)
        Vector3 dir = parentTransform.TransformPoint(defaultPos) - referenceTransform.position;

        // 벽·바닥에만 부딪히게 마스크·트리거 무시 적용
        if (Physics.SphereCast(referenceTransform.position, collisionOffset, dir.normalized,
                               out RaycastHit hit, defaultDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float actualDistance = Mathf.Max(0.1f, hit.distance - collisionOffset);   // 최소 거리 유지
            targetLocalPos = directionNormalized * actualDistance;
        }

        // 벽에 가려져 '당길 때'는 즉시(뚫림 방지), '원위치로 멀어질 때'만 부드럽게
        bool movingIn = targetLocalPos.sqrMagnitude < transform.localPosition.sqrMagnitude;
        float k = movingIn ? 1f : Time.deltaTime * cameraSpeed;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, k);
    }
}