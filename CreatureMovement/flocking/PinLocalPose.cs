using UnityEngine;

// 이 오브젝트를 Start 때의 '부모 기준 로컬 위치/회전'에 계속 고정한다.
// 플레이어에 붙은 boid가 flocking·노이즈·물리로 떠내려가지 않게 할 때 그 boid에 붙인다.
//  - Rigidbody가 있으면 kinematic으로 만들어 물리 드리프트 차단.
//  - LateUpdate에서 로컬 pose를 매 프레임 되돌려 BoidFlocking의 회전/이동도 덮어씀.
[DisallowMultipleComponent]
public class PinLocalPose : MonoBehaviour
{
    [Tooltip("위치 고정")]
    public bool pinPosition = true;
    [Tooltip("회전 고정")]
    public bool pinRotation = true;

    private Vector3 _localPos;
    private Quaternion _localRot;

    private void Start()
    {
        _localPos = transform.localPosition;
        _localRot = transform.localRotation;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;            // 물리로 안 밀리게
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (pinPosition) transform.localPosition = _localPos;
        if (pinRotation) transform.localRotation = _localRot;
    }
}
