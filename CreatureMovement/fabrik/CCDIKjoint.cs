using UnityEngine;

public class CCDIKjoint : MonoBehaviour
{
    public Vector3 axis = Vector3.right;
    Vector3 perpendicular; void Start() { perpendicular = axis.Perpendicular(); }
    public bool isTip;

    public float minAngle = -360f;  // 음수 = 반대 방향 허용 범위
    public float maxAngle =  360f;  // 양수 = 정방향 허용 범위

    public void evalute(Transform tip, Transform target)
    {
        //손끝을 목적지로 옮기기
        transform.rotation = isTip ?
        Quaternion.FromToRotation(tip.up, target.forward)
        : Quaternion.FromToRotation
        (tip.position - transform.position, target.position - transform.position)
         * transform.rotation;

        //관절 제한하기
        transform.rotation = Quaternion.FromToRotation(transform.rotation * axis,
        transform.parent.rotation * axis
        ) * transform.rotation;

        // hinge - 부호 있는 각도로 비대칭 제한
        Vector3 parentPerp  = transform.parent.rotation * perpendicular;
        Vector3 currentPerp = transform.rotation * perpendicular;
        Vector3 rotAxis     = transform.parent.rotation * axis;

        // 현재 각도 계산 (음수 = 반대 방향, 양수 = 정방향)
        float currentAngle = Vector3.SignedAngle(parentPerp, currentPerp, rotAxis);
        float clampedAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        // 각도가 범위 밖이면 보정
        if (Mathf.Abs(currentAngle - clampedAngle) > 0.001f)
        {
            Vector3 targetPerp = Quaternion.AngleAxis(clampedAngle, rotAxis) * parentPerp;
            transform.rotation = Quaternion.FromToRotation(currentPerp, targetPerp) * transform.rotation;
        }
    }
}