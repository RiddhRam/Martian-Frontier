using System;
using System.Collections;
using System.Collections.Generic;
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
    public float timer = 0;
    private readonly float maxTimer = 20f;

    // Cache
    NavMeshPath path;

    public bool transitioning = false;

    public Vector3 dest;

    // Start is called before the first frame update
    void Start() {
        agent.updateUpAxis = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        tilemapAStar.mineRenderer = nPCManager.mineRenderer;

        rb = GetComponent<Rigidbody2D>();

        StartCoroutine(HoldPlayerCardStill());
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (transitioning) {
            return;
        }

        timer += Time.deltaTime;

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

            // If npc is close or timer reached then choose a new spot
            if (Vector3.Distance(transform.position, dest) < 0.5f || timer > maxTimer) {
                RequestNewPosition();
            }

        } else {
            
            if (tilemapAStar.Waypoints.Count != 0 && maxTimer > timer) {
                float distance = Vector3.Distance(transform.position, tilemapAStar.Waypoints[0]);

                if (distance < 2f) {
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

        while (Vector3.Distance(transform.position, dest) > 1f) {
            joystickVec = (dest - transform.position).normalized;
            MoveVehicle();
            yield return null;
        }
        rb.velocity = Vector2.zero;

        yield return new WaitUntil(() => nPCManager.mineRenderer.mineInitialization != 0);
        RequestNewPosition();
        yield return new WaitForSeconds((float) random.NextDouble() * 3);

        if (Vector3.Distance(dest, new(0, 6)) < 0.1f) {
            UpdateAgentDestination(new(random.Next(-2, 2), random.Next(6, 9)));
            tilemapAStar.GeneratePath(transform.position, new(0, 6), 3, true);
        }

        if (haulerController == null) {
            agent.enabled = false;
            agent.enabled = true;
        }

        transitioning = false;
    }

    public Vector3 GetRandomPosition() {
        // Used by drillers only

        // First algorithm, only works when tilemaps are generated, so later in the game not at the start
        SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValues = nPCManager.mineRenderer.unplacedTilemapsTileValues;

        int rowsPerTier = unplacedTilemapsTileValues.GetLength(1)/nPCManager.mineRenderer.oresPerTier.Length; // 18 * 3
        int lower = rowsPerTier * (drillTier - 1) - 1;
        if (lower < 0) {
            lower = 0;
        }
        int upper = rowsPerTier * drillTier - 1; 

        Vector2Int bestTilemaptoTarget = new(0, lower);

        bool tilemapGenerated = false;

        try {
            if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y] != null) {
                tilemapGenerated = true;
            }
        } catch {
            tilemapGenerated = false;
        }
        

        if (tilemapGenerated) {
            // Find best tilemap
            int randomSubtract = random.Next(-80, 80);
            for (int i = lower; i != upper; i++) {
                for (int j = 0; j != unplacedTilemapsTileValues.GetLength(0); j++) { 
                    try {
                        if (unplacedTilemapsTileValues[j, i] == null) {
                            break;
                        }
                    } catch {
                        break;
                    }
                    
                    // Choose randomly between the best tilemaps if there's a tie
                    if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Count == unplacedTilemapsTileValues[j, i].Count && random.NextDouble() < 0.33) {
                        bestTilemaptoTarget = new(j, i);
                    }
                    // Otherwise choose the best if not a tie (subtract from the count to add a bit of randomness)
                    else if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Count - randomSubtract < unplacedTilemapsTileValues[j, i].Count) {
                        bestTilemaptoTarget = new(j, i);
                    }
                }
            }

            // Get all tiles with ores in the best tilemap
            List<Vector2Int> oreTiles = new();

            foreach (Vector2Int tilePos in unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Keys) {
                if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].TryGetValue(tilePos, out int value) && nPCManager.mineRenderer.oreDelegation.VerifyIfOre(value))
                {
                    int oreTier = nPCManager.mineRenderer.GetTileTier(nPCManager.mineRenderer.tileValues[value]);
                    if (drillTier - 1 > oreTier || oreTier > drillTier) {
                        continue;
                    }

                    oreTiles.Add(tilePos);
                }
            }

            // If tiles found
            if (oreTiles.Count != 0) {
                // Choose a random ore tile
                Vector2Int chosenCell = oreTiles[random.Next(0, oreTiles.Count)];

                return new(chosenCell.x, chosenCell.y, 0);
            }
        }
        
        // Second algorithm, usually used at the start of the game
        timer = 0;

        int maxY;
        int minY;

        if (drillTier == 1)  {
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
        timer = 0;

        // Get hauler position
        if (haulerController != null) {
            AskIfHaulingIsNeeded();

            // If hauler doesnt need to become a drill, or isnt waiting in spawn, then go to a new position
            if (!transitioning) {
                UpdateAgentDestination(RequestNewHaulerPosition());
            } 
            // Otherwise drop off materials if there are more than 10 then try again
            else if (haulerController.GetTotalMaterialCount() > 0) {
                UpdateAgentDestination(new(0, 6));
                tilemapAStar.GeneratePath(transform.position, new(0, 6), 3, true);
                transitioning = false;
            }
            // Otherwise, it is going to become a driller
            
            return;
        }

        // Get driller position
        UpdateAgentDestination(nPCManager.RequestNewMiningPosition(transform.position, transform.eulerAngles.z, drillTier));
    }

    public Vector3 RequestNewHaulerPosition() {

        // Threshold
        if (haulerController.GetTotalMaterialCount() >= haulerController.GetMaxMaterials() * 0.5 || (nPCManager.GetMaterialCount() < nPCManager.Get1HaulerThreshold() && nPCManager.drillingNeeded)) {
            tilemapAStar.GeneratePath(transform.position, new(0, 6), 3, true);
            return new(0, 6);
        }

        // If path is null, initialize it
        path ??= new NavMeshPath();

        Vector3 newHaulerPosition;

        // Check if the game object's collider is touching a tilemap with "Mine Tag"
        // Dont do this in the spawn
        if (transform.position.y < -5) {
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, new(40, 40), 0);

            foreach (Collider2D collision in colliders) {
                if (!collision.CompareTag("Material Tag")) {
                    continue;
                }

                newHaulerPosition = collision.transform.position;

                tilemapAStar.GeneratePath(transform.position, newHaulerPosition, 3);

                if (tilemapAStar.PathFound) {

                    return newHaulerPosition;
                }
            }
        }

        // Check for a position for a max of 60 times. If still invalid, then the hauler is too big
        // TODO: Code dupe, fix this, its also in npc manager
        int haulPositionCount = 0;
        do {
            newHaulerPosition = nPCManager.RequestNewHaulerPosition(drillTier);
            tilemapAStar.GeneratePath(transform.position, newHaulerPosition, 3);

            haulPositionCount++;
        } 
        while(!tilemapAStar.PathFound && haulPositionCount < 10);

        // If invalid
        if (haulPositionCount >= 10) {
            newHaulerPosition = new(0, -6);
            tilemapAStar.GeneratePath(transform.position, new(0, 6), 3, true);
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

                for (int j = 0; j != frontWheels.childCount; j++) {
                    frontWheels.GetChild(j).GetComponent<BoxCollider2D>().enabled = false;
                }
                return;
            }
        }
        frontWheels = null;

        
    }

    public void AskIfHaulingIsNeeded() {
        transitioning = true;

        if (nPCManager.drillingNeeded) {
            // If has cargo, don't switch yet, let it drop off the ores then check again
            if (haulerController.GetTotalMaterialCount() > 0) {
                return;
            }

            StartCoroutine(nPCManager.SwitchHaulerToDriller(npcIndex));
        } else {
            transitioning = false;
            return;
        }

        StartCoroutine(MakeHaulerWait());
    }

    private IEnumerator MakeHaulerWait() {
        // If transitioning to a driller, keep going to final destination
        while (Vector3.Distance(transform.position, dest) > 0.5f) {
            MoveVehicle();
            joystickVec = (dest - transform.position).normalized;
            yield return null;
        }

        // Then stop moving and wait
        stopMoving = true;
        rb.velocity = Vector2.zero;
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