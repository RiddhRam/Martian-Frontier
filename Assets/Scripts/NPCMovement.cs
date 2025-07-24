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
    public int npcIndex;
    public int drillTier;

    [Header("Scripts")]
    public NPCManager nPCManager;
    public DrillerController drillerController;

    [Header("Movement")]
    public NavMeshAgent agent;
    [SerializeField] private float playerSpeed = 5f;
    private Rigidbody2D rb;
    private float lastRotation; // To track the last rotation angle
    Transform frontWheels;
    public bool stopMoving;

    [Header("Visual")]
    public SortingGroup sortingGroup;
    public TextMeshProUGUI npcNameText;
    public Canvas worldSpaceCanvas;
    public Transform droneDetails;
    public GameObject pointToDrillArrow;
    public Transform noticeIcon;
    public RectTransform button;
    public Transform upright;

    [Header("Cache")]
    private Vector2 joystickVec;
    private float targetAngle;
    private float currentAngle;
    private float newAngle;
    private float tempLastRotation;
    private readonly float maxBodyRotation = 30;
    private readonly float maxChangeRotation = 20;
    private float wheelRotation;
    Vector2 direction;
    readonly System.Random random = new();
    public float timer = 0;
    private const float maxTimer = 5f;

    public bool transitioning = false;

    public Vector3 dest;

    private double cashToShow;
    private Coroutine floatingTextCoroutine;
    const float fadeDuration = 0.5f;
    public TextMeshProUGUI cashEarnedText;
    public SpriteRenderer cashIconSpriteRenderer;
    private static readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

    public float yPos;
    public float xPos;

    private Coroutine cooldownDrill;

    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        agent.updateUpAxis = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        rb = GetComponent<Rigidbody2D>();

        StartCoroutine(SetButtonSize());
        StartCoroutine(AnimateNoticeIcon());
        StartCoroutine(HoldCanvasStill());

        lineRenderer = new GameObject("PathLine").AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Required for color to show
        lineRenderer.startWidth = 1.5f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 5;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Manual control
        if (JoystickMovement.Instance.nPCMovement == this)
        {
            lineRenderer.gameObject.SetActive(false);
            joystickVec = JoystickMovement.Instance.joystickVec;
            MoveVehicle();
            return;
        }

        // Automatic controls
        if (transitioning)
        {
            return;
        }

        DrawPathLines();

        drillTier = nPCManager.playerState.GetRecommendedDrillTier();

        try
        {
            UpdateAgentDestination(agent.destination);
        }
        catch
        {
        }

        float distance = Vector3.Distance(transform.position, agent.steeringTarget);

        if (distance < 0.5f)
        {
            agent.nextPosition = transform.position;
        }
        else
        {
            direction = (agent.steeringTarget - transform.position).normalized;
        }

        // If npc is close or timer reached then choose a new spot
        if (Vector3.Distance(transform.position, dest) < 0.5f || timer > maxTimer)
        {
            RequestNewPosition();
        }

        joystickVec = direction;

        if (!stopMoving)
        {
            timer += Time.deltaTime;
            MoveVehicle();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private float GetMaxDistance()
    {
        return GetSpeed() * maxTimer;
    }

    private void DrawPathLines()
    {
        lineRenderer.gameObject.SetActive(true);

        float maxDistance = GetMaxDistance();

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, dest);

        // Distance and normalized value
        float distance = Vector3.Distance(transform.position, dest);
        float t = Mathf.Clamp01(1 - (distance / maxDistance)); // 0 = far, 1 = close

        // Interpolate color: Green (far) -> Red (close)
        Color color = Color.Lerp(Color.white, Color.green, t);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    public void UpdateAgentDestination(Vector3 newDestination)
    {
        float maxDistance = GetMaxDistance();

        // Clamp newDesintation to not be greater than the max Distance
        Vector3 direction = newDestination - transform.position;
        if (direction.magnitude > maxDistance)
        {
            direction.Normalize();
            newDestination = transform.position + direction * maxDistance;
        }

        if (Vector3.Distance(agent.destination, newDestination) > 0.1f) {
            agent.SetDestination(newDestination);
        }

        dest = newDestination;
    }

    public IEnumerator WaitInSpawnPosition(Vector3 newDestination)
    {
        transitioning = true;

        UpdateAgentDestination(newDestination);

        while (Vector3.Distance(transform.position, dest) > 1f)
        {
            // If player enables manual control
            if (JoystickMovement.Instance.nPCMovement == this)
            {
                transitioning = true;
                yield break;
            }

            joystickVec = (dest - transform.position).normalized;
            MoveVehicle();
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        // If player enables manual control
        if (JoystickMovement.Instance.nPCMovement == this)
        {
            transitioning = true;
            yield break;
        }

        yield return new WaitUntil(() => nPCManager.mineRenderer.mineInitialization != 0);
        RequestNewPosition();

        agent.enabled = false;
        agent.enabled = true;

        transitioning = false;
    }

    public Vector3 GetRandomPosition()
    {
        timer = 0;
        // SCRIPT HAS 2 ALGORITHMS. FIRST ONE RUNS WHEN TILEMAPS ARE GENERATED, SECOND ONE USUALLY RUNS AT THE START OF A NEW MINE

        // START OF FIRST ALGORITHM, only works when tilemaps are generated, so later in the game not at the start
        SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValues = nPCManager.mineRenderer.unplacedTilemapsTileValues;

        int rowsPerTier = unplacedTilemapsTileValues.GetLength(1) / nPCManager.mineRenderer.oresPerTier.Length; // 18 * 3
        int lower = rowsPerTier * (drillTier - 1) - 1;
        if (lower < 0)
        {
            lower = 0;
        }
        int upper = rowsPerTier * drillTier - 1;

        Vector2Int bestTilemaptoTarget = new(0, lower);

        bool tilemapGenerated = false;

        try
        {
            if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y] != null)
            {
                tilemapGenerated = true;
            }
        }
        catch
        {
            tilemapGenerated = false;
        }

        if (tilemapGenerated)
        {
            // Find best tilemap
            for (int i = lower; i != upper; i++)
            {
                for (int j = 0; j != unplacedTilemapsTileValues.GetLength(0); j++)
                {
                    try
                    {
                        if (unplacedTilemapsTileValues[j, i] == null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        break;
                    }

                    // Choose randomly between the best tilemaps if there's a tie
                    if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Count == unplacedTilemapsTileValues[j, i].Count && random.NextDouble() < 0.33)
                    {
                        bestTilemaptoTarget = new(j, i);
                    }
                    // Otherwise choose the best if not a tie
                    else if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Count < unplacedTilemapsTileValues[j, i].Count)
                    {
                        bestTilemaptoTarget = new(j, i);
                    }
                }
            }

            // Get all tiles with ores in the best tilemap
            List<Vector2Int> oreTiles = new();

            foreach (Vector2Int tilePos in unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].Keys)
            {
                if (unplacedTilemapsTileValues[bestTilemaptoTarget.x, bestTilemaptoTarget.y].TryGetValue(tilePos, out int value) && nPCManager.mineRenderer.oreDelegation.VerifyIfOre(value))
                {
                    oreTiles.Add(tilePos);
                }
            }

            // If tiles found
            if (oreTiles.Count != 0)
            {
                // Choose a random ore tile
                Vector2Int chosenCell = oreTiles[random.Next(0, oreTiles.Count)];
                return new(chosenCell.x, chosenCell.y, 0);
            }
        }
        // END OF FIRST ALGORITHM

        // START OF SECOND ALGORITHM, usually used at the start of the game
        int maxY;
        int minY;

        if (drillTier == 1)
        {
            minY = -155;
            maxY = -8;
        }
        else if (drillTier == 2)
        {
            minY = -325;
            maxY = -165;
        }
        else
        {
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
            randomAngle = 360 - (float)(random.NextDouble() * (facingAngle - exclusionRange - (facingAngle - halfArc)) + (facingAngle - halfArc));
        }
        else
        {
            // Pick from upper range
            randomAngle = 360 - (float)(random.NextDouble() * (facingAngle + halfArc - (facingAngle + exclusionRange)) + (facingAngle + exclusionRange));
        }

        float distance = (float)(random.NextDouble() * 11 + 5);

        float rad = randomAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * distance;

        Vector3 newPos = new(transform.position.x + offset.x, transform.position.y + offset.y, dest.z);

        if (newPos.y < minY || newPos.y > maxY)
        {
            newPos.y *= -1;
        }

        newPos.x = Math.Clamp(newPos.x, -60, 60);
        newPos.y = Math.Clamp(newPos.y, minY, maxY);

        Vector2Int tilemapPos = nPCManager.mineRenderer.CalculateTileMapPos(new((int)newPos.x, (int)newPos.y));

        // If null, that tile wasn't generated yet
        if (nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y] == null)
        {
            return newPos;
        }

        if (!nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].ContainsKey(new((int)newPos.x, (int)newPos.y)))
        {

            for (int y = minY; y > maxY; y -= 12)
            {

                for (int x = -60; x <= 60; x += 25)
                {
                    tilemapPos = nPCManager.mineRenderer.CalculateTileMapPos(new(x, y));

                    if (nPCManager.mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Count > 0)
                    {
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
        // END OF SECOND ALGORITHM
    }

    public void RequestNewPosition()
    {
        timer = 0;

        // Get driller position
        UpdateAgentDestination(nPCManager.RequestNewMiningPosition(transform.position, transform.eulerAngles.z, drillTier, this));
    }

    private float GetSpeed()
    {
        return playerSpeed * VehicleUpgradeBayManager.Instance.speedBoost;
    }

    public void MoveVehicle()
    {
        // Make sure vehicle is trying to move
        if (joystickVec.x == 0 && joystickVec.y == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speed = GetSpeed();

        // Translation logic
        // Translate the vehicle position
        rb.linearVelocity = new Vector2(
            joystickVec.x * speed,
            joystickVec.y * speed
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
        if (frontWheels)
        {
            SteerWheel(frontWheels, tempLastRotation, newAngle);
        }
    }

    private void SteerWheel(Transform frontWheels, float tempLastRotation, float newAngle)
    {

        // Might fail after changing vehicle
        try
        {

            if (tempLastRotation - 90 > newAngle)
            {
                newAngle += 360;
            }

            if (tempLastRotation < 0)
            {
                tempLastRotation += 360;
            }

            // newAngle - tempLastRotation is same as rotationDifference, but without Mathf.Abs
            // Wheel rotation cannot exceed 30 degrees of the body
            wheelRotation = Mathf.Clamp((newAngle - tempLastRotation) * 20, -maxBodyRotation, maxBodyRotation);

            // Wheel rotation cannot exceed 20 degrees of the last frame's rotation
            wheelRotation = Mathf.Clamp(wheelRotation - frontWheels.GetChild(0).rotation.z, -maxChangeRotation, maxChangeRotation);
            for (int i = 0; i != frontWheels.childCount; i++)
            {
                frontWheels.GetChild(i).rotation = Quaternion.Euler(0, 0, wheelRotation + newAngle);
            }
        }
        catch
        {
        }
    }

    public void SetSpeed(float newSpeed)
    {
        playerSpeed = newSpeed;

        Transform vehicle = transform.GetChild(2);

        // SetSpeed is called when a new vehicle is placed
        // When a new vehicle is placed we should also check if it needs animated wheels or not
        for (int i = 0; i != vehicle.childCount; i++)
        {
            if (vehicle.GetChild(i).name == "Front Wheels")
            {
                frontWheels = vehicle.GetChild(i);

                for (int j = 0; j != frontWheels.childCount; j++)
                {
                    frontWheels.GetChild(j).GetComponent<BoxCollider2D>().enabled = false;
                }
                return;
            }
        }
        frontWheels = null;


    }

    private IEnumerator SetButtonSize()
    {
        // Wait for driller controller to load
        yield return new WaitUntil(() => drillerController != null);

        // Get the references
        Transform drillParent = drillerController.transform.parent;
        BoxCollider2D drillBody = drillParent.GetChild(0).GetComponent<BoxCollider2D>();

        // Set the size
        float x = drillParent.localScale.x * drillBody.size.x + 3f;
        float y = drillParent.localScale.y * drillBody.size.y + 3f;

        button.sizeDelta = new(x, y);
    }

    private IEnumerator AnimateNoticeIcon()
    {
        Vector3 normalScale = noticeIcon.localScale;
        Vector3 bloatScale = normalScale * 1.4f;
        float interval = 2f;
        float animDuration = 0.2f;
        float nextBloatTime = Time.time + interval;

        while (true)
        {
            // check if it’s time to bloat
            if (Time.time >= nextBloatTime)
            {
                // SCALE UP
                float t = 0f;
                while (t < animDuration)
                {
                    t += Time.deltaTime;
                    float frac = t / animDuration;
                    noticeIcon.localScale = Vector3.Lerp(normalScale, bloatScale, frac);
                    yield return null;
                }

                // SCALE DOWN
                t = 0f;
                while (t < animDuration)
                {
                    t += Time.deltaTime;
                    float frac = t / animDuration;
                    noticeIcon.localScale = Vector3.Lerp(bloatScale, normalScale, frac);
                    yield return null;
                }

                // ensure exact reset
                noticeIcon.localScale = normalScale;
                nextBloatTime = Time.time + interval;
            }

            yield return null;
        }
    }

    private IEnumerator HoldCanvasStill()
    {
        while (true)
        {
            upright.rotation = normalRotation;

            yield return null;
        }
    }

    public void NewOreMined(double cashEarned)
    {
        cashToShow += cashEarned;

        if (floatingTextCoroutine != null)
        {
            StopCoroutine(floatingTextCoroutine);
        }
        floatingTextCoroutine = StartCoroutine(ShowFloatingText());
    }

    private IEnumerator ShowFloatingText()
    {

        // Show text and icon
        cashEarnedText.text = nPCManager.playerState.FormatPrice(new System.Numerics.BigInteger(cashToShow), 1);
        float alphaValue = 1;
        cashEarnedText.alpha = alphaValue;
        cashIconSpriteRenderer.color = new(1, 1, 1, alphaValue);

        // Wait 2 seconds
        yield return new WaitForSecondsRealtime(2);

        // Fade text and icon out
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            alphaValue = Mathf.Lerp(1f, 0f, time / fadeDuration);
            cashEarnedText.alpha = alphaValue;
            cashIconSpriteRenderer.color = new(1, 1, 1, alphaValue);
            yield return null;
        }

        // Ensure it’s fully invisible
        alphaValue = 0;
        cashEarnedText.alpha = alphaValue;
        cashIconSpriteRenderer.color = new(1, 1, 1, alphaValue);
        // Reset for next time
        cashToShow = 0;
    }

    public void StartCooldownDrill()
    {
        if (cooldownDrill == null)
        {
            cooldownDrill = StartCoroutine(CooldownDrill());
        }
    }

    private IEnumerator CooldownDrill()
    {
        // Drill should pause checking for ores and moving while cooling down
        transitioning = true;
        stopMoving = true;
        rb.linearVelocity = Vector2.zero;

        // Cooldown drill
        while (drillerController.drillHeat > 0)
        {
            drillerController.drillHeat = Mathf.Max(0, (int)(drillerController.drillHeat - VehicleUpgradeBayManager.Instance.GetCoolRate("")));
            yield return new WaitForFixedUpdate();
        }

        // Drill can resume
        transitioning = false;
        stopMoving = false;
        cooldownDrill = null;
    }
}