using UnityEngine;

public class MouseLookRig : MonoBehaviour
{
    [Header("References")]
    public Transform player;   // Player root (Snowman)
    public Transform cam;      // Main Camera transform

    [Header("Mouse")]
    public float sensitivity = 2.0f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Follow")]
    public Vector3 followOffset = new Vector3(0f, 1.6f, 0f); // Rig position relative to player

    private float yaw;
    private float pitch;

    void Start()
    {
        // Lock cursor for mouse-look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;

        pitch = cam != null ? cam.localEulerAngles.x : 0f;
        // Convert to [-180, 180] to avoid jump when angle > 180
        if (pitch > 180f) pitch -= 360f;
    }

    void LateUpdate()
    {
        if (player == null || cam == null) return;

        // Follow player position (after physics)
        transform.position = player.position + followOffset;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Apply yaw on rig, pitch on camera
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}