using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickMovement : MonoBehaviour
{

    private static JoystickMovement _instance;
    public static JoystickMovement Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<JoystickMovement>();
            }
            return _instance;
        }
    }

    public GameObject joystick;
    public GameObject joystickBG;
    public NPCMovement nPCMovement;
    public Vector2 joystickVec;
    public Image joystickRaycastImage;
    private Vector2 joystickTouchPos;
    private Vector2 joystickOriginalPos;
    private float joystickRadius;
    PointerEventData pointerEventData;
    private Camera mainCamera;
    private RectTransform myRectTransform;

    // Start is called before the first frame update
    void Start()
    {
        joystickOriginalPos = Vector3.zero;
        joystickRadius = joystickBG.GetComponent<RectTransform>().sizeDelta.y / 4;

        mainCamera = Camera.main;
        myRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    public void PointerDown() {
        if (Input.touchCount >= 2) {
            // User is zooming so reset the joystick
            PointerUp();
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            myRectTransform,   // The Canvas RectTransform
            Input.mousePosition, 
            mainCamera,   // The UI Camera
            out localPoint
        );

        joystick.transform.localPosition = localPoint;
        joystickBG.transform.localPosition = joystick.transform.localPosition;
        joystickTouchPos = joystick.transform.localPosition;

        // Don't display if the player is not controlling a drone
        if (nPCMovement == null)
        {
            return;
        }

        joystick.SetActive(true);
        joystickBG.SetActive(true);
    }

    public void Drag(BaseEventData baseEventData) {

        if (Input.touchCount >= 2) {
            // User is zooming so reset the joystick
            PointerUp();
            return;
        }
        
        pointerEventData = baseEventData as PointerEventData;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            myRectTransform,   // The Canvas RectTransform
            pointerEventData.position, 
            mainCamera,   // The UI Camera
            out localPoint
        );

        Vector2 dragPos = localPoint;
        UpdateJoystickVector((dragPos - joystickTouchPos).normalized, Vector2.Distance(dragPos, joystickTouchPos));

    }

    public void UpdateJoystickVector (Vector3 normalizedVector, float distance) {
        joystickVec = normalizedVector;

        float joystickDist = distance;

        if (joystickDist < joystickRadius) {
            joystick.transform.localPosition = joystickTouchPos + joystickVec * joystickDist;
        } else {
            joystick.transform.localPosition = joystickTouchPos + joystickVec * joystickRadius;
        }
    }

    public void PointerUp() {
        joystick.SetActive(false);
        joystickBG.SetActive(false);

        // User let go so reset the joystick
        joystickVec = Vector2.zero;
        joystick.transform.localPosition = joystickOriginalPos;
        joystickBG.transform.position = joystickOriginalPos;
    }
}