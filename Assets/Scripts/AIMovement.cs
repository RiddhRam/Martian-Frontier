using System.Collections.Generic;
using UnityEngine;

public class AIMovement : MonoBehaviour
{
    public GameObject mine;
    public string vehicleType = "Driller";
    public int drillTier = 1;


    private Vector3 targetPosition;
    private bool isMoving = false;
    float angle;
    float step;

    private MineRenderer mineRenderer;

    // For detecting if the vehicle is stuck
    private Vector3 previousPosition;
    private float stuckTimer = 0f;
    private float stuckThreshold = 3f; // Time threshold for being stuck (3 seconds)
    private float pushDistance = 3f; // Distance to push forward if stuck
    float positionTolerance = 0.01f; // Adjust this value if needed
    List<Vector3> travelPositions = new();

    // X: from -68 to 68
    // Y: 
    // Maximum: -5
    // Minimum: (Manually measured)
    // Tier 1: -150
    // Tier 2: -320
    // Tier 3: -500

    void Start()
    {
        // Set initial target position
        ChooseNewTargetPosition();
        previousPosition = transform.position; // Store initial position
        mineRenderer = mine.GetComponent<MineRenderer>();
    }

    void Update()
    {
        // Check if y-coordinate is above -5 and reset position if needed
        if (transform.position.y > -5)
        {
            travelPositions.Clear();
            travelPositions.Add(new Vector3(0, -5, 0));
            isMoving = true;
        }

        if (travelPositions.Count <= 0) {
            return;
        }

        targetPosition = travelPositions[0];
        // Face the direction of movement
        Vector3 direction = (targetPosition - transform.position).normalized;

        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Use bezier curve to curve between 2 points.
        // Place all interpolated points into a list
        // targetPosition = first element in list
        // Remove element when done.
        // Use the list even for a straight line, for readability
        // If a straight line, there will be only one point in the list
        // Wait until vehicle is done moving toward point before editing list by adding or removing elements

        // Move towards the target position
        if (isMoving)
        {
            step = 10f * Time.deltaTime; // Adjust speed as needed
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // Check if reached the target position
            if (transform.position == targetPosition)
            {

                travelPositions.RemoveAt(0);

                // Check mine progress
                int leftToDestroy = 0;
                int alreadyDestroyed = 0;
                SerializableDictionary<Vector2Int, int>[,] unplaced = mineRenderer.GetUnplacedTilemapsTileValues();
                SerializableDictionary<Vector2Int, int>[,] destroyed = mineRenderer.GetDestroyedTilemapsTileValues();

                for (int i = 0; i != unplaced.GetLength(0); i++) {
                    for (int j = 0; j != unplaced.GetLength(1); j++) {
                        if (unplaced[i, j] == null) {
                            break;
                        }
                        leftToDestroy += unplaced[i, j].Count;
                        alreadyDestroyed += destroyed[i, j].Count;
                    }
                }

                Debug.Log(alreadyDestroyed * 100f / (leftToDestroy + alreadyDestroyed));

                // If at least 20%, reset mine
                if ((alreadyDestroyed * 100f / (leftToDestroy + alreadyDestroyed)) >= 20) {
                    GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>().CallResetMineFromButton();
                    return;
                }

                // If reached final desintation
                if (travelPositions.Count == 0) {
                    isMoving = false;
                    // Go to new position
                    ChooseNewTargetPosition();
                }
            }
        }

        // Check 1: Check if vehicle is in same spot as earlier (within position tolerance)
        if (Vector3.Distance(transform.position, previousPosition) < positionTolerance)
        {
            stuckTimer += Time.deltaTime;

            // Check 2: Check if the vehicle is stuck (hasn't moved for 'stuckThreshold' seconds)
            if (stuckTimer >= stuckThreshold)
            {
                // Push the vehicle forward in the direction it's facing
                transform.position += direction * pushDistance;
                stuckTimer = 0f; // Reset stuck timer
            }
        }
        else
        {
            stuckTimer = 0f; // Reset stuck timer if position changed
        }

        // Store the current position for next frame
        previousPosition = transform.position;
    }

    void ChooseNewTargetPosition()
    {
        travelPositions.Clear();

        // Determine y-bounds based on drill tier
        float minY = -5;
        switch (drillTier)
        {
            case 1:
                minY = -150;
                break;
            case 2:
                minY = -320;
                break;
            case 3:
                minY = -500;
                break;
        }

        // Choose a random position within the bounds
        float randomX = Random.Range(-68f, 68f);
        float randomY = Random.Range(minY, -5f);

        Vector3 finalPos = new(randomX, randomY, 0);

        // 1 in 3 chance for a curved route
        if (Random.Range(0, 3) == 0)
        {
            // Calculate a control point for the curve
            Vector3 midPoint = (transform.position + finalPos) / 2;
            Vector3 controlPoint = midPoint + new Vector3(0, 5, 0); // Adjust offset as needed

            // Interpolate points along the Bézier curve
            int numCurvePoints = 5; // Adjust for curve smoothness
            for (int i = 1; i <= numCurvePoints; i++)
            {
                float t = i / (float)numCurvePoints;
                Vector3 curvePoint = GetBezierPoint(transform.position, controlPoint, finalPos, t);
                travelPositions.Add(curvePoint);
            }
        }
        else
        {
            // Go directly to the final position
            travelPositions.Add(finalPos);
        }

        Debug.Log(travelPositions[^1]);

        isMoving = true;
    }

    Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // Quadratic Bézier curve formula
        return Mathf.Pow(1 - t, 2) * p0 +
            2 * (1 - t) * t * p1 +
            Mathf.Pow(t, 2) * p2;
    }
}
