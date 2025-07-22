using UnityEngine;

public class JoystickRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    public RectTransform joystickHandle;
    const float blockRadius = 3.5f;

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (eventCamera == null)
            eventCamera = Camera.main;

        // build a world‐space ray from the screen point
        Ray ray = eventCamera.ScreenPointToRay(sp);

        foreach (var npc in NPCManager.Instance.npcs)
        {
            if (npc == null) break;

            // project npc position onto the ray
            Vector3 toNpc = npc.transform.position - ray.origin;
            float t = Vector3.Dot(toNpc, ray.direction);
                
            // find the closest point on the ray to the npc
            Vector3 closestPoint = ray.origin + ray.direction * t;

            // if within blockRadius, block the UI tap
            if (Vector3.Distance(closestPoint, npc.transform.position) <= blockRadius)
            {
                return false;
            }
            
        }

        // no NPC too close — allow the UI event
        return true;
    }
}
