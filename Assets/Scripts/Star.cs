using UnityEngine;

public class Star : MonoBehaviour
{
    public AudioClip starSound; // star被收集的音效
    public float volume = 1.0f;
    public Vector3 rotationAxis = Vector3.up; // 控制旋转轴  
    public float rotationSpeed = 50f; // 控制旋转速度 
    void Update()
    {
        // 围绕指定的轴旋转  
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }

    public void Collect()
    {
        // 播放音效  
        AudioSource.PlayClipAtPoint(starSound, Camera.main.transform.position, volume);
        Destroy(gameObject);
    }
}
