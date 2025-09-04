using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlaceholderScreen : MonoBehaviour
{
    public GameObject bufferCircle;

    private float rotationSpeed = 200f; // Speed of buffer rotation in degrees per second

    void Start()
    {
        SceneManager.LoadSceneAsync("Singleplayer");    
    }

    // Update is called once per frame
    void Update()
    {
        bufferCircle.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
