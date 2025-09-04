using UnityEngine;

public class CustomAdScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    private float rotationSpeed = 200f;

    // Update is called once per frame
    void Update()
    {
        bufferCircle.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
