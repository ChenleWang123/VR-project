using UnityEngine;

public class PlaySoundWhenMoving : MonoBehaviour
{
    public AudioSource audioSource;
    public float speedThreshold = 0.05f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        if (speed > speedThreshold)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        lastPosition = transform.position;
    }
}
