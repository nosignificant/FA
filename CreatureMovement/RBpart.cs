using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class RBpart : MonoBehaviour
{
    public Transform target;
    public Rigidbody rb;


    public Vector3 moveDir;
    public float moveSpeed;
    public float maxSpeed;
    public float rotSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        TowardTarget();
    }
    void TowardTarget()
    {
        if (target == null || rb == null) return;

        Vector3 targetDir = (target.position - transform.position).normalized;
        if (targetDir.sqrMagnitude <= 0.0001f) return;

        rb.AddForce(targetDir * moveSpeed, ForceMode.Force);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.deltaTime * rotSpeed));
    }
}
