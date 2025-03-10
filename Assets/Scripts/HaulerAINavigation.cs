using UnityEngine;
using UnityEngine.AI;

public class HaulerAINavigation : MonoBehaviour
{
    [SerializeField] Transform target;

    NavMeshAgent agent;
    public JoystickMovement joystickMovement;
    Vector3 direction = Vector3.zero;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Disable automatic updates so we can manually control movement and rotation.
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Let the agent compute the path.
        agent.SetDestination(new(4.5f, 5.4f, 0));

        // Calculate the normalized direction toward the steering target.
        //Debug.Log($"{agent.steeringTarget} vs {transform.position}");

        float distance = Vector3.Distance(transform.position, agent.steeringTarget);

        direction = (agent.steeringTarget - transform.position).normalized;
        
        if (distance < 0.1f) {
            agent.nextPosition = transform.position;
        } else {
            joystickMovement.UpdateJoystickVector(direction, 1f);
        }
    }
}
