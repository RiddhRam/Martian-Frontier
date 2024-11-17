using System;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    public JoystickMovement joystickMovement;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private float cameraFollowSpeed = 5f; // Controls how smoothly the camera follows
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

            if (joystickVec.x != 0 || joystickVec.y != 0)
            {
                // Calculate target angle in degrees
                float targetAngle = Mathf.Atan2(joystickVec.y, joystickVec.x) * Mathf.Rad2Deg - 90;

                // Normalize the angle to keep it within [0, 360] degrees
                targetAngle = (targetAngle + 360) % 360;

                // Smoothly rotate towards the target angle over time (1 second)
                float currentAngle = transform.eulerAngles.z;
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime / 0.3f);
                
                // This checks if the user is trying to go straight forward or reverse, if neither then rotate
                if (Math.Abs(transform.rotation.eulerAngles.z - newAngle) < 11) {
                    // Apply the new rotation
                    transform.rotation = Quaternion.Euler(0, 0, newAngle);
                } else {
                    transform.rotation = Quaternion.Euler(0, 0, targetAngle);
                }
            }
        }

        // Smooth camera follow
        Vector3 targetPosition = new(transform.position.x, transform.position.y, Camera.main.transform.position.z);
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
    }
}
