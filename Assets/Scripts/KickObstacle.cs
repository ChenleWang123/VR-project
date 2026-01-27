using UnityEngine;

public class KickObstacle : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var obstacle = other.GetComponent<Snow>();
        if (obstacle != null)
        {
            Destroy(gameObject);
        }
    }
}
