using System;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public Boolean zoomingEnabled;
    public float zoomOutMin;
    public float zoomOutMax;

    // Used for panning
    private Vector3 touchStart;

    private bool zooming = false;

    // Restricts camera panning
    private float xLimit = 0.1f;
    private float yLimit = 0.1f;

    void Start() {
        // Adjust zoom based on resolution and aspect ratio

        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update () {
        // If zooming was disabled, do nothing. Usually disabled if a secondary UI element with a scroll view is open
        if (!zoomingEnabled) {
            return;
        }

        // If zooming, but now there's only 1 input, then start position will be from where user last released the screen
        // Maybe delete this

        if (zooming && Input.touchCount == 1) {
            touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.touchCount < 2) {
            zooming = false;
        }

        // Zoom
        if (Input.touchCount == 2) {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero. position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
        
            float difference = currentMagnitude - prevMagnitude;
            Zoom(difference * 0.01f);
            zooming = true;
        } 
        
        // Zoom with mouse scrollwheel
        Zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    private void Zoom(float increment) {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomOutMin, zoomOutMax);
        
        // Calculate dynamic xLimit and yLimit based on screen aspect ratio
        float screenAspect = (float) Screen.width / Screen.height;
        float baseAspect = 9.0f / 16.0f;  // Base aspect ratio (9:16)

        // Formula for absolute values to clamp the camera position
        xLimit = -0.5f * Camera.main.orthographicSize + (4f * baseAspect / screenAspect);
        yLimit = -1 * Camera.main.orthographicSize + (8f * baseAspect / screenAspect);
    }

    public void ToggleZooming() {
        zoomingEnabled = !zoomingEnabled;
    }
}
