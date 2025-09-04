using UnityEngine;

public class CreditMagnet : MonoBehaviour
{
    [SerializeField] private OreMagnetRoundManager oreMagnetRoundManager;

    public float magnetRadius;
    public float pullForce;

    private readonly float minimumDistance = 0.1f; // Prevents division by zero or excessive force
    private readonly float checkInterval = 0.1f; // Check 10 times per second

    private float checkTimer = 0f;
    private Collider2D[] nearbyColliders;

    void Update()
    {
        if (!oreMagnetRoundManager.roundInProgress) {
            return;
        }
        
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            FindNearbyTargets();
            checkTimer = checkInterval;
        }

        // Apply pull force every frame to the objects found in the last scan
        PullTargets();
    }

    void FindNearbyTargets()
    {
        nearbyColliders = Physics2D.OverlapCircleAll(transform.position, magnetRadius);
    }

    void PullTargets()
    {
        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Collider2D hitCollider = nearbyColliders[i];

            if (!hitCollider.gameObject.activeSelf) {
                continue;
            }

            // Check if the found collider has the correct tag
            if (hitCollider.CompareTag("Material Tag"))
            {
                // Calculate direction from the target towards the magnet
                Vector3 directionToMagnet = transform.position - hitCollider.transform.position;

                // Calculate the distance
                float distance = directionToMagnet.magnitude;

                // Only apply force if the object is within radius and further than the minimum distance
                if (distance < magnetRadius && distance > minimumDistance)
                {
                    // This formula makes the strength proportional to (1 - distance/radius),
                    // so it's max strength near the center and zero at the edge.
                    float pullStrength = pullForce * (1.0f - (distance / magnetRadius));

                    Vector3 normalizedDirection = directionToMagnet.normalized;

                    Vector3 movement = normalizedDirection * pullStrength * Time.deltaTime;
                    hitCollider.transform.position += movement;
                }
            }
        }
    }
}