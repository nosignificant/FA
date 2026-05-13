using UnityEngine;

public class SC_CameraCollision : MonoBehaviour
{
    public Transform referenceTransform;
    public float collisionOffset = 1.0f; // 카메라가 물체를 뚫지 않도록 띄워주는 거리
    public float cameraSpeed = 15f; // 카메라가 제자리로 돌아가는 속도

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

        RaycastHit hit;
        // Raycast를 쏠 방향 (캐릭터 위치 -> 카메라 원래 위치)
        Vector3 dir = parentTransform.TransformPoint(defaultPos) - referenceTransform.position;

        // Raycast의 최대 거리 (원래 카메라 거리)
        float maxDistance = defaultDistance;


        if (Physics.SphereCast(referenceTransform.position, collisionOffset, dir.normalized, out hit, defaultDistance))
        {
            float actualDistance = hit.distance - collisionOffset;

            // 실제 카메라가 위치해야 할 Local Position
            targetLocalPos = directionNormalized * actualDistance;

            if (targetLocalPos.magnitude < 0.1f)
            {
                targetLocalPos = directionNormalized * 0.1f; // 최소 거리 유지
            }
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * cameraSpeed);
    }
}