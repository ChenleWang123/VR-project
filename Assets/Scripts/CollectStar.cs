using UnityEngine;

public class CollectStar : MonoBehaviour
{
    [SerializeField] private int starNum = 0;
    void OnTriggerEnter(Collider other)
    {
        var star = other.GetComponent<Star>();
        if (star != null)
        {
            star.Collect();
            starNum++;
        }
    }
}
