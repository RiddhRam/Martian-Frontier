using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickMovement : MonoBehaviour
{

    public GameObject joystick;

    public GameObject joystickBG;
    public Vector2 joystickVec;
    public Vector2 joystickTouchPos;
    private Vector2 joystickOriginalPos;
    private float joystickRadius;

    // Start is called before the first frame update
    void Start()
    {
        joystickOriginalPos = joystickBG.transform.position;
        joystickRadius = joystickBG.GetComponent<RectTransform>().sizeDelta.y / 4;
    }

    public void PointerDown() {
        if (Input.touchCount < 2) {
            joystick.transform.position = Input.mousePosition;
            joystickBG.transform.position = Input.mousePosition;
            joystickTouchPos = Input.mousePosition;
        } else {
            // User is zooming so reset the joystick
            PointerUp();
        }
    }

    public void Drag(BaseEventData baseEventData) {
        if (Input.touchCount < 2) {
            PointerEventData pointerEventData = baseEventData as PointerEventData;
            Vector2 dragPos = pointerEventData.position;
            joystickVec = (dragPos - joystickTouchPos).normalized;

            float joystickDist = Vector2.Distance(dragPos, joystickTouchPos);

            if (joystickDist < joystickRadius) {
                joystick.transform.position = joystickTouchPos + joystickVec * joystickDist;
            } else {
                joystick.transform.position = joystickTouchPos + joystickVec * joystickRadius;
            }

        } else {
            // User is zooming so reset the joystick
            PointerUp();
        }
    }

    public void PointerUp() {
        // User let go so reset the joystick
        joystickVec = Vector2.zero;
        joystick.transform.position = joystickOriginalPos;
        joystickBG.transform.position = joystickOriginalPos;
    }
}