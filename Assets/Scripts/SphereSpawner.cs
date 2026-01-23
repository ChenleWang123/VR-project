using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.AddComponent<SphereBehavior2>().gradient = new Gradient();
            sphere.GetComponent<SphereBehavior2>().colorGradient = new Gradient();
            sphere.GetComponent<SphereBehavior2>().speed = i;
        }
    }

    void Update()
    {
        // 如果暂时不用，可以留空
    }
}
