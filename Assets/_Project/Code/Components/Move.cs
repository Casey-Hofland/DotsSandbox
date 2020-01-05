using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a script part of testing out the differences between Rigidbodies (PhysX) and Physics Bodies (DOTS Unity Physics implementation).
public class Move : MonoBehaviour
{
    public float moveSpeed = 12f;
    public float maxSpeed = 15f;
    public float jumpForce = 40f;
    public float groundCheckDistance = 0.2f;

    private new Rigidbody rigidbody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Moves the actor
        var target = new Vector3()
        {
            x = transform.position.x - Input.GetAxis("Horizontal")
            , z = transform.position.z - Input.GetAxis("Vertical")
        };

        var normalizedTarget = (target - transform.position).normalized;
        var vel = normalizedTarget * moveSpeed;

        rigidbody.velocity = new Vector3(vel.x, rigidbody.velocity.y, vel.z);

        // Makes the actor Jump
        if(Input.GetButtonDown("Jump") && IsGrounded())
        {
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        return true;    // Method simplification for quick prototyping purposes
    }
}
