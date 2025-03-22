using UnityEngine;
using UnityEngine.AI;

public class HaulerAINavigation : MonoBehaviour
{

    public NavMeshAgent agent;
    public JoystickMovement joystickMovement;
    public NPCMovement nPCMovement;

    private Vector3 direction = Vector3.zero;
    public Vector2 target;
    private Transform frontWheels;
    private float distance;
    
    
    void OnEnable()
    {
        agent.agentTypeID =  NavMesh.GetSettingsByIndex(1).agentTypeID;
        
        // Disable automatic updates so we can manually control movement and rotation.
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        Transform vehicle = transform.GetChild(0);

        for (int i = 0; i != vehicle.childCount; i++) {
            if (vehicle.GetChild(i).name == "Front Wheels") {
                frontWheels = vehicle.GetChild(i);
                break;
            }
        }

        for (int i = 0; i != frontWheels.childCount; i++) {
            frontWheels.GetChild(i).GetComponent<PolygonCollider2D>().enabled = false;
        }

        transform.GetChild(0).GetComponent<HaulerController>().IncreaseMaxMaterials();
    }

    // Update is called once per frame
    void Update()
    {
        // Let the agent compute the path.
        agent.SetDestination(new(target.x, target.y, 0));

        // Calculate the normalized direction toward the steering target.
        //Debug.Log($"{agent.steeringTarget} vs {transform.position}");

        distance = Vector3.Distance(transform.position, agent.steeringTarget);

        if (distance < 0.2f) {
            agent.nextPosition = transform.position;
        } else {
            direction = (agent.steeringTarget - transform.position).normalized;
            /* 
            Vector3 toTarget = (agent.steeringTarget - transform.position).normalized;
            Vector3 offset = Vector3.Cross(toTarget, Vector3.forward) * 0.1f; // Adjust 0.5f for a wider gap
            direction = (agent.steeringTarget + offset - transform.position).normalized;
            */
        }

        joystickMovement.UpdateJoystickVector(direction, 1f);
    }

}