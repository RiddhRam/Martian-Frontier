using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickMovement : MonoBehaviour
{

    public GameObject joystick;
    public GameObject joystickBG;
    public Vector2 joystickVec;
    private Vector2 joystickTouchPos;
    private Vector2 joystickOriginalPos;
    private float joystickRadius;
    private Camera mainCamera;
    private RectTransform myRectTransform;

    // Start is called before the first frame update
    void Start()
    {
        joystickOriginalPos = Vector3.zero;
        joystickRadius = joystickBG.GetComponent<RectTransform>().sizeDelta.y / 4;

        mainCamera = Camera.main;
        myRectTransform = transform.parent.parent.GetComponent<RectTransform>();
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

        joystick.SetActive(true);
        joystickBG.SetActive(true);
    }

    public void Drag(BaseEventData baseEventData) {

        if (Input.touchCount >= 2) {
            // User is zooming so reset the joystick
            PointerUp();
            return;
        }
        
        PointerEventData pointerEventData = baseEventData as PointerEventData;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            myRectTransform,   // The Canvas RectTransform
            pointerEventData.position, 
            mainCamera,   // The UI Camera
            out localPoint
        );

        //myRectTransform.anchoredPosition = pointerEventData.position / myCanvas.scaleFactor;
        Vector2 dragPos = localPoint;
        joystickVec = (dragPos - joystickTouchPos).normalized;

        float joystickDist = Vector2.Distance(dragPos, joystickTouchPos);

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