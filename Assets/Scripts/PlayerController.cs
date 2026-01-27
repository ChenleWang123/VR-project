using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float maxSpeed = 6f;
    public float acceleration = 25f;

    [Header("Jump")]
    public float jumpImpulse = 6.5f;
    public float groundCheckDistance = 3f;
    public LayerMask groundMask = ~0; // include every Layers
                                      // https://docs.unity3d.com/6000.2/Documentation/Manual/layermask-introduction.html

    [Header("References")]
    public Transform groundCheckOrigin;     
    public Transform cameraTransform;       

    [Header("Facing")]
    public bool faceMoveDirection = true;
    public float turnSpeedDeg = 360f;

    private Rigidbody rb;
    private bool jumpRequested;

    // initialize before game start
    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // save RigidBody component into rb
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform; // use Main Camera transform as cameraTransform
    }

    // every frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // check if space is pressed
            jumpRequested = true;
    }

    // base on time not frames, 
    void FixedUpdate()
    {
        Move();
        Jump();
        jumpRequested = false;
    }

    void Move()
    {
        // get input
        // moving left and right
        float h = Input.GetAxisRaw("Horizontal"); // from -1.0 (A) to +1.0 (D)
        // moving back and forward
        float v = Input.GetAxisRaw("Vertical");   // from -1.0 (S) to +1.0 (W)

        Vector3 input = new Vector3(h, 0f, v);

        if (input.sqrMagnitude > 1f) 
            input.Normalize(); // normalize lenght to 1, keep the speed constant

        // if no camera
        Vector3 fwd = Vector3.forward; // directon is +Z (world)
        Vector3 right = Vector3.right; // directon is +X (world)

        // have camera
        if (cameraTransform != null)
        {
            fwd = cameraTransform.forward; // where the camera is facing (include y)
            right = cameraTransform.right; // right-hand-side of the camera (include y)

            // project to horizontal plane, prevent looking up/down
            fwd.y = 0f; 
            right.y = 0f;

            // normalize to 1
            fwd.Normalize(); 
            right.Normalize();
        }

        // moving direction
        Vector3 moveDir = (right * input.x + fwd * input.z);
        if (moveDir.sqrMagnitude > 1f) 
            moveDir.Normalize();

        Vector3 desiredVel = moveDir * moveSpeed;
        Vector3 rb_vel = rb.linearVelocity; // linear velocity of rb
        Vector3 hori_vel = new Vector3(rb_vel.x, 0f, rb_vel.z); // horizontal velocity
        Vector3 delta = desiredVel - hori_vel;

        Vector3 accel = Vector3.ClampMagnitude(delta * acceleration, acceleration); // limit length of the vector
                                                                                    // ClampMagnitude: length of delta * acceleration < acceleration
        rb.AddForce(accel, ForceMode.Acceleration); // Add a continuous acceleration to the rigidbody, ignoring its mass
                                                    // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/ForceMode.html

        // set sppeed limit and check if it exceed or not
        Vector3 newHoriz = new Vector3(rb_vel.x, 0f, rb_vel.z);
        if (newHoriz.magnitude > maxSpeed)
        {
            rb_vel = rb.linearVelocity;
            newHoriz = newHoriz.normalized * maxSpeed; // set speed equal to the limit
            rb_vel = new Vector3(newHoriz.x, rb_vel.y, newHoriz.z);
            rb.linearVelocity = rb_vel;
        }

        bool hasMoveInput = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;

        if (faceMoveDirection && hasMoveInput)
        {
            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(
                rb.rotation,
                target,
                turnSpeedDeg * Time.fixedDeltaTime
            );
            rb.MoveRotation(newRot);
        }

    }

    void Jump()
    {
        if (!jumpRequested) return;
        if (!IsGrounded()) return; // jump only when it's on the ground

        Vector3 vel = rb.linearVelocity;

        // vel on y is not negative
        if (vel.y < 0f) 
            vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse); // Impulse: add an instant force impulse to the rigidbody, using its mass
                                                                  // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/ForceMode.html
    }

    bool IsGrounded()
    {
        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position; // use groundCheckOrigin.position if it is assigned
                                                                                              // otherwise transform.position
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore); // don't hit Triggers
                                                                                                                       // Raycast: Casts a ray, from point "origin", in direction "direction", of length "maxDistance", against all colliders in the Scene
                                                                                                                       // groundMask (layerMask): A Layer mask that is used to selectively filter which colliders are considered when casting a ray
                                                                                                                       // Returns true if the ray intersects with a Collider, otherwise false
                                                                                                                       // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Physics.Raycast.html
    }

    // for checking, only when object is selected in edit mode
    // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/MonoBehaviour.OnDrawGizmosSelected.html
    void OnDrawGizmosSelected()
    {
        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance); // drow a line down with groundCheckDistance
    }
}
