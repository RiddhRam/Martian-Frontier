using System;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    public JoystickMovement joystickMovement;
    private float playerSpeed = 5f;
    [SerializeField]
    private float cameraFollowSpeed = 5f; // Controls how smoothly the camera follows
    private Rigidbody2D rb;
    private float lastRotation; // To track the last rotation angle
    // If the difference between last and current rotation is less than this, we assume it's stuck
    [SerializeField]
    private float rotationThreshold; 
    Transform frontWheels;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 joystickVec = joystickMovement.joystickVec;

        // Translation logic
        // Translate the vehicle position
        rb.velocity = new Vector2(
            joystickVec.x * playerSpeed,
            joystickVec.y * playerSpeed
        );

        // Smooth camera follow
        Vector3 targetPosition = new(transform.position.x, transform.position.y, Camera.main.transform.position.z);
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);

        // Make sure vehicle is trying to rotate
        if (joystickVec.x == 0 && joystickVec.y == 0) {
            return;
        }

        // Rotation logic
        // Calculate target angle in degrees
        float targetAngle = Mathf.Atan2(joystickVec.y, joystickVec.x) * Mathf.Rad2Deg - 90;

        // Normalize the angle to keep it within [0, 360] degrees
        targetAngle = (targetAngle + 360) % 360;

        // Smoothly rotate towards the target angle over time (0.3 second)
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime / 0.3f);

        // This checks if the user is trying to go straight forward or reverse, if neither then rotate
        if (Math.Abs(transform.rotation.eulerAngles.z - newAngle) < 11) {
            // Apply the new rotation
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        } else {
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }

        // Save this value in case it's needed for front wheels
        float tempLastRotation = lastRotation;
        // Update the last known rotation angle
        lastRotation = newAngle;

        // Front wheels logic
        if (!frontWheels) {
            return;
        }

        // Might fail after changing
        try {
            float maxBodyRotation = 30;
            float maxChangeRotation = 20;

            if (tempLastRotation - 90 > newAngle) {
                newAngle += 360;
            }

            if (tempLastRotation < 0) {
                tempLastRotation += 360;
            }

            // newAngle - tempLastRotation is same as rotationDifference, but without Mathf.Abs
            // Wheel rotation cannot exceed 30 degrees of the body
            float wheelRotation = Mathf.Clamp((newAngle - tempLastRotation) * 20, -maxBodyRotation, maxBodyRotation);

            // Wheel rotation cannot exceed 20 degrees of the last frame's rotation
            wheelRotation = Mathf.Clamp(wheelRotation - frontWheels.GetChild(0).rotation.z, -maxChangeRotation, maxChangeRotation);
            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).rotation = Quaternion.Euler(0, 0, wheelRotation + newAngle);
            }
        } catch {
        }

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
}