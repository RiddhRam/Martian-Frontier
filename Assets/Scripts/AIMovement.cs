using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AIMovement : MonoBehaviour
{
    public string vehicleType = "Driller";
    public int drillTier = 1;

    private Vector3 targetPosition;
    private bool isMoving = false;
    float angle;
    float step;

    public GameObject cashEarnedText;
    public GameObject mapText;
    public GameObject progressBar;
    private MineRenderer mineRenderer;
    private RefineryController refineryController;
    private GameObject materialsDelegator;
    private UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    private OreDelegation oreDelegation;

    // For detecting if the vehicle is stuck
    private Vector3 previousPosition;
    private float stuckTimer = 0f;
    private float stuckThreshold = 3f; // Time threshold for being stuck (3 seconds)
    private float pushDistance = 3f; // Distance to push forward if stuck
    float positionTolerance = 0.1f; // Adjust this value if needed
    readonly List<Vector3> travelPositions = new();

    bool firstThresholdReached = false;
    bool secondThresholdReached = false;
    long cashEarned = 0;

    void Start()
    {
        // Set initial target position
        ChooseNewTargetPosition();
        previousPosition = transform.position; // Store initial position

        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        materialsDelegator = GameObject.Find("Materials Delegator");
        uncollectedMaterialsDelegator = materialsDelegator.GetComponent<UncollectedMaterialsDelegator>();
        oreDelegation = GameObject.Find("Ore Prices").GetComponent<OreDelegation>();

        cashEarnedText.transform.parent.gameObject.SetActive(true);
        mapText.SetActive(false);
        progressBar.SetActive(true);
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

                float progress = alreadyDestroyed * 100f / (leftToDestroy + alreadyDestroyed);
                Debug.Log(progress);
                // If at least 20%, reset mine
                if (progress >= 19) {
                    ResetMine();
                    return;

                } else if (progress >= 13 && !secondThresholdReached) {
                    transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
                    ReduceBattery();
                    RemoveMaterials();

                    if (refineryController.GetRefineryBattery() == 0) {
                        ResetMine();
                    }
                    secondThresholdReached = true;
                    return;

                } else if (progress >= 6 && !firstThresholdReached) {
                    transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
                    ReduceBattery();
                    RemoveMaterials();

                    if (refineryController.GetRefineryBattery() == 0) {
                        ResetMine();
                    }

                    firstThresholdReached = true;
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

    private void ReduceBattery() {
        float initial = refineryController.GetInitialBattery();
        float percentage = 0.33f; 
        float current = refineryController.GetRefineryBattery() - (initial * percentage);

        // Clamp current between 0 and initial
        current = Mathf.Clamp(current, 0, initial);
        refineryController.SetRefineryBattery(current);
    }

    private void ResetMine() {
        refineryController.CallResetMineFromButton();
        firstThresholdReached = false;
        secondThresholdReached = false;

        cashEarnedText.GetComponent<TextMeshProUGUI>().text = FormatPrice(cashEarned);

        cashEarned = 0;
    }

    private void RemoveMaterials() {
        List<Transform> activeChildren = new();
        string[] oreNames = oreDelegation.GetOreNames();
        int[] prices = oreDelegation.GetMaterialPrices();

        foreach (Transform child in materialsDelegator.transform) {
            if (child.gameObject.activeSelf) {
                activeChildren.Add(child);
            }
        }

        float oresToDestroy = refineryController.GetInitialBattery() * 0.33f;
        MaterialManager materialManager;

        int destroyedCount = 0;
        // Start at highest index, and go down
        for (int j = oreNames.Length - 1; j >= 0; j--) {
            // Go through all active children and find if any of highest one is active
            for (int i = activeChildren.Count - 1; i >= 0; i--) {
                
                // If already destroyed 33% then stop
                if (destroyedCount >= oresToDestroy) {
                    destroyedCount = (int) oresToDestroy;
                    break;
                }

                // Make sure it matches (child name has '(Clone)' in it, that's why we use .Contains method)
                if (!activeChildren[i].name.Contains(oreNames[j])) {
                    continue;
                }

                // Remove material
                materialManager = activeChildren[i].GetComponent<MaterialManager>();

                destroyedCount += materialManager.count;
                cashEarned += materialManager.count * prices[j];

                uncollectedMaterialsDelegator.RemoveMaterial(materialManager.id);
                mineRenderer.ReturnMaterialObject(activeChildren[i].gameObject, materialManager.materialIndex, materialManager.id);
                activeChildren.RemoveAt(i);
            }
            
        }
    }

    private void ChooseNewTargetPosition()
    {
        travelPositions.Clear();

        // (Manually measured)
        // X: from -68 to 68
        // Y: 
        // Maximum:
        // Tier 1: -6
        // Tier 2: -165
        // Tier 3: -335
        // Minimum: 
        // Tier 1: -155
        // Tier 2: -325
        // Tier 3: -505

        // Determine y-bounds based on drill tier
        float minY = -6;
        float maxY = -6;
        switch (drillTier)
        {
            case 1:
                minY = -155;
                maxY = -6;
                break;
            case 2:
                minY = -325;
                maxY = -165;
                break;
            case 3:
                minY = -505;
                maxY = -335;
                break;
        }

        // Choose a random position within the bounds
        float randomX = Random.Range(-68f, 68f);
        float randomY = Random.Range(minY, maxY);

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

    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor(price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor(price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor(price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor(price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor(price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor(price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

}
