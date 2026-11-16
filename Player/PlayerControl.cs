using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10.0f;
    public float jumpForce = 6.5f;
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

    // 커서는 그대로 두고 이동/시점만 막는 게이트 (코덱스 등 오버레이 UI용)
    public static bool inputBlocked = false;

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

        // 고속 이동(달리기 ~44u/s) 시 캡슐이 바닥/지형에 박혀 감속·비껴가는 것 방지.
        //  Continuous = 빠른 이동 중 지형 관통/걸림 줄임, Interpolate = 시각적 떨림 완화.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (Pause.Instance != null && Pause.Instance.IsOpen) return;   // 일시정지 중엔 시점 회전까지 차단

        RotationLogic();   // 조종(canMove=false)·코덱스 중에도 마우스 시점 회전 허용 (락온 중엔 내부에서 LockOn 전담)

        if (!canMove) return;       // 조종 등 → 이동·점프만 차단
        if (inputBlocked) return;   // 이동/점프만 차단

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
        if (canMove && !inputBlocked)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float currentSpeed = moveSpeed;

            // 이동 기준축은 '실제로 보고 있는' 카메라(= 락온이 쓰는 pl.cam)에서 읽는다.
            //  씬 중복/스테일 참조로 cameraTransform이 다른 카메라를 가리키면 W가 엉뚱한 방향으로 감.
            Transform camT = ActiveCam();
            Vector3 camFwd = camT.forward;
            Vector3 camRight = camT.right;
            camFwd.y = 0; camRight.y = 0;
            camFwd.Normalize(); camRight.Normalize();

            Vector3 moveDir = (camFwd * v + camRight * h).normalized;
            if (Input.GetKey(KeyCode.LeftShift)) currentSpeed += moveSpeed * 1.2f;
            Vector3 targetVel = moveDir * currentSpeed;

            targetVel.y = rb.linearVelocity.y;

            rb.linearVelocity = targetVel;
        }
        // canMove=false(빙의 등)면 Rigidbody를 아예 건드리지 않음.
        // (조종 중 플레이어는 proxy 자식이라, 여기서 velocity를 세팅하면 proxy에서 떨어져 나감)
    }

    // 실제 렌더/회전되는 카메라 트랜스폼. 인스펙터의 cameraTransform이 중복·스테일 카메라를
    //  가리키는 경우를 자가 치유: 권위 있는 Player.Instance.pl.cam 으로 맞춘다.
    private bool _camWarned;
    Transform ActiveCam()
    {
        Transform real = (Player.Instance != null && Player.Instance.pl != null && Player.Instance.pl.cam != null)
            ? Player.Instance.pl.cam.transform : null;
        if (real == null) return cameraTransform;   // 아직 준비 안 됨 → 기존 참조 사용

        if (cameraTransform != real)
        {
            if (!_camWarned)
            {
                Debug.LogWarning($"[PlayerControl] 이동 기준 카메라 불일치 발견! " +
                    $"cameraTransform='{(cameraTransform ? cameraTransform.name : "null")}' → 실제 카메라 '{real.name}'로 교정. " +
                    $"(씬에 중복 카메라가 있을 수 있음)", this);
                _camWarned = true;
            }
            cameraTransform = real;   // 교정 — 다음부터는 일치
        }
        return cameraTransform;
    }

    void RotationLogic()
    {
        ActiveCam();   // cameraTransform이 실제 카메라와 어긋나 있으면 여기서 교정 (이동/시점 축 일치)

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
        // 월드 회전 직접 지정 → 조종 중 카메라가 proxy 자식이어도 정상 회전 (yaw+pitch)
        if (cameraTransform != null)
            cameraTransform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
    }


    // 스폰/텔레포트 시 바라보는 방향을 이 yaw(도)로 강제. 내부 rotation을 갱신해야
    // 다음 프레임 RotationLogic이 다시 덮어쓰지 않음 (transform만 돌리면 바로 되돌려짐).
    public void SetLookYaw(float yaw)
    {
        rotation.y = yaw;
        rotation.x = 0f;   // 수평 시점으로 리셋
        transform.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        if (cameraTransform != null)
            cameraTransform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
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