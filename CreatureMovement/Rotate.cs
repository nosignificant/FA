using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float rotateSpeed;
    public Vector3 rotateAxis;
    public float startOffset = 0f;

    [Header("state")]
    public bool isSelfRotate;

    public bool isTargetRotate;

    [Header("target")]

    public Transform target1; public Transform target2;


    private float timer = 0f;
    void Update()
    {
        if (isSelfRotate)
            SelfRotate();
        else if (isTargetRotate)
            TargetRotate();
    }

    void SelfRotate()
    {
        timer += Time.deltaTime;
        if (timer > startOffset)
            transform.Rotate(rotateAxis * rotateSpeed * Time.deltaTime);
    }

    void TargetRotate()
    {
        Vector3 rotDir = target1.position - target2.position;
        Quaternion targetRotation = Quaternion.LookRotation(rotDir);
        transform.position = target1.position;
        transform.rotation = targetRotation;
    }

    void rotateEuler()
    {
        Vector3 currentRot = transform.eulerAngles;
        currentRot.z += rotateSpeed * Time.deltaTime;
        transform.eulerAngles = currentRot;
    }
    public Quaternion GetRotationDelta(Quaternion previousRot, Quaternion currentRot)
    {
        return currentRot * Quaternion.Inverse(previousRot);
    }
}
