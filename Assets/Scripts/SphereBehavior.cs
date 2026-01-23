using UnityEngine;

public class SphereBehavior : MonoBehaviour
{
    public float speed = 1f;
    public Gradient colorGradient;
    public Gradient gradient;  // ✅ 新增字段

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        Material m = r.material;

        // 创建一个最简单的红-蓝渐变
        Gradient gradient = new Gradient();
        gradient.colorKeys = new GradientColorKey[]
        {
        new GradientColorKey(Color.red, 0f),
        new GradientColorKey(Color.blue, 1f)
        };

        // 随机从渐变中取一个颜色
        float t = Random.Range(0f, 1f);
        m.color = gradient.Evaluate(t);
    }

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}
