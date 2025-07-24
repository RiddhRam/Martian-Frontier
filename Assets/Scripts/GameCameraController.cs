using System.Collections;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    private static GameCameraController _instance;
    public static GameCameraController Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<GameCameraController>();
            }
            return _instance;
        }
    }

    public Rect cameraBounds;

    public Transform droneToFollow;

    [Header("Config")]
    private bool movementEnabled = true;
    public float zoomOutMin; // Sensible default
    public float zoomOutMax; // Sensible default
    public float zoomSpeed = 0.04f; // Adjusted for new pinch logic
    public float zoomScrollSpeed = 5f; // Adjusted for new scroll logic
    public float zoomSmoothTime = 0.15f;
    private float targetOrthographicSize;

    private Vector3 positionVelocity;
    private float zoomVelocity;

    private Vector3 lastMousePanPosition;
    private Vector3 targetPosition;
    const int targetZPos = -10;
    public float positionSmoothTime = 0.03f;

    [Header("References")]
    private Camera mainCamera;
    // Keep uiCamera in same spot and size as the main camera
    public Camera uiCamera;

    [Header("UI")]
    public GameObject instruction;
    public RectTransform refineryUpgradePanelRect;
    public float param1 = 4f;
    public float param2 = 2f;
    public float param3 = 2f;
    public float bottomY;

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 topLeft     = new Vector3(cameraBounds.xMin, cameraBounds.yMax, 0);
        Vector3 topRight    = new Vector3(cameraBounds.xMax, cameraBounds.yMax, 0);
        Vector3 bottomRight = new Vector3(cameraBounds.xMax, cameraBounds.yMin, 0);
        Vector3 bottomLeft  = new Vector3(cameraBounds.xMin, cameraBounds.yMin, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }*/

    void Awake()
    {
        mainCamera = Camera.main;

        // Initialize targets to the camera's starting state
        targetPosition = new(transform.position.x, transform.position.y, targetZPos);
        targetOrthographicSize = mainCamera.orthographicSize;
    }

    void FixedUpdate()
    {
        // If game is loading, don't allow any camera movement
        if (LoadingScreen.Instance.loadedItems < LoadingScreen.Instance.totalItems)
        {
            return;
        }

        // If a drone is being followed, update the target position.
        // This ensures we get the drone's latest position for the frame.
        if (droneToFollow != null)
        {
            // Make the drone be in the lower half of the screen by adding to the cameras y pos.
            // This makes it easier to tap the drone, players finger doesn't need to reach as far
            targetPosition = new Vector3(droneToFollow.position.x, droneToFollow.position.y + 3, targetZPos);

            // Push drone further down, so the refinery upgrade panel can take the center
            /*if (NPCManager.Instance.refineryUpgradePanel.activeSelf)
            {
                float screenCenterY = Screen.height / val;

                // Calculate world-space offset
                float worldUnitsPerPixel = (targetOrthographicSize * 2f) / Screen.height;
                float worldOffsetY = screenCenterY * worldUnitsPerPixel;

                targetPosition.y = droneToFollow.position.y + worldOffsetY;
            }*/
        }

        // Handle user input for panning and zooming.
        // These methods will only update the 'target' variables, not move the camera directly.
        HandleInput();

        // Clamp the target position and size before applying them.
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, zoomOutMin, zoomOutMax);
        ClampTargetPosition();

        // Smoothly interpolate the camera's actual position and size towards their targets.
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
        mainCamera.orthographicSize = Mathf.SmoothDamp(mainCamera.orthographicSize, targetOrthographicSize, ref zoomVelocity, zoomSmoothTime);
        
        // Ensure the UI camera always matches the main camera's properties.
        SyncUICamera();
    }

    private void HandleInput()
    {
        if (!movementEnabled) return;

        if (Input.touchCount >= 2)
        {
            // Pinch gesture handles both panning and zooming simultaneously.
            HandlePinchGesture();
            // Disable instruction
            instruction.SetActive(false);
        }
        else if (Input.touchCount == 1)
        {
            // Single finger for panning.
            HandleSingleTouchPan();
            instruction.SetActive(false);
        }
        // Fallback to mouse input if no touches are detected.
        else if (Input.GetMouseButton(0))
        {
            HandleMousePan();
            instruction.SetActive(false);
        }

        // Handle zooming with the mouse scroll wheel for desktop/editor convenience.
        HandleMouseScrollZoom();
    }
    
    // Handles panning with a single finger, using touch phases for robust control.
    private void HandleSingleTouchPan()
    {
        Touch touch = Input.GetTouch(0);

        // Pan only when the finger is actually moving.
        if (touch.phase == TouchPhase.Moved)
        {

            // Only if not following a drone
            if (droneToFollow != null)
            {
                return;
            }

            // Get the positions of the touch in the current and previous frame.
            Vector3 currentWorldPos = mainCamera.ScreenToWorldPoint(touch.position);
            Vector3 previousWorldPos = mainCamera.ScreenToWorldPoint(touch.position - touch.deltaPosition);

            // Calculate the difference in world space.
            Vector3 panDelta = previousWorldPos - currentWorldPos;
            panDelta.z = targetZPos;

            // Add this difference to our target position.
            targetPosition += panDelta;
        }
    }

    // Handles mouse-based panning for the editor.
    private void HandleMousePan()
    {

        if (Input.GetMouseButtonDown(0))
        {
            // Only if not following a drone
            if (droneToFollow != null)
            {
                return;
            }

            // When the pan begins, record the starting world position.
            lastMousePanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            if (JoystickMovement.Instance.nPCMovement != null)
            {
                JoystickMovement.Instance.joystickRaycastImage.raycastTarget = true;
            }

            // Only if not following a drone
            if (droneToFollow != null)
            {
                return;
            }

            // Each frame, calculate how far the mouse has moved in world space since the last frame.
            Vector3 currentMousePanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 panDelta = lastMousePanPosition - currentMousePanPosition;
            panDelta.z = targetZPos;

            // Add this difference to our target position.
            targetPosition += panDelta;
        }

    }

    // This new logic centers the zoom on the pinch midpoint, preventing wobble.
    private void HandlePinchGesture()
    {
        // These calculations are always needed to determine the pinch distance.
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        // ZOOMING LOGIC
        // This part runs whether we are following a drone or not.

        // Store the camera's size before we apply the zoom this frame.
        float originalOrthographicSize = targetOrthographicSize;

        // Calculate how much the distance between fingers has changed.
        float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
        float magnitudeDifference = currentMagnitude - prevMagnitude;

        // Apply this change to the target zoom level.
        targetOrthographicSize -= magnitudeDifference * zoomSpeed;
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, zoomOutMin, zoomOutMax);

        if (droneToFollow != null)
        {
            return;
        }

        // POSITIONAL LOGIC
        // We ONLY adjust the camera's position if we are NOT following a drone.
        Vector2 currentMidpointScreen = (touchZero.position + touchOne.position) / 2f;

        // Get the world position of the pinch-midpoint using the size BEFORE this frame's zoom.
        Vector3 worldPosBeforeZoom = ScreenToWorldPointAtSize(currentMidpointScreen, targetPosition, originalOrthographicSize);

        // Get the world position of the same screen midpoint using the size AFTER this frame's zoom.
        Vector3 worldPosAfterZoom = ScreenToWorldPointAtSize(currentMidpointScreen, targetPosition, targetOrthographicSize);

        // Apply the difference as a correction. This simultaneously handles panning (by tracking the midpoint)
        // and zoom-to-point anchoring, keeping the gesture stable and intuitive.
        targetPosition += (worldPosBeforeZoom - worldPosAfterZoom);
    }

    private void HandleMouseScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        // ZOOMING LOGIC
        // This part runs whether we are following a drone or not.

        // Get the world position of the mouse cursor before the zoom.
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Update the target size.
        targetOrthographicSize -= scroll * zoomScrollSpeed;
        // Clamp immediately.
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, zoomOutMin, zoomOutMax);

        if (droneToFollow != null)
        {
            return;
        }

        // POSITIONAL LOGIC
        // We ONLY adjust the camera's position if we are NOT following a drone.

        // Get the world position of the mouse cursor after the zoom and correct the camera position.
        Vector3 newMouseWorldPos = ScreenToWorldPointAtSize(Input.mousePosition, targetPosition, targetOrthographicSize);
        targetPosition += (mouseWorldPos - newMouseWorldPos);
    }
    
    private void ClampTargetPosition()
    {
        float camHeight = targetOrthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;
        
        float minX = cameraBounds.xMin + camWidth / 2f;
        float maxX = cameraBounds.xMax - camWidth / 2f;
        float minY = cameraBounds.yMin + camHeight / 2f;
        float maxY = cameraBounds.yMax - camHeight / 2f;

        // If the bounds are smaller than the camera view, the camera will be fixed to the center.
        if (minX > maxX) minX = maxX = cameraBounds.center.x;
        if (minY > maxY) minY = maxY = cameraBounds.center.y;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);
        
        targetPosition = new Vector3(clampedX, clampedY, targetZPos);
    }

    private void SyncUICamera()
    {
        if (uiCamera == null) return;
        uiCamera.transform.position = transform.position;
        uiCamera.orthographicSize = mainCamera.orthographicSize;
    }
    
    // Replaced the inefficient helper that created a new Camera every frame.
    // This version uses math to calculate the world point for a given ortho size.
    private Vector3 ScreenToWorldPointAtSize(Vector3 screenPosition, Vector3 cameraPosition, float orthoSize)
    {
        float aspect = mainCamera.aspect;
        float cameraHeight = orthoSize * 2f;
        float cameraWidth = cameraHeight * aspect;

        // Convert screen coordinates (pixels) to a 0-1 range, then to a -0.5 to 0.5 range.
        float x = (screenPosition.x / Screen.width) - 0.5f;
        float y = (screenPosition.y / Screen.height) - 0.5f;

        // Calculate the world offset from the camera's center.
        float worldX = x * cameraWidth;
        float worldY = y * cameraHeight;
        
        // Return the camera's target position plus the calculated offset.
        return new Vector3(cameraPosition.x + worldX, cameraPosition.y + worldY, cameraPosition.z);
    }

    public void ToggleMovement(bool newValue)
    {
        movementEnabled = newValue;
    }
    
    public void SetDroneToFollow(Transform newDroneToFollow)
    {
        droneToFollow = newDroneToFollow;
        // The LerpZoom coroutine will still work as intended.
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