using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public Rect cameraBounds;

    private bool zoomingEnabled = true;
    public float zoomOutMin;
    public float zoomOutMax;

    // Used for panning
    private Vector3 touchStart;

    private bool zooming = false;

    private Camera mainCamera;
    // Keep uiCamera in same spot and size as the main camera
    public Camera uiCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        // First, calculate the orthographic size required to fit the bounds' height.
        float orthoSizeY = cameraBounds.size.y / 2f;

        // Then, calculate the orthographic size required to fit the bounds' width, based on the camera's aspect ratio.
        float orthoSizeX = cameraBounds.size.x / (2f * mainCamera.aspect);

        // The true maximum zoom-out level is the *smaller* of these two values.
        // This ensures that both width and height fit within the camera's view.
        // We also take the minimum of this calculated value and the user-defined zoomOutMax.
        zoomOutMax = Mathf.Min(zoomOutMax, orthoSizeY, orthoSizeX);
    }

    // Update is called once per frame
    void Update()
    {

        // Only used for panning, not zooming
        if (Input.GetMouseButtonDown(0) && !zooming)
        {
            // Record mouse start position
            touchStart = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        // If zooming, but now there's only 1 input, then start position will be from where user last released the screen
        // Maybe delete this

        if (zooming && Input.touchCount == 1)
        {
            touchStart = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.touchCount < 2)
        {
            zooming = false;
        }

        // Zoom
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            Zoom(difference * 0.01f);
            zooming = true;
        }
        // Pan
        else if (Input.GetMouseButton(0))
        {
            // Pan the camera in the direction by getting the mouse position and comparing to initiali position
            Vector3 direction = touchStart - mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mainCamera.transform.position += direction;
        }

        // Zoom with mouse scrollwheel
        Zoom(Input.GetAxis("Mouse ScrollWheel"));

        // Keep camera in bounds
        ClampCameraPos();
    }

    private void Zoom(float increment)
    {
        if (!zoomingEnabled)
        {
            return;
        }

        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - increment, zoomOutMin, zoomOutMax);
        uiCamera.orthographicSize = mainCamera.orthographicSize;
    }


    // Disabled when a UI panel is open
    public void ToggleZooming(bool newValue)
    {
        zoomingEnabled = newValue;
    }

    private void ClampCameraPos()
    {
        // First, get the camera's current view dimensions in world units.
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        // Get the camera's current position.
        Vector3 camPos = mainCamera.transform.position;

        // Calculate the min/max coordinates the camera's center can be at.
        // This is derived from the cameraBounds minus half the camera's view size.
        // This ensures the camera's *edges* don't go past the bounds' *edges*.
        float minX = cameraBounds.xMin + camWidth / 2f;
        float maxX = cameraBounds.xMax - camWidth / 2f;
        float minY = cameraBounds.yMin + camHeight / 2f;
        float maxY = cameraBounds.yMax - camHeight / 2f;

        // Clamp the camera's position to these calculated limits.
        float clampedX = Mathf.Clamp(camPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(camPos.y, minY, maxY);

        // Apply the clamped position back to the camera.
        mainCamera.transform.position = new Vector3(clampedX, clampedY, camPos.z);
        uiCamera.transform.position = mainCamera.transform.position;
    }
    
    void OnDrawGizmosSelected()
    {
        if (cameraBounds.size.x > 0 && cameraBounds.size.y > 0)
        {
            Gizmos.color = Color.yellow;
            // The center of the Gizmo cube should be the center of the Rect.
            // The size of the Gizmo cube should be the size of the Rect.
            Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
        }
    }

}
