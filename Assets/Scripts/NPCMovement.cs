using System;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
public class NPCMovement : MonoBehaviour
{

    [SerializeField]
    private float playerSpeed = 5f;
    private Rigidbody2D rb;
    private float lastRotation; // To track the last rotation angle
    // If the difference between last and current rotation is less than this, we assume it's stuck
    /*[SerializeField]
    private float rotationThreshold;  // should be 0.1*/
    Transform frontWheels;
    public int npcIndex;
    public NPCManager nPCManager;
    public NavMeshAgent agent;
    public int rebirthLevel;

    public bool stopMoving;

    // Used in FixedUpdate, but declared here to reduce GC usage
    private Vector2 joystickVec;
    private float targetAngle;
    private float currentAngle;
    private float newAngle;
    private float tempLastRotation;
    private readonly float maxBodyRotation = 30;
    private readonly float maxChangeRotation = 20;
    private float wheelRotation;
    Vector2 direction;
    System.Random random = new();

    // Start is called before the first frame update
    void Start()
    {
        agent.updateUpAxis = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (Mathf.Approximately(agent.destination.y, -8f)) {
            float randomX = (float) (random.NextDouble() * 100 - 50);
            float randomY = (float) (random.NextDouble() * 121 - 130);;
            Vector3 newDestination = new Vector3(randomX, randomY, agent.destination.z);
            agent.SetDestination(newDestination);
        }

        joystickVec = direction;

        float distance = Vector3.Distance(transform.position, agent.steeringTarget);

        if (distance < 0.5f) {
            agent.nextPosition = transform.position;
        } else {
            direction = (agent.steeringTarget - transform.position).normalized;
        }

        /*if (npcIndex == 1) {
            Debug.Log(agent.destination.y);
        }*/
        
        if (agent.remainingDistance < 0.5f) {
            agent.SetDestination(nPCManager.RequestNewMiningPosition(transform.position, transform.eulerAngles.z));
        }

        if (!stopMoving) {
            MoveVehicle();
        }
    }

    public void MoveVehicle() {
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

}