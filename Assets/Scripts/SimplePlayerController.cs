using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")] public float moveSpeed = 5f;

    [Header("Mouse Look")] public float mouseSensitivity = 2f;

    private float yaw = 0f; // 左右
    private float pitch = 0f; // 上下

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }


    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // A / D
        float v = Input.GetAxis("Vertical"); // W / S

        Vector3 move =
            transform.forward * v +
            transform.right * h;

        transform.position += move * moveSpeed * Time.deltaTime;
    }
}