using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public JoystickMovement joystickMovement;
    public float playerSpeed;
    private Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (joystickMovement.joystickVec.y != 0) {
            rb.velocity = new Vector2(joystickMovement.joystickVec.x * playerSpeed, joystickMovement.joystickVec.y * playerSpeed);
        } else {
            rb.velocity = Vector2.zero;
        }
    }
}
