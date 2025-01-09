using UnityEngine;

public class AIMovement : MonoBehaviour
{
    public string vehicleType = "Driller";
    public int drillTier = 1;
    private Vector3 targetPosition;
    private bool isMoving = false;
    float angle;
    float step;

    // For detecting if the vehicle is stuck
    private Vector3 previousPosition;
    private float stuckTimer = 0f;
    private float stuckThreshold = 3f; // Time threshold for being stuck (3 seconds)
    private float pushDistance = 3f; // Distance to push forward if stuck

    void Start()
    {
        // Set initial target position
        ChooseNewTargetPosition();
        previousPosition = transform.position; // Store initial position
    }

    void Update()
    {
        // Check if y-coordinate is above -5 and reset position if needed
        if (transform.position.y > -5)
        {
            targetPosition = new Vector3(0, -5, 0);
            isMoving = true;
        }

        // Face the direction of movement
        Vector3 direction = (targetPosition - transform.position).normalized;

        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Move towards the target position
        if (isMoving)
        {
            step = 5f * Time.deltaTime; // Adjust speed as needed
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // Check if reached the target position
            if (transform.position == targetPosition)
            {
                isMoving = false;
                ChooseNewTargetPosition();
            }
        }

        // Check if the vehicle is stuck (hasn't moved for 'stuckThreshold' seconds)
        if (transform.position == previousPosition)
        {
            stuckTimer += Time.deltaTime;

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
        targetPosition = new Vector3(randomX, randomY, 0);
        Debug.Log(targetPosition);
        isMoving = true;
    }
}
