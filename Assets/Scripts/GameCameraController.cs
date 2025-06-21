using System.Collections;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public Rect cameraBounds;

    public Transform droneToFollow;

    [Header("Config")]
    private bool zoomingEnabled = true;
    public float zoomOutMin;
    public float zoomOutMax;
    public float zoomSpeed = 0.06f;
    public float zoomScrollSpeed = 3.15f;
    public float zoomSmoothTime = 0.15f;
    private float targetOrthographicSize;

    private Vector3 positionVelocity;
    private float zoomVelocity;

    private Vector3 lastPanPosition;
    private Vector3 targetPosition;
    public float positionSmoothTime = 0.03f;
    
    private bool zooming = false;

    [Header("References")]
    private Camera mainCamera;
    // Keep uiCamera in same spot and size as the main camera
    public Camera uiCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        // Initialize targets to the camera's starting state
        targetPosition = transform.position;
        targetOrthographicSize = mainCamera.orthographicSize;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Handle user input for panning and zooming.
        // These methods will only update the 'target' variables, not move the camera directly.
        if (Input.touchCount >= 2)
        {
            // Pinch gesture handles both panning and zooming simultaneously.
            HandlePinchGesture();
        }

        // Handle zooming with the mouse scroll wheel for desktop/editor convenience.
        HandleMouseScrollZoom();
        // If we are not following a drone, check for manual input.
        if (droneToFollow == null)
        {
            if (Input.GetMouseButton(0))
            {
                // Single finger/mouse drag for panning.
                HandlePanGesture();
            }
        }
        else // --- Drone Following Logic (part of original LateUpdate) ---
        {
            // If a drone is being followed, update the target position.
            // It's often better to do this after the drone's own FixedUpdate has run.
            targetPosition = new Vector3(droneToFollow.position.x, droneToFollow.position.y, transform.position.z);
        }

        // --- Camera Movement Logic (formerly in LateUpdate) ---

        // Before moving, clamp the target position to ensure it's within the defined bounds.
        ClampTargetPosition();

        // Smoothly interpolate the camera's actual position and size towards their targets.
        // In FixedUpdate, this movement is tied to the physics timestep.
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
        mainCamera.orthographicSize = Mathf.SmoothDamp(mainCamera.orthographicSize, targetOrthographicSize, ref zoomVelocity, zoomSmoothTime);
        
        // Ensure the UI camera always matches the main camera's properties.
        SyncUICamera();
    }

    private void HandlePanGesture()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // When the pan begins, record the starting world position.
            lastPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            // Each frame, calculate how far the mouse has moved in world space since the last frame.
            Vector3 currentPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 panDelta = lastPanPosition - currentPanPosition;
            
            // Add this difference to our target position.
            targetPosition += panDelta;
        }
    }

    private void HandlePinchGesture()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        // --- Panning (while pinching) ---
        // Calculate the midpoint of the touches in both the current and previous frame.
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
        Vector2 prevMidpoint = (touchZeroPrevPos + touchOnePrevPos) / 2f;
        Vector2 currentMidpoint = (touchZero.position + touchOne.position) / 2f;

        // Convert these screen midpoints to world positions and find the difference.
        Vector3 panOffset = mainCamera.ScreenToWorldPoint(prevMidpoint) - mainCamera.ScreenToWorldPoint(currentMidpoint);
        targetPosition += panOffset;

        // --- Zooming ---
        // Calculate the distance between fingers in the previous and current frame.
        float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
        float magnitudeDifference = currentMagnitude - prevMagnitude;

        // Apply this difference to the target orthographic size.
        if (zoomingEnabled)
        {
             // Note: Here we directly modify the target size. For even smoother feel, you could lerp this value as well,
             // but smoothing the camera's size in LateUpdate already achieves a great result.
             targetOrthographicSize = Mathf.Clamp(targetOrthographicSize - magnitudeDifference * zoomSpeed, zoomOutMin, zoomOutMax);
        }
    }

    private void HandleMouseScrollZoom()
    {
        if (!zoomingEnabled) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // For scroll wheel, we zoom in on the mouse cursor's position.
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float originalSize = targetOrthographicSize;
            
            // Update the target size
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize - scroll * zoomScrollSpeed, zoomOutMin, zoomOutMax);

            // Calculate how the world point under the mouse shifts due to the zoom, and correct the camera position.
            // This makes the zoom centered on the mouse cursor.
            Vector3 newMouseWorldPos = ScreenToWorldPointAtSize(Input.mousePosition, mainCamera.transform.position, targetOrthographicSize);
            targetPosition += (mouseWorldPos - newMouseWorldPos);
        }
    }
    
    private void ClampTargetPosition()
    {
        // Use the target orthographic size to calculate the camera's view dimensions.
        float camHeight = targetOrthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;
        
        // Calculate the min/max coordinates the camera's center can be at.
        float minX = cameraBounds.xMin + camWidth / 2f;
        float maxX = cameraBounds.xMax - camWidth / 2f;
        float minY = cameraBounds.yMin + camHeight / 2f;
        float maxY = cameraBounds.yMax - camHeight / 2f;

        // Clamp the target position to these limits.
        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);
        
        targetPosition = new Vector3(clampedX, clampedY, targetPosition.z);
    }

    private void SyncUICamera()
    {
        if (uiCamera == null) return;
        uiCamera.transform.position = transform.position;
        uiCamera.orthographicSize = mainCamera.orthographicSize;
    }
    
    // Helper function to calculate a screen point to world point with a custom orthographic size.
    private Vector3 ScreenToWorldPointAtSize(Vector3 screenPosition, Vector3 cameraPosition, float orthoSize)
    {
        // Create a temporary camera to perform the calculation without altering the main camera.
        GameObject tempCamGo = new GameObject("TempCam");
        Camera tempCam = tempCamGo.AddComponent<Camera>();
        tempCam.orthographic = true;
        tempCam.orthographicSize = orthoSize;
        tempCam.transform.position = cameraPosition;

        Vector3 worldPoint = tempCam.ScreenToWorldPoint(screenPosition);
        Destroy(tempCamGo);
        return worldPoint;
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

    /*
    void OnDrawGizmosSelected()
    {
        if (cameraBounds.size.x > 0 && cameraBounds.size.y > 0)
        {
            Gizmos.color = Color.yellow;
            // The center of the Gizmo cube should be the center of the Rect.
            // The size of the Gizmo cube should be the size of the Rect.
            Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
        }
    }*/

    public void SetDroneToFollow(Transform newDroneToFollow)
    {
        droneToFollow = newDroneToFollow;

        StartCoroutine(LerpZoom(25f, 0.5f));
    }

    private IEnumerator LerpZoom(float targetSize, float duration)
    {
        float startValue = targetOrthographicSize;
        float time = 0f;

        while (time < duration)
        {
            targetOrthographicSize = Mathf.Lerp(startValue, targetSize, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        targetOrthographicSize = targetSize;
    }
}
