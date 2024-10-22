using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public JoystickMovement joystickMovement;
    public float playerSpeed;
    public float cameraFollowSpeed = 5f; // Controls how smoothly the camera follows
    private Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 joystickVec = joystickMovement.joystickVec;

        rb.velocity = new Vector2(
            joystickVec.x * playerSpeed,
            joystickVec.y * playerSpeed
        );

        if (joystickVec.x != 0 && joystickVec.y != 0) {
            // Calculate the angle in degrees from the joystick vector
            float joystickDegAngle = Mathf.Atan2(joystickVec.y, joystickVec.x) * Mathf.Rad2Deg;

            // Subtract 90 to align with the sprite’s orientation (facing north)
            joystickDegAngle -= 90;

            // Normalize the angle to ensure it’s between 0 and 360 degrees
            joystickDegAngle = (joystickDegAngle + 360) % 360;

            float steeringAngle = ((transform.rotation.eulerAngles.z - joystickDegAngle + 360) % 360) - 180;

            float rotationSpeed = 150f; // Adjust this as needed

            // Rotate only if the angle is beyond the threshold
            if (Mathf.Abs(steeringAngle) > 1.5 && Mathf.Abs(steeringAngle) < 180 - 1.5)
            {
                float direction = Mathf.Sign(steeringAngle); // -1 for left, 1 for right
                transform.Rotate(0, 0, direction * rotationSpeed * Time.deltaTime);
            }
        }

        // Smooth camera follow
        Vector3 targetPosition = new(transform.position.x, transform.position.y, Camera.main.transform.position.z);
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
    }
}
