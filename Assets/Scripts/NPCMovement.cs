using System;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
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
    public SortingGroup sortingGroup;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI rebirthText;
    public Canvas worldSpaceCanvas;


    public int rebirthLevel;
    public HaulerController haulerController;
    public int haulerIndex;
    public int drillTier;
    public bool stopMoving;

    // Used in FixedUpdate, but declared here to reduce GC usage
    private Vector2 joystickVec;
    private float targetAngle;
    private float currentAngle;
    private float newAngle;
    private float tempLastRotation;
    private readonly float maxBodyRotation = 30;
    private readonly float maxChangeRotation = 20;
    private readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);
    private float wheelRotation;
    Vector2 direction;
    System.Random random = new();
    private float timer = 0;

    // Cache
    NavMeshPath path;

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
        worldSpaceCanvas.transform.rotation = normalRotation;
        float angle = transform.eulerAngles.z; // Get the Y-axis rotation

        // Convert angle to radians
        float rad = Mathf.Deg2Rad * angle;

        // Calculate new position based on rotation
        float x = Mathf.Sin(rad) * 3;
        float y = Mathf.Cos(rad) * 4;

        // Update canvas position relative to the vehicle
        worldSpaceCanvas.transform.localPosition = new Vector3(x, y, 0);

        try {
            agent.SetDestination(agent.destination);
        } catch {
        }
        

        // (400, 400, 0) means theres a problem with requesting new position
        if (Math.Abs(agent.destination.y - -6) < 0.1) {
            // If its a hauler, drop to a smaller hauler
            if (haulerController != null) {
                if (haulerController.GetTotalMaterialCount() > 0) {
                    agent.SetDestination(new(0, 6));
                    return;
                }
                
                if (haulerController.width > 3) {
                    StartCoroutine(nPCManager.SwitchToAnotherHauler(npcIndex, haulerIndex));
                    return;
                }
                
            } 
            // If its a driller, just drill to some random spot
            else {
                agent.SetDestination(GetRandomPosition());
            }
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

        if (!agent.enabled || !agent.isOnNavMesh) {
            return;
        }
        
        // If npc takes more than 10 seconds to reach destination, set a new destination
        if (agent.remainingDistance < 0.5f) {
            RequestNewPosition();
        } else {
            timer += Time.deltaTime;
        }

        if (timer > 10) {
            agent.enabled = false;
            agent.enabled = true;
            RequestNewPosition();
        }

        if (!stopMoving) {
            MoveVehicle();
        } else {
            rb.velocity = Vector2.zero;
        }
    }

    public Vector3 GetRandomPosition() {
        timer = 0;

        int maxY;
        int minY;

        if (drillTier == 1)  {
            minY = -155;
            maxY = -6;
        } else if (drillTier == 2) {
            minY = -325;
            maxY = -165;
        } else {
            minY = -505;
            maxY = -335;
        }

        return new((float) (random.NextDouble() * 120 - 60), (float) (random.NextDouble() * (maxY - minY) + minY), agent.destination.z);
    }

    public void RequestNewPosition() {
        timer = 0;

        // Get hauler position
        if (haulerController != null) {
            agent.SetDestination(RequestNewHaulerPosition());
            return;
        }

        // Get driller position
        agent.SetDestination(nPCManager.RequestNewMiningPosition(transform.position, transform.eulerAngles.z, drillTier));
    }

    public Vector3 RequestNewHaulerPosition() {

        if (haulerController.GetTotalMaterialCount() >= haulerController.GetMaxMaterials() * 0.5) {
            return new(0, 6);
        }

        // If path is null, initialize it
        path ??= new NavMeshPath();

        Vector3 newHaulerPosition;

        // Check for a position for a max of 5 times. If still invalid, then the hauler is too big
        int haulPositionCount = 0;
        do {
            newHaulerPosition = nPCManager.RequestNewHaulerPosition();

            haulPositionCount++;
        } 
        while((!agent.CalculatePath(newHaulerPosition, path) || path.status != NavMeshPathStatus.PathComplete) && haulPositionCount < 30);

        // If invalid
        if (haulPositionCount >= 30) {
            newHaulerPosition = new(0, -6);
        }

        return newHaulerPosition;
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

    public void AskIfHaulingIsNeeded() {
        nPCManager.CheckIfHaulingNeeded(npcIndex);
    }

}