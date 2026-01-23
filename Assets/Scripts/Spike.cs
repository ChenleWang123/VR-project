using UnityEngine;

public class Spike : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        var balloon = collision.gameObject.GetComponent<Balloon>();
        if (balloon != null)
        {
            // Debug.Log("collide");
            balloon.Pop();
        }
    }
}