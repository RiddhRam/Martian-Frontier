using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

public class HaulerAINavigation : MonoBehaviour
{
    [SerializeField] Transform target;
    float constantSpeed;
    NavMeshAgent agent;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Disable automatic updates so we can manually control movement and rotation.
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        constantSpeed = transform.GetChild(0).GetComponent<HaulerController>().GetPlayerSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        // Let the agent compute the path.
        agent.SetDestination(new(4.5f, 5.4f, 0));

        // Calculate the normalized direction toward the steering target.
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;

        // Manually update the position with constant speed.
        transform.position += direction * constantSpeed * Time.deltaTime;
        
        // Keep the agent's internal position in sync.
        agent.nextPosition = transform.position;

        // Calculate 2D rotation (only around Z axis).
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
}
