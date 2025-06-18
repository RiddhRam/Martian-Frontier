using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform mainCamera;
    public Transform uiCamera;
    public PlayerState playerState;
    public bool stopMoving;

    [SerializeField] private float playerSpeed = 5f;
    private readonly float cameraFollowSpeed = 5f; // Controls how smoothly the camera follows
    private Rigidbody2D rb;
    private float lastRotation; // To track the last rotation angle
    // If the difference between last and current rotation is less than this, we assume it's stuck
    /*[SerializeField]
    private float rotationThreshold;  // should be 0.1*/
    Transform frontWheels;

    // Used in FixedUpdate, but declared here to reduce GC usage
    private Vector3 targetPosition;
    private Vector2 joystickVec;
    private float targetAngle;
    private float currentAngle;
    private float newAngle;
    private float tempLastRotation;

    // Used for steering wheel
    private readonly float maxBodyRotation = 40;
    private readonly float maxChangeRotation = 30;
    private float wheelRotation;

    // Only for recording
    public bool freezeCamera = false;

    // Details above the player
    [SerializeField] private Transform sliderCanvas;
    private readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(HoldCanvasStill());

        stopMoving = false;
        rb = GetComponent<Rigidbody2D>();
        mainCamera.position = new(transform.position.x, transform.position.y, -10);
        uiCamera.position = new(transform.position.x, transform.position.y, -10);
    }

    // 50 times a second
    void FixedUpdate()
    {
        float y = transform.position.y;
        float x = transform.position.x;
        if (y > 24 || y < -515 || x > 79 || x < -79) {
            transform.position = new(0, 10, 0);
        }

        // Leave this before the if statement, that way the camera repositions properly upon restarting the game.
        // Otherwise it gets stuck at the spawn
        // Smooth camera follow
        MoveCamera();

        joystickVec = JoystickMovement.Instance.joystickVec;

        // Make sure vehicle is trying to move
        if (stopMoving || (joystickVec.x == 0 && joystickVec.y == 0)) {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Translation logic
        // Translate the vehicle position
        rb.linearVelocity = new Vector2(
            joystickVec.x * playerSpeed,
            joystickVec.y * playerSpeed
        );

        // Rotation logic
        // Calculate target angle in degrees
        targetAngle = Mathf.Atan2(joystickVec.y, joystickVec.x) * Mathf.Rad2Deg - 90;
        // Normalize the angle to keep it within [0, 360] degrees
        targetAngle = (targetAngle + 360) % 360;

        // Smoothly rotate towards the target angle over time (0.3 second)
        currentAngle = transform.eulerAngles.z;
        newAngle = Mathf.LerpAngle(currentAngle, targetAngle, 8f * Time.deltaTime); // 8f = sharpness, higher is snappier
        
        transform.rotation = Quaternion.Euler(0, 0, newAngle);

        // Save this value in case it's needed for front wheels
        tempLastRotation = lastRotation;
        // Update the last known rotation angle
        lastRotation = newAngle;

        // Front wheels logic
        if (frontWheels) {
            SteerWheel(frontWheels, tempLastRotation, newAngle);
        }
    }

    private void SteerWheel(Transform frontWheels, float tempLastRotation, float newAngle) {

        // Might fail after changing vehicle
        try {
            
            if (tempLastRotation - 90 > newAngle) {
                newAngle += 360;
            }

            if (tempLastRotation < 0) {
                tempLastRotation += 360;
            }

            // newAngle - tempLastRotation is same as rotationDifference, but without Mathf.Abs
            // Wheel rotation cannot exceed maxBodyRotation degrees of the body
            wheelRotation = Mathf.Clamp((newAngle - tempLastRotation) * 10, -maxBodyRotation, maxBodyRotation);

            // Wheel rotation cannot exceed maxChangeRotation degrees of the last frame's rotation
            wheelRotation = Mathf.Clamp(wheelRotation - frontWheels.GetChild(0).rotation.z, -maxChangeRotation, maxChangeRotation);
            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).rotation = Quaternion.Euler(0, 0, wheelRotation + newAngle);
            }
        } catch {
        }
    }

    // Smooth camera follow
    public void MoveCamera()
    {
        if (freezeCamera)
        {
            return;
        }
        targetPosition = new(transform.position.x, transform.position.y, mainCamera.position.z);
        mainCamera.position = Vector3.Lerp(mainCamera.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
        uiCamera.position = mainCamera.position;
    }

    public void SetSpeed(float newSpeed) {
        playerSpeed = newSpeed;

        Transform vehicle = transform.GetChild(0);
        // SetSpeed is called when a new vehicle is placed
        // When a new vehicle is placed we should also check if it needs animated wheels or not
        for (int i = 0; i != vehicle.childCount; i++) {
            if (vehicle.GetChild(i).name == "Front Wheels") {
                frontWheels = vehicle.GetChild(i);
                return;
            }
        }
        frontWheels = null;
    }

    public float GetSpeed() {
        return playerSpeed;
    }

    private string FormatPositionY(int positionY)
    {
        if (positionY <= 0) {
            return "0 M";
        }
        
        if (positionY >= 1_000)
        {
            // Truncate to 3 decimal places and format with "KM"
            return (positionY / 1_000) + " KM";
        } 
        else {
            return positionY + " M";
        }
    }

    private IEnumerator HoldCanvasStill() {
        
        if (sliderCanvas == null) {
            Debug.Log("No canvas found");
            yield break;
        }
        while (true) {

            sliderCanvas.rotation = normalRotation;

            float angle = Mathf.Deg2Rad * transform.eulerAngles.z; // Get the Y-axis rotation

            // Calculate new position based on rotation
            float x = Mathf.Sin(angle) * 5.2f;
            float y = Mathf.Cos(angle) * 5.2f;

            sliderCanvas.localPosition = new Vector3(x, y, 0);

            yield return null;
        }
    }

}