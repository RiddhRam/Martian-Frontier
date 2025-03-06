using System;
using TMPro;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    public GameObject mainCamera;
    public JoystickMovement joystickMovement;
    public GameObject speedText;
    public TextMeshProUGUI depthTracker;

    [SerializeField]
    private float playerSpeed = 5f;
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
    private readonly float maxBodyRotation = 30;
    private readonly float maxChangeRotation = 20;
    private float wheelRotation;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera.transform.position = new(transform.position.x, transform.position.y, -10);

        // Disable this if using AI Movement
        if (gameObject.GetComponent<AIMovement>().isActiveAndEnabled) {
            joystickMovement = null;
        }
    }

    // Update is called once per frame
    
    void FixedUpdate()
    {
        // Leave this before the if statement, that way the camera repositions properly upon restarting the game.
        // Otherwise it gets stuck at the spawn
        // Smooth camera follow
        MoveCamera();
        UpdateDepth();

        // Disable this if using AI Movement
        if (!joystickMovement) {
            return;
        }

        joystickVec = joystickMovement.joystickVec;

        // Make sure vehicle is trying to move
        if (joystickVec.x == 0 && joystickVec.y == 0) {
            rb.velocity = Vector2.zero;
            return;
        }

        // Translation logic
        // Translate the vehicle position
        rb.velocity = new Vector2(
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
        newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime / 0.3f);

        // This checks if the user is trying to go straight forward or reverse, if neither then rotate
        if (Math.Abs(transform.rotation.eulerAngles.z - newAngle) < 11) {
            // Apply the new rotation
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        } else {
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }

        // Save this value in case it's needed for front wheels
        tempLastRotation = lastRotation;
        // Update the last known rotation angle
        lastRotation = newAngle;

        // Front wheels logic
        if (!frontWheels) {
            return;
        }

        // Might fail after changing
        try {
            
            if (tempLastRotation - 90 > newAngle) {
                newAngle += 360;
            }

            if (tempLastRotation < 0) {
                tempLastRotation += 360;
            }

            // newAngle - tempLastRotation is same as rotationDifference, but without Mathf.Abs
            // Wheel rotation cannot exceed 30 degrees of the body
            wheelRotation = Mathf.Clamp((newAngle - tempLastRotation) * 20, -maxBodyRotation, maxBodyRotation);

            // Wheel rotation cannot exceed 20 degrees of the last frame's rotation
            wheelRotation = Mathf.Clamp(wheelRotation - frontWheels.GetChild(0).rotation.z, -maxChangeRotation, maxChangeRotation);
            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).rotation = Quaternion.Euler(0, 0, wheelRotation + newAngle);
            }
        } catch {
        }

    }

    // Smooth camera follow
    public void MoveCamera() {
        targetPosition = new(transform.position.x, transform.position.y, mainCamera.transform.position.z);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
    }

    public void SetSpeed(float newSpeed) {
        playerSpeed = newSpeed;

        speedText.GetComponent<TextMeshProUGUI>().text = playerSpeed.ToString();

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

    public void UpdateDepth() {
        depthTracker.text = FormatPositionY((int) -(transform.position.y + 5));
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
        } else {
            return positionY + " M";
        }
    }

}