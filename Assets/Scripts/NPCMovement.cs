using System;
using System.Collections;
using System.Linq;
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
    Transform frontWheels;
    public int npcIndex;
    public NPCManager nPCManager;
    public NavMeshAgent agent;
    public TilemapAStar tilemapAStar;
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
    readonly System.Random random = new();
    private float timer = 0;
    private float maxTimer = 10f;

    // Cache
    NavMeshPath path;

    public bool transitioning = false;

    private Vector3 dest;

    // Start is called before the first frame update
    void Start() {
        agent.updateUpAxis = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        rb = GetComponent<Rigidbody2D>();

        StartCoroutine(HoldPlayerCardStill());
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (transitioning) {
            return;
        }

        if (!haulerController) {

            try {
                UpdateAgentDestination(agent.destination);
            } catch {
            }

            float distance = Vector3.Distance(transform.position, agent.steeringTarget);

            if (distance < 0.5f) {
                agent.nextPosition = transform.position;
            } else {
                direction = (agent.steeringTarget - transform.position).normalized;
            }

            // If npc is close then choose a new spot
            if (Vector3.Distance(transform.position, dest) < 0.5f) {
                RequestNewPosition();
            }

        } else {
            
            if (tilemapAStar.Waypoints.Count != 0) {
                float distance = Vector3.Distance(transform.position, tilemapAStar.Waypoints[0]);

                if (distance < 0.5f) {
                    tilemapAStar.Waypoints.RemoveAt(0);
                } else {
                    direction = (tilemapAStar.Waypoints[0] - transform.position).normalized;
                }
            } else {
                RequestNewPosition();
            }

        }

        // (0, -6, 0) means theres a problem with requesting new position
        if (Math.Abs(dest.y + 6) < 0.1) {
            // If its a hauler, drop to a smaller hauler or sell ores
            if (haulerController != null) {
                if (haulerController.GetTotalMaterialCount() > 0) {
                    UpdateAgentDestination(new(0, 6));
                    return;
                }
                
                if (haulerController.width > 3) {
                    StartCoroutine(nPCManager.SwitchToAnotherHauler(npcIndex, haulerIndex));
                    return;
                }
            }
            // If its a driller, just drill to some random spot
            else {
                UpdateAgentDestination(GetRandomPosition());
            }
        }

        joystickVec = direction;

        timer += Time.deltaTime;

        if (!stopMoving) {
            MoveVehicle();
        } else {
            rb.velocity = Vector2.zero;
        }
    }

    public void UpdateAgentDestination(Vector3 newDestination) {
        if (haulerController == null) {
            agent.SetDestination(newDestination);
        }

        dest = newDestination;
    }

    public IEnumerator WaitInSpawnPosition(Vector3 newDestination) {
        transitioning = true;

        yield return null;

        transitioning = true;
        
        UpdateAgentDestination(newDestination);


        while (Vector3.Distance(transform.position, dest) > 0.5f) {
            joystickVec = (dest - transform.position).normalized;

            MoveVehicle();

            yield return null;
        }
        rb.velocity = Vector2.zero;

        yield return new WaitUntil(() => nPCManager.mineRenderer.mineInitialization != 0);
        yield return new WaitForSeconds((float) random.NextDouble() * 4);

        transitioning = false;
    }

    public Vector3 GetRandomPosition() {
        timer = 0;

        int maxY;
        int minY;

        // Since player speed can't be 10 naturally, since specter is disabled, then it was set for recording purposes
        if (drillTier == 1 || playerSpeed == 10)  {
            minY = -155;
            maxY = -8;
        } else if (drillTier == 2) {
            minY = -325;
            maxY = -165;
        } else {
            minY = -505;
            maxY = -335;
        }
        
        float facingAngle = transform.eulerAngles.z;    // in degrees
        float halfArc = 90f;        // half of range

        float exclusionRange = 25f; // Inner range to exclude

        // Compute random angle within the arc but outside the exclusion range
        float randomAngle;
        if (random.NextDouble() < 0.5)
        {
            // Pick from lower range
            randomAngle = 360 -(float)(random.NextDouble() * (facingAngle - exclusionRange - (facingAngle - halfArc)) + (facingAngle - halfArc));
        }
        else
        {
            // Pick from upper range
            randomAngle = 360 -(float)(random.NextDouble() * ((facingAngle + halfArc) - (facingAngle + exclusionRange)) + (facingAngle + exclusionRange));
        }

        float distance = (float)(random.NextDouble() * 11 + 5);

        float rad = randomAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * distance;

        Vector3 newPos = new(transform.position.x + offset.x, transform.position.y + offset.y, dest.z);
 
        if (newPos.y < minY || newPos.y > maxY) {
            newPos.y *= -1;
        }

        newPos.x = Math.Clamp(newPos.x, -60, 60);
        newPos.y = Math.Clamp(newPos.y, minY, maxY);

        Vector2Int tilemapPos = nPCManager.mineRenderer.CalculateTileMapPos(new((int) newPos.x, (int) newPos.y));

        // If null, that tile wasn't generated yet
        if (nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y] == null) {
            return newPos;
        }

        if (!nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].ContainsKey(new((int) newPos.x, (int) newPos.y))) {

            for (int y = minY; y > maxY; y -= 12) {

                for (int x = -60; x <= 60; x += 25) {
                    tilemapPos = nPCManager.mineRenderer.CalculateTileMapPos(new(x, y));

                    if (nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Count > 0) {
                        Vector2Int randomValue = nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y]
                            .ElementAt(UnityEngine.Random.Range(0, nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Count))
                            .Key;

                        newPos = new(randomValue.x, randomValue.y);

                        break;
                    }
                }
            }
        }

        return newPos;
    }

    public void RequestNewPosition() {

        // Get hauler position
        if (haulerController != null) {
            AskIfHaulingIsNeeded();

            // If hauler doesnt need to become a drill, then go to a new position
            if (!transitioning) {
                UpdateAgentDestination(RequestNewHaulerPosition());
            } 
            // Otherwise drop off materials if there are more than 10 then try again
            else if (haulerController.GetTotalMaterialCount() > 0) {
                UpdateAgentDestination(new(0, 6));
                transitioning = false;
            }
            // Otherwise, it is going to become a driller
            
            return;
        }

        // Get driller position
        UpdateAgentDestination(nPCManager.RequestNewMiningPosition(transform.position, transform.eulerAngles.z, drillTier));
    }

    public Vector3 RequestNewHaulerPosition() {

        // Threshold; potent
        if (haulerController.GetTotalMaterialCount() >= haulerController.GetMaxMaterials() * 0.4 || nPCManager.GetMaterialCount() < nPCManager.Get1HaulerThreshold()) {
            return new(0, 6);
        }

        // If path is null, initialize it
        path ??= new NavMeshPath();

        Vector3 newHaulerPosition = new();

        bool foundNearbyMaterial = false;
        // Check if the game object's collider is touching a tilemap with "Mine Tag"
        // Dont do this in the spawn
        if (transform.position.y < -5) {
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, new(40, 40), 0);

            foreach (Collider2D collision in colliders) {
                if (!collision.CompareTag("Material Tag")) {
                    continue;
                }

                newHaulerPosition = collision.transform.position;

                tilemapAStar.GeneratePath(transform.position, newHaulerPosition);

                if (tilemapAStar.PathFound) {
                    foundNearbyMaterial = true;
                    break;
                }
            }
        }

        if (foundNearbyMaterial) {
            return newHaulerPosition;
        }

        // Check for a position for a max of 60 times. If still invalid, then the hauler is too big
        // TODO: Code dupe, fix this, its also in npc manager
        int haulPositionCount = 0;
        do {
            newHaulerPosition = nPCManager.RequestNewHaulerPosition(drillTier);
            tilemapAStar.GeneratePath(transform.position, newHaulerPosition, 3);

            haulPositionCount++;
        } 
        while(!tilemapAStar.PathFound && haulPositionCount < 60);

        // If invalid
        if (haulPositionCount >= 60) {
            newHaulerPosition = new(0, -6);
            nPCManager.drillingNeeded = true;
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

        Transform vehicle = transform.GetChild(2);

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
        transitioning = true;
        bool stay = nPCManager.CheckIfHaulingNeeded(npcIndex, haulerController.GetTotalMaterialCount() > 10);

        if (stay) {
            transitioning = false;
        }
    }

    private IEnumerator HoldPlayerCardStill() {

        while (true) {
            worldSpaceCanvas.transform.rotation = normalRotation;
            float angle = Mathf.Deg2Rad * transform.eulerAngles.z; // Get the Y-axis rotation

            // Calculate new position based on rotation
            float x = Mathf.Sin(angle) * 4.2f;
            float y = Mathf.Cos(angle) * 4.2f;

            worldSpaceCanvas.transform.localPosition = new Vector3(x, y, 0);

            yield return new WaitForEndOfFrame();
        }
    }
}