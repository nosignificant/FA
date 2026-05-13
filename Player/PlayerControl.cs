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

    Rigidbody rb;
    Vector2 rotation = Vector2.zero;
    bool isGrounded = false;
    private static bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

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
        MoveLogicSnappy();
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
            rotation.y = cameraTransform.eulerAngles.y;

            float currentX = cameraTransform.localEulerAngles.x;
            if (currentX > 180) currentX -= 360;
            rotation.x = currentX;
            transform.rotation = Quaternion.Euler(0f, rotation.y, 0f);
            cameraTransform.localRotation = Quaternion.Euler(rotation.x, 0f, 0f);

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