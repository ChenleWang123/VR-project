using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float speed = 1f;
    public Gradient randomColorRange = new Gradient();
    public GameObject balloonBody;
    private AudioSource audioSource;

    void Start()
    {
        float randomNum = Random.Range(0f, 1f);
        Color color = randomColorRange.Evaluate(randomNum);
        balloonBody.GetComponent<MeshRenderer>().material.color = color;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        transform.position = transform.position + new Vector3(0, speed * Time.deltaTime, 0f);
    }

    public void Pop()
    {
        speed = 0f;
        GetComponent<Rigidbody>().useGravity = true;

        balloonBody.SetActive(false);

        // 播放音效
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();

        Destroy(gameObject, 2f);
    }
}