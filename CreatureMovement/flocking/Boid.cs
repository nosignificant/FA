using UnityEngine;

public class Boid : MonoBehaviour
{
    public Vector3 velocity;
    public float maxVelocity = 2.0f;
    public float rotSpeed = 5f;
    public string boidName = "default";
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (velocity.magnitude > maxVelocity)
            velocity = velocity.normalized * maxVelocity;

        rb.linearVelocity = velocity;
    }
}