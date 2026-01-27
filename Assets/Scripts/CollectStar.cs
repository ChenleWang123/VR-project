using UnityEngine;

public class CollectStar : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var star = other.GetComponent<Star>();
        if (star != null)
        {
            star.Collect();
        }
    }
}
