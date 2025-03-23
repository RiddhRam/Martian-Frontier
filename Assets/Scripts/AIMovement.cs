using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AIMovement : MonoBehaviour
{
    public string mode = "Random";
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

    float positionTolerance = 0.1f; // Adjust this value if needed
    float rotationTolerance = 20f;

    readonly List<Vector3> travelPositions = new();

    bool firstThresholdReached = false;
    bool secondThresholdReached = false;
    long cashEarned = 0;


    void Start()
    {
        // Set initial target position(s)
        if (mode == "Random") {
            ChooseNewRandomTargetPosition();
        } else if (mode == "Grid") {
            GridMove();
        }

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
            GetNewPos();
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

                if (mode == "Random") {
                    RandomlyMove();
                }
            }
        }

        // Check 1: Check if vehicle is in same spot as earlier (within position tolerance)
        if (Vector3.Distance(transform.position, previousPosition) < positionTolerance + 0.01f)
        {
            stuckTimer += Time.deltaTime;

            // Check 2: Check if the vehicle is stuck (hasn't moved for 'stuckThreshold' seconds)
            if (stuckTimer >= stuckThreshold)
            {
                StartCoroutine(WiggleVehicle());

                stuckTimer = 0f; // Reset stuck timer
            }
        }

        // Store the current position for next frame
        previousPosition = transform.position;
    }

    private IEnumerator WiggleVehicle() {
        // Store the original rotation
        Vector3 originalRotation = transform.rotation.eulerAngles;

        transform.rotation = Quaternion.Euler(originalRotation.x, originalRotation.y, originalRotation.z + 15);
        yield return new WaitForSeconds(0.1f);

        transform.rotation = Quaternion.Euler(originalRotation.x, originalRotation.y, originalRotation.z - 15);
        yield return new WaitForSeconds(0.1f);

        transform.rotation = Quaternion.Euler(originalRotation);
    }

    private void GetNewPos() {
        // If reached final desintation
        isMoving = false;

        // Go to new position
        if (mode == "Random") {
            ChooseNewRandomTargetPosition();
        } else if (mode == "Grid") {
            GridMove();
        }
    }

    private void RandomlyMove() {
        // Check mine progress
        int leftToDestroy = 0;
        int alreadyDestroyed = 0;
        SerializableDictionary<Vector2Int, int>[,] unplaced = mineRenderer.unplacedTilemapsTileValues;
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

        } else if (progress >= 13 && !secondThresholdReached) {
            transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
            ReduceBattery();
            RemoveMaterials();

            if (refineryController.GetRefineryBattery() == 0) {
                ResetMine();
            }
            secondThresholdReached = true;

        } else if (progress >= 6 && !firstThresholdReached) {
            transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
            ReduceBattery();
            RemoveMaterials();

            if (refineryController.GetRefineryBattery() == 0) {
                ResetMine();
            }

            firstThresholdReached = true;
        }
    }

    private void GridMove() {

        float x = transform.position.x;
        float y = transform.position.y;
        float currentRotation = transform.rotation.eulerAngles.z;

        isMoving = true;

        bool IsClose(float a, float b, float tolerance) => Mathf.Abs(a - b) <= tolerance;

        if (IsClose(0, x, 4) && IsClose(-7, y, 4) && IsClose(180, currentRotation, rotationTolerance)) {
            transform.rotation = Quaternion.Euler(0, 0, 270);
            travelPositions.Add(new(73, -7));
        }
        // If facing right 
        else if (IsClose(270, currentRotation, rotationTolerance)) {
            // Face down
            transform.rotation = Quaternion.Euler(0, 0, 180);
            travelPositions.Add(new(x, -505));
        }
        // If facing down 
        else if (IsClose(180, currentRotation, rotationTolerance)) {
            // Face left
            transform.rotation = Quaternion.Euler(0, 0, 90);
            travelPositions.Add(new(x - 4, -505));
        }
        // If facing left and at the bottom
        else if (IsClose(90, currentRotation, rotationTolerance) && IsClose(-505, y, 10)) {
            // Face up
            transform.rotation = Quaternion.Euler(0, 0, 0);
            travelPositions.Add(new(x, -8));
        }
        // If facing left and at the top
        else if (IsClose(90, currentRotation, rotationTolerance) && IsClose(-8, y, 10)) {
            // Face down
            transform.rotation = Quaternion.Euler(0, 0, 180);
            travelPositions.Add(new(x, -8));
        }
        // If facing up
        else if (IsClose(0, currentRotation, rotationTolerance) || IsClose(360, currentRotation, rotationTolerance) && IsClose(-8, y, 10)) {
            // Face left
            //transform.rotation = Quaternion.Euler(0, 0, 90);
            travelPositions.Add(new(x - 4, -8));
        }
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

    private void ChooseNewRandomTargetPosition()
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
