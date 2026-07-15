using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10.0f;
    public float jumpForce = 5.0f;
    public LayerMask groundLayer;

    // [추가됨] 중력 배수 설정 (기본 1.0, 2.5 정도면 묵직하게 떨어짐)
    [Header("중력 설정")]
    public float gravityMultiplier = 2.5f;


    [Header("시선 설정")]
    public float lookSpeed = 2.0f;
    public float lookXLimit = 60.0f;
    public Transform cameraTransform;

    [Header("땅 감지")]
    [Tooltip("발밑 판정 구의 중심 오프셋(아래로)")]
    public float groundCheckOffset = 1.0f;
    public float groundCheckRadius = 0.3f;

    Rigidbody rb;
    Vector2 rotation = Vector2.zero;
    bool isGrounded = false;
    private static bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // groundLayer 미설정이면 "Ground" 레이어로 자동 지정
        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Ground");

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        rotation.y = transform.eulerAngles.y;
        if (cameraTransform != null) rotation.x = cameraTransform.localEulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearDamping = 0f;
    }

    void Update()
    {
        if (!canMove) return;

        RotationLogic();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = jumpForce;
            rb.linearVelocity = vel;
        }
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        MoveLogicSnappy();

        // 중력 배수 적용 (기본 중력 외 추가 하강력 → 묵직한 낙하)
        if (gravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
    }

    // 발밑 구 판정으로 접지 여부 갱신
    void UpdateGrounded()
    {
        Vector3 origin = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * groundCheckOffset, groundCheckRadius);
    }

    void MoveLogicSnappy()
    {
        if (canMove)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float currentSpeed = moveSpeed;

            Vector3 camFwd = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camFwd.y = 0; camRight.y = 0;
            camFwd.Normalize(); camRight.Normalize();

            Vector3 moveDir = (camFwd * v + camRight * h).normalized;
            if (Input.GetKey(KeyCode.LeftShift)) currentSpeed += moveSpeed * 1.2f;
            Vector3 targetVel = moveDir * currentSpeed;

            targetVel.y = rb.linearVelocity.y;

            rb.linearVelocity = targetVel;
        }
        else { rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); }
    }

    void RotationLogic()
    {
        if (Player.Instance.isTracking)
        {
            // 락온 중엔 PlayerLockOn이 카메라 회전을 전담.
            // 여기선 언락 대비 값만 동기화하고 회전은 건드리지 않음 (이중 제어로 인한 떨림 방지).
            // yaw·pitch 모두 월드 기준으로 읽어야 언락 시 복원식(Euler(0,y,0)*Euler(x,0,0))과 일치.
            // localEulerAngles.x는 몸 yaw 차이로 pitch가 오염돼서 시점이 튐.
            rotation.y = cameraTransform.eulerAngles.y;

            float currentX = cameraTransform.eulerAngles.x;
            if (currentX > 180) currentX -= 360;
            rotation.x = currentX;

            return;
        }
        rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotation.x = Mathf.Clamp(rotation.x, -lookXLimit, lookXLimit);

        transform.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(rotation.x, 0f, 0f);
    }


    public static void SetPlayerMove(bool onOff)
    {
        canMove = onOff;
        if (onOff == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}