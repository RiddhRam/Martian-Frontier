using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapRecordingMode : MonoBehaviour
{
    public Transform playerVehicle;
    public GameObject mapText;
    public GameObject videoInfo;
    public PlayerState playerState;
    public UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    public JoystickMovement joystickMovement;
    public PlayerMovement playerMovement;
    public MineRenderer mineRenderer;
    public RawImage mapCameraView;
    public Outline panelOutline;

    public TextMeshProUGUI cargoValueText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI mineText;
    public TextMeshProUGUI valueText;

    public Slider depthProgressSlider;
    public TextMeshProUGUI depthProgressSliderText;
    public GameObject depthIconGO;
    public int minY;
    public int maxY;
    public int currentTier;

    public bool routeRoulette;
    public float moveSpeed;
    public GameObject arrowPanel;
    public GameObject arrowSeperator;
    public RectTransform arrow;
    public RectTransform outlineArrow;
    public RectTransform currentArrow;
    public RectTransform playerArrow;
    private float timer;
    private float targetTime;

    [SerializeField]
    int minimumCameraSize;
    [SerializeField]
    int maximumCameraSize;
    [SerializeField]
    int visionRadius;
    [SerializeField]
    float farthestRight;
    [SerializeField]
    float farthestLeft;
    [SerializeField]
    float farthestTop;
    [SerializeField]
    float farthestDown;
    System.Numerics.BigInteger originalBlocksMined;
    System.Numerics.BigInteger originalMineValue;

    Camera thisCamera;
    Camera mainCamera;

    HaulerController haulerController;

    // Mine values: 
    // Min and max vein radius: 2
    // Min and max vein count: 5

    private System.Random random = new System.Random();
    bool rotating = false;
    public float rotateDuration;
    private Coroutine rotateRoutine;
    private Vector2Int tileToGoTo;
    public float smoothFactor1;


    [Header("Movement Settings")]
    public float rotationSpeed;
    
    [Header("Raycast Settings")]
    public float raycastDistance;
    public float miniRaycastDistance;
    public string mineTagName = "Mine Tag";
    
    [Header("Debug")]
    public bool showRays = true;

    private Rigidbody2D rb;
    public Transform frontWheels;
    private readonly int largeAngleDiff = 40;
    private readonly int miniAngleDiff = 40;

    private readonly float maxBodyRotation = 30;
    private readonly float maxChangeRotation = 20;
    private float wheelRotation;

    void Start()
    {
        rb = playerVehicle.GetComponent<Rigidbody2D>();
        // If ray points aren't assigned, use the vehicle transform
        mapText.SetActive(false);
        //videoInfo.SetActive(true);
        thisCamera = GetComponent<Camera>();
        mainCamera = Camera.main;

        originalBlocksMined = playerState.GetBlocksMined();
        originalMineValue = uncollectedMaterialsDelegator.GetMineValue();

        // Hide map icons layer
        thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));

        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;

        if (routeRoulette) {
            thisCamera.orthographicSize = 19;
            cargoValueText.transform.parent.gameObject.SetActive(true);
            arrowPanel.SetActive(true);
            arrowSeperator.SetActive(true);
            currentArrow.transform.parent.gameObject.SetActive(true);

            RectTransform rectTransform = cargoValueText.transform.parent.GetComponent<RectTransform>();

            Vector3 localPos = rectTransform.localPosition;
            rectTransform.localPosition = new(localPos.x, 70, localPos.z);

            rectTransform.offsetMin = new Vector2(-160f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(160f, rectTransform.offsetMax.y);
            panelOutline.effectColor = new(0, 0, 0);
            panelOutline.gameObject.GetComponent<Image>().color = new(0, 0, 0);
        } else {
            thisCamera.orthographicSize = 21;
            depthProgressSlider.transform.parent.gameObject.SetActive(true);
            panelOutline.effectColor = new(44/255f, 44/255f, 44/255f);
        }
    }

    public void SetSliderBoundaries() {
        
        if (playerVehicle.position.y > -155)  {
            minY = -155;
            maxY = -6;
            currentTier = 1;
        } else if (playerVehicle.position.y > -325) {
            minY = -325;
            maxY = -165;
            currentTier = 2;
        } else if (playerVehicle.position.y > -325) {
            minY = -505;
            maxY = -335;
            currentTier = 3;
        }

    }

    void FixedUpdate()
    {
        Vector3 pos = playerVehicle.position;

        if (pos.x > farthestRight)
            farthestRight = pos.x;

        if (pos.x < farthestLeft)
            farthestLeft = pos.x;

        if (pos.y > farthestTop)
            farthestTop = pos.y;

        if (pos.y < farthestDown)
            farthestDown = pos.y;
        
        //Zoom();
        //ClampCamera();
        
        if (routeRoulette) {
            if (!rotating) {
                rotating = true;
                rotateRoutine = StartCoroutine(RotateVehicle(arrow.eulerAngles, rotateDuration));
            }
            
            float angle = (playerVehicle.eulerAngles.z + 90) * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            playerVehicle.position += moveSpeed * Time.deltaTime * (Vector3)forward;
            
            transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y - 10, transform.position.z);
            
            //float joystickAngle = Mathf.Atan2(joystickMovement.joystickVec.y, joystickMovement.joystickVec.x) * Mathf.Rad2Deg;
            //playerArrow.eulerAngles = new(0, 0, joystickAngle - 90);

            timer += Time.deltaTime;
            if (timer >= targetTime)
            {
                timer = 0f;
                targetTime = (float)(2.5f + (random.NextDouble() * (2.5f - 2f)));

                if (rotateRoutine != null) {
                    StopCoroutine(rotateRoutine);
                }
                rotating = false;

                // 1. ore finding algorithm
                // Convert the player's position to grid coordinates by rounding.
                Vector2Int posInt = new Vector2Int(
                    Mathf.RoundToInt(pos.x),
                    Mathf.RoundToInt(pos.y)
                );

                // List to store coordinates with nonzero values.
                List<Vector2Int> validCoords = new();

                SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValueDictionary = mineRenderer.GetUnplacedTilemapsTileValues();
                
                int maxRadius = 9;
                int minRadius = 4;
                float currentAngle = arrow.eulerAngles.z;
                // Search an area around the player.
                for (int x = posInt.x - maxRadius; x <= posInt.x + maxRadius; x++)
                {
                    for (int y = posInt.y - maxRadius; y <= posInt.y + maxRadius; y++)
                    {
                        Vector2Int coord = new Vector2Int(x, y);

                        // Skip the inner square
                        if (Math.Abs(x - posInt.x) <= minRadius && Math.Abs(y - posInt.y) <= minRadius)
                            continue;
                        if (transform.position.x < (-35f + maxRadius/2) && (x - posInt.x) < 0) {
                            continue;
                        } else if (transform.position.x > (35f - maxRadius/2) && (posInt.x - x) < 0) {
                            continue;
                        } else if (y > -35f) {
                            continue;
                        } 
                        else if (!CheckIfSurrounded(coord, unplacedTilemapsTileValueDictionary)) {
                            continue;
                        }

                        Vector2 mineDirection = new Vector2(coord.x, coord.y) - new Vector2(pos.x, pos.y);
                        float thisAngle = (Mathf.Atan2(mineDirection.y, mineDirection.x) * Mathf.Rad2Deg) - 90;

                        if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, thisAngle)) < 70f || Mathf.Abs(Mathf.DeltaAngle(currentAngle + 180f, thisAngle)) < 60f) {
                            continue;
                        }

                        Vector2Int tilemapPos = mineRenderer.CalculateTileMapPos(coord);

                        try {
                            if (unplacedTilemapsTileValueDictionary[tilemapPos.x, tilemapPos.y] != null) {

                            }
                        } catch  (Exception ex) {
                            Debug.Log(ex.Message);
                            continue;
                        }

                        // Check if the coordinate exists in the dictionary and has a nonzero value.
                        if (unplacedTilemapsTileValueDictionary[tilemapPos.x, tilemapPos.y].TryGetValue(coord, out int value) && value != 0)
                        {
                            validCoords.Add(coord);
                        }
                    }
                }

                float newAngle = 180;

                if (validCoords.Count != 0) {
                    Vector2Int chosenCoord = validCoords[random.Next(0, validCoords.Count)];

                    // Calculate the direction vector from the player to the chosen coordinate.
                    Vector2 mineDirection = new Vector2(chosenCoord.x, chosenCoord.y) - 
                                        new Vector2(pos.x, pos.y);
                    // Calculate the angle in degrees using Atan2.
                    newAngle = (Mathf.Atan2(mineDirection.y, mineDirection.x) * Mathf.Rad2Deg) - 90;
                }

                // 2. pure random algorithm
                /*
                float currentAngle = arrow.localEulerAngles.z;
                bool invalidAngle;

                do
                {
                    newAngle = (float)(0f + (random.NextDouble() * (361f - 0f)));
                    // The sprite is offset by 90 degress CCW
                    // Check if we need to block left or right or up angles
                    bool goingLeft = newAngle > 0f && newAngle < 180f; // Angles pointing left
                    bool goingRight = newAngle < 0f || newAngle > 180f; // Angles pointing right
                    bool goingUp = newAngle < 90f || newAngle > 270f; 

                    invalidAngle = Mathf.Abs(Mathf.DeltaAngle(currentAngle, newAngle)) < 90f ||
                                        Mathf.Abs(Mathf.DeltaAngle(currentAngle + 180f, newAngle)) < 40f ||
                                        (transform.position.x < -35f && goingLeft) ||
                                        (transform.position.x > 35f && goingRight) || 
                                        transform.position.y > -55 && goingUp;

                } 
                while (invalidAngle);*/


                // Set angle
                arrow.eulerAngles = new Vector3(0, 0, newAngle);
                outlineArrow.eulerAngles = arrow.eulerAngles;
                currentArrow.eulerAngles = arrow.eulerAngles;
            }

            //float zDifference = Mathf.DeltaAngle(arrow.rotation.eulerAngles.z, playerVehicle.transform.rotation.eulerAngles.z);
            //Debug.Log(zDifference);

        } 
        else {
            
            transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y, transform.position.z);            
            
            // Mining algorithm
            /*
            timer += Time.deltaTime;
            if (timer >= targetTime)
            {
                timer = 0f;
                targetTime = (float)(6.5f + (random.NextDouble() * (6.5f - 6f)));

                if (rotateRoutine != null) {
                    //StopCoroutine(rotateRoutine);
                    return;
                }

                Vector2Int posInt = new Vector2Int(
                    Mathf.RoundToInt(pos.x),
                    Mathf.RoundToInt(pos.y)
                );

                // List to store coordinates with nonzero values.
                List<Vector2Int> validCoords = new();

                SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValueDictionary = mineRenderer.GetUnplacedTilemapsTileValues();
                
                int maxRadius = 9;
                int minRadius = 4;

                float currentAngle = playerVehicle.eulerAngles.z;
                // Search an area around the player.
                for (int x = posInt.x - maxRadius; x <= posInt.x + maxRadius; x++)
                {
                    for (int y = posInt.y - maxRadius; y <= posInt.y + maxRadius; y++)
                    {
                        Vector2Int coord = new Vector2Int(x, y);

                        // Skip the inner square
                        if (Math.Abs(x - posInt.x) <= minRadius && Math.Abs(y - posInt.y) <= minRadius)
                            continue;
                        if (transform.position.x < (-15f + maxRadius/2) && (x - posInt.x) < 0) {
                            continue;
                        } else if (transform.position.x > (15f - maxRadius/2) && (posInt.x - x) < 0) {
                            continue;
                        } else if (y > -30f) {
                            continue;
                        } 
                        else if (!CheckIfSurrounded(coord, unplacedTilemapsTileValueDictionary)) {
                            continue;
                        }

                        Vector2 mineDirection = new Vector2(coord.x, coord.y) - new Vector2(pos.x, pos.y);
                        float thisAngle = (Mathf.Atan2(mineDirection.y, mineDirection.x) * Mathf.Rad2Deg) - 90;

                        if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, thisAngle)) < 70f || Mathf.Abs(Mathf.DeltaAngle(currentAngle + 180f, thisAngle)) < 60f) {
                            continue;
                        }

                        Vector2Int tilemapPos = mineRenderer.CalculateTileMapPos(coord);

                        try {
                            if (unplacedTilemapsTileValueDictionary[tilemapPos.x, tilemapPos.y] != null) {

                            }
                        } catch  (Exception ex) {
                            Debug.Log(ex.Message);
                            continue;
                        }

                        // Check if the coordinate exists in the dictionary and has a nonzero value.
                        if (unplacedTilemapsTileValueDictionary[tilemapPos.x, tilemapPos.y].TryGetValue(coord, out int value) && value != 0)
                        {
                            validCoords.Add(coord);
                        }
                    }
                }

                if (validCoords.Count != 0) {
                    tileToGoTo = validCoords[random.Next(0, validCoords.Count)];

                } else {
                    tileToGoTo = new((int) playerVehicle.position.x, -600);
                }

                if (!rotating) {
                    rotating = true;
                    rotateRoutine = StartCoroutine(RotateVehicleToTile(tileToGoTo, (float)(3.5f + (random.NextDouble() * (3.5f - 2.5f)))));
                }

            }*/


            /*In the FixedUpdate() function I need to do this (Its a 2d unity game by the way):

Spawn a physics collider that extends from the center of a 2d gameobject to 2 units in front. Keep in mind the gameobject can be rotated in any direction (z axis).

Then go through each object the collider hits, and check if any have the tag "Mine Tag". If it does, we need to rotate the gameobject. Spawn 2 more colliders on each side of the forward direction of the game object, say 15 degrees on each side. Check if there's room to move on either side, there is room if the collider doesnt hit any "Mine Tag" game object. Then get the gameobject to rotate there. Keep in mind: This is*/
            if (haulerController) {
                float angle = (playerVehicle.eulerAngles.z + 90) * Mathf.Deg2Rad;
                Vector2 forward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                playerVehicle.position += moveSpeed * Time.deltaTime * (Vector3)forward;

                NavigateThroughTunnel();
            }
        }

        UpdateText();
    }

    void NavigateThroughTunnel()
    {
        // mini is smaller than the others, if they hit something, then we need to hard turn
        bool wallAhead = CheckForMine(playerVehicle.transform.up, false);
        bool wallLeft = CheckForMine(Quaternion.Euler(0, 0, largeAngleDiff) * playerVehicle.transform.up, false);
        bool wallRight = CheckForMine(Quaternion.Euler(0, 0, -largeAngleDiff) * playerVehicle.transform.up, false);
        bool wallLeftMini = CheckForMine(Quaternion.Euler(0, 0, miniAngleDiff) * playerVehicle.transform.up, true);
        bool wallRightMini = CheckForMine(Quaternion.Euler(0, 0, -miniAngleDiff) * playerVehicle.transform.up, true);

        // Get distances to walls for smoother steering
        float distanceAhead = GetDistanceToMine(playerVehicle.transform.up);
        float distanceLeft = GetDistanceToMine(Quaternion.Euler(0, 0, largeAngleDiff) * playerVehicle.transform.up);
        float distanceRight = GetDistanceToMine(Quaternion.Euler(0, 0, -largeAngleDiff) * playerVehicle.transform.up);
        
        // Draw debug rays
        /*if (showRays)
        {
            Debug.DrawRay(playerVehicle.position, playerVehicle.transform.up * raycastDistance, wallAhead ? Color.blue : Color.green);
            //Debug.DrawRay(playerVehicle.position, Quaternion.Euler(0, 0, largeAngleDiff) * playerVehicle.transform.up * raycastDistance, wallLeft ? Color.yellow : Color.green);
            //Debug.DrawRay(playerVehicle.position, Quaternion.Euler(0, 0, -largeAngleDiff) * playerVehicle.transform.up * raycastDistance, wallRight ? Color.red : Color.green);
            Debug.DrawRay(playerVehicle.position, Quaternion.Euler(0, 0, miniAngleDiff) * playerVehicle.transform.up * miniRaycastDistance, wallLeftMini ? Color.yellow : Color.green);
            Debug.DrawRay(playerVehicle.position, Quaternion.Euler(0, 0, -miniAngleDiff) * playerVehicle.transform.up * miniRaycastDistance, wallRightMini ? Color.red : Color.green);
        }*/

        // Calculate steering direction - positive for left, negative for right
        float steeringDirection = 0f;
        
        // Decision making logic
        if (wallAhead)
        {
            if (wallLeftMini && !wallRightMini) {
                // Hard turn right
                steeringDirection = -1.5f;
            } 
            else if (wallRightMini && !wallLeftMini) {
                // Hard turn left
                steeringDirection = 1.5f;
            }
            else
            {
                // Dead end, turn around
                steeringDirection = 2f;
            }
        }
        else
        {
            float centeringFactor = (distanceLeft - distanceRight) / (distanceLeft + distanceRight);
            steeringDirection = centeringFactor * 0.4f;

            // No wall ahead, but check if we're too close to side walls and adjust
            if (wallLeftMini && !wallRightMini) {
                // Hard turn right
                steeringDirection -= 0.25f;
            } 
            else if (wallRightMini && !wallLeftMini) {
                // Hard turn left
                steeringDirection += 0.25f;
            } 
        }

        // Apply the calculated steering with smooth interpolation
        ApplySmoothSteering(steeringDirection);
    }

    float GetDistanceToMine(Vector2 direction)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(playerVehicle.position, direction, miniRaycastDistance);

        float minDistance = raycastDistance;
        foreach (RaycastHit2D hit in hits) {
            if (hit.collider.CompareTag(mineTagName)) {
                if (hit.distance < minDistance) {
                    minDistance = hit.distance;
                }
            }
        }
        
        return minDistance; // Return max distance if no wall found
    }

    // Variable to track current steering intensity
    private float currentSteeringAmount = 0f;

    void ApplySmoothSteering(float targetSteeringDirection)
    {
        // Smoothly interpolate between current steering and target steering
        currentSteeringAmount = Mathf.Lerp(currentSteeringAmount, targetSteeringDirection, 
                                        Time.fixedDeltaTime * 8f); // Adjust this value to control responsiveness
        
        // Apply the rotation based on the smoothed steering amount
        float rotationAmount = currentSteeringAmount * rotationSpeed * Time.fixedDeltaTime;

        //Debug.Log($"Current: {currentSteeringAmount + rb.rotation}, Target: {targetSteeringDirection + rb.rotation}");
        rb.rotation += rotationAmount;
        
        SteerWheel(frontWheels, rb.rotation, targetSteeringDirection);
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
            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).localEulerAngles = new(0, 0, wheelRotation + newAngle);
            }
        } catch {
        }
    }

    bool CheckForMine(Vector2 direction, bool mini)
    {
        RaycastHit2D[] hits;

        if (!mini) {
            hits = Physics2D.RaycastAll(playerVehicle.position, direction, raycastDistance);
        } else {
            hits = Physics2D.RaycastAll(playerVehicle.position, direction, miniRaycastDistance);
        }
        
        foreach (RaycastHit2D hit in hits) {
            if (hit.collider.CompareTag(mineTagName)) {
                return true;
            }
        }
        
        return false;
    }

    void RotateVehicle(float direction)
    {
        // Apply rotation based on direction and rotation speed
        float rotationAmount = direction * rotationSpeed * Time.fixedDeltaTime;
        rb.rotation += rotationAmount;
    }

    private IEnumerator RotateVehicle(Vector3 targetEulerAngles, float duration)
    {
        
        Quaternion startRotation = playerVehicle.rotation;
        Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            playerVehicle.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);
            yield return null; // Wait until the next frame
        }

        // Ensure it ends exactly at the target rotation
        playerVehicle.rotation = targetRotation;
        rotateRoutine = null;
        rotating = false;
    }

    private IEnumerator RotateVehicleToTile(Vector2Int targetTile, float duration)
    {
        
        Quaternion startRotation = playerVehicle.rotation;
        Quaternion targetRotation;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            Vector2 mineDirection = new Vector2(targetTile.x, targetTile.y) - 
                                        new Vector2(playerVehicle.position.x, playerVehicle.position.y);

            float angle = Mathf.Atan2(mineDirection.y, mineDirection.x) * Mathf.Rad2Deg;
            if (targetTile.y == -600)
                angle -= 90;

            targetRotation = Quaternion.Euler(0, 0, angle);

            float smoothFactor = smoothFactor1; // Adjust this value as needed

            Debug.Log(angle);

            playerVehicle.rotation = Quaternion.Slerp(playerVehicle.rotation, targetRotation, smoothFactor * Time.deltaTime);
            yield return null; // Wait until the next frame
        }

        rotateRoutine = null;
        rotating = false;
    }

    private bool CheckIfSurrounded(Vector2Int tilePos, SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValueDictionary) {
        int searchRadius = 2;

        for (int x = tilePos.x - searchRadius; x <= tilePos.y + searchRadius; x++) {
            for (int y = tilePos.y - searchRadius; y <= tilePos.y + searchRadius; y++) {
                Vector2Int tilemapPos = mineRenderer.CalculateTileMapPos(new(x, y));

                if (!unplacedTilemapsTileValueDictionary[tilemapPos.x, tilemapPos.y].ContainsKey(new(x, y))) {
                    return false;
                }
            }
        }

        return true;
    }

    private void ClampCamera()
    {
        
        if (thisCamera.orthographicSize >= maximumCameraSize - 0.5) {
            transform.position = Vector3.Lerp(transform.position, new(playerVehicle.position.x, playerVehicle.position.y, transform.position.z), Time.deltaTime * 5f);
        } else {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp((farthestLeft + farthestRight) / 2, farthestLeft - visionRadius, farthestRight + visionRadius);
            clampedPosition.y = Mathf.Clamp((farthestTop + farthestDown) / 2, -600, -thisCamera.orthographicSize - 4.5f);
            transform.position = clampedPosition;
        }
        
    }

    private void Zoom()
    {
        float width = farthestRight - farthestLeft + (visionRadius * 3);
        float height = (farthestTop - farthestDown + (visionRadius * 8))/2;
        float targetSize = Mathf.Max(width, height);
        targetSize = Mathf.Clamp(targetSize, minimumCameraSize, maximumCameraSize);
        thisCamera.orthographicSize = Mathf.Lerp(thisCamera.orthographicSize, targetSize, Time.deltaTime * 5);
    }

    public void UpdateText() {
        depthText.text = FormatPositionY((int) -playerVehicle.position.y - 5);
        mineText.text = FormatPrice(playerState.GetBlocksMined() - originalBlocksMined);
        valueText.text = "$" + FormatPrice(uncollectedMaterialsDelegator.GetMineValue() - originalMineValue);
        
        if (routeRoulette) {
            cargoValueText.text = valueText.text;
        } else {
            int previousTier = currentTier;
            SetSliderBoundaries();

            if (!haulerController) {

                if (currentTier != previousTier) {
                    // Flipped cuz we need positive values
                    depthProgressSlider.minValue = -maxY;
                    depthProgressSlider.maxValue = -minY;
                }

                depthProgressSlider.value = -playerVehicle.position.y;
                depthProgressSliderText.text = depthText.text;

            } else {
                depthIconGO.SetActive(false);
                depthProgressSlider.maxValue = 8000 * Mathf.Pow(10, currentTier);

                depthProgressSlider.value = (float) haulerController.GetTotalCargoValue();
                
                depthProgressSliderText.text = "$" + FormatPrice(haulerController.GetTotalCargoValue());
            }

            cargoValueText.text = valueText.text;
        }
        
    }

    private string FormatPositionY(int positionY)
    {
        if (positionY <= 0) {
            return "0 M";
        }
        
        if (positionY >= 1_000)
        {
            // Truncate to 3 decimal places and format with "KM"
            return (positionY / 1_000) + " KM";
        } else {
            return positionY + " M";
        }
    }

    private string FormatPrice(System.Numerics.BigInteger price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("0.##") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("0.##") + "B";
        }
        else if (price >= 1_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("0.##") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("0.##") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }


    [ContextMenu("Reset Camera")]
    public void ResetCamera() {
        
        transform.position = new(0, -256, -17);

        Vector3 pos = playerVehicle.position;

        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;
        
        // Create a new RenderTexture
        RenderTexture renderTexture = new RenderTexture(1749, 2725, 24, RenderTextureFormat.ARGB32); // 24 is the depth buffer bit size
        renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.S8_UInt;
        renderTexture.Create();

        // Assign the RenderTexture to the mapCamera's target texture
        GetComponent<Camera>().targetTexture = renderTexture;
        mapCameraView.texture = renderTexture;
        originalBlocksMined = playerState.GetBlocksMined();
        originalMineValue = uncollectedMaterialsDelegator.GetMineValue();

        Transform vehicle = playerVehicle.transform.GetChild(0);
        BoxCollider2D boxCollider2D = vehicle.GetChild(1).GetComponent<BoxCollider2D>();
        if (boxCollider2D) {
            boxCollider2D.size = new(boxCollider2D.size.x + 2, boxCollider2D.size.y);
            haulerController = null;
            frontWheels = null;
        } else {
            haulerController = vehicle.GetComponent<HaulerController>();

            // SetSpeed is called when a new vehicle is placed
            // When a new vehicle is placed we should also check if it needs animated wheels or not
            for (int i = 0; i != vehicle.childCount; i++) {
                if (vehicle.GetChild(i).name == "Front Wheels") {
                    frontWheels = vehicle.GetChild(i);
                    break;
                }
            }

            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).GetComponent<PolygonCollider2D>().enabled = false;
            }
        }

        /*
        string fullPath = Path.Combine(Application.persistentDataPath, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        string tempPath = fullPath + ".csv";

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = string.Join("\n", mineValues.Select((value, index) => $"{index},{value}"));

            // Write to a temporary file first
            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 2097152, useAsync: true))
            using (StreamWriter writer = new StreamWriter(stream)) {
                writer.WriteAsync(dataToStore);
            }

            // Replace the original file with the temporary file
            // If the original file exists, replace it. Otherwise, move the temp file.
            if (File.Exists(fullPath)) {
                File.Replace(tempPath, fullPath, null);
            } else {
                File.Move(tempPath, fullPath);
            }
        } 
        catch (System.Exception ex) {
            Debug.Log($"Error when trying to save data to file: {ex.Message}");
        }
        */
    }
}