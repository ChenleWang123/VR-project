using UnityEngine;

public class SphereBehavior2 : MonoBehaviour
{
    public float speed = 1f;
    public Gradient colorGradient;
    public Gradient gradient;  // ✅ 新增字段

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        Material m = r.material;

        Gradient gradient = new Gradient();
        gradient.colorKeys = new GradientColorKey[]
        {
        new GradientColorKey(Color.red, 0f),
        new GradientColorKey(Color.yellow, 0.25f),
        new GradientColorKey(Color.green, 0.5f),
        new GradientColorKey(Color.cyan, 0.75f),
        new GradientColorKey(Color.magenta, 1f)
        };

        float t = Random.Range(0f, 1f);
        m.color = gradient.Evaluate(t);
    }



    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}
