using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab;
    public int count = 20;
    public Vector2 areaSize = new Vector2(10f, 10f); // XZ 平面生成范围
    public float baseY = 0f;

    void Start()
    {
        if (balloonPrefab == null)
        {
            Debug.LogError("Assign balloonPrefab in Inspector.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
            Vector3 pos = new Vector3(x, baseY, z);
            Instantiate(balloonPrefab, pos, Quaternion.identity, transform);
        }
    }
}