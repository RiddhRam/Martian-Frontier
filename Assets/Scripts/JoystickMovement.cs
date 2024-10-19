using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickMovement : MonoBehaviour
{

    public GameObject joystick;

    public GameObject joystickBG;
    public Vector2 joystickVec;
    public Vector2 joystickTouchPos;
    public Vector2 joystickOriginalPos;
    private float joystickRadius;

    // Start is called before the first frame update
    void Start()
    {
        joystickOriginalPos = joystickBG.transform.position;
        joystickRadius = joystickBG.GetComponent<RectTransform>().sizeDelta.y / 4;
    }

    public void PointerDown() {
        joystick.transform.position = Input.mousePosition;
        joystickBG.transform.position = Input.mousePosition;
        joystickTouchPos = Input.mousePosition;
    }

    public void Drag(BaseEventData baseEventData) {
        PointerEventData pointerEventData = baseEventData as PointerEventData;
        Vector2 dragPos = pointerEventData.position;
        joystickVec = (dragPos - joystickTouchPos).normalized;

        float joystickDist = Vector2.Distance(dragPos, joystickTouchPos);

        if (joystickDist < joystickRadius) {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickDist;
        } else {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickRadius;
        }
    }

    public void PointerUp() {
        joystickVec = Vector2.zero;
        joystick.transform.position = joystickOriginalPos;
        joystickBG.transform.position = joystickOriginalPos;
    }
}

/*
if anyone is using multitouch and also has the panel that parents the object covering half of the screen, in my case the right part, you will see that when you try it out and touch first the part of the screen that the panel doesnt cover and then holding you touch the one with the panel, you will see that the joystick moves to a position right in the middle of your fingers, because Input.mousePostion does the averege of all Inputs, so what you need is  to get Input.touches which is the array of all touches and then figure out how to get the right finger.id by sorting the positions of each input inside the array, either by X or Y (depends in what part of the screen you want the joystick to be), then you will be able to tell which touch is more to the right, left, up or down, again depends of what you want, and you can set your joystick position to that touch position we just distinguished from the others, because that is the one we wanted. It might sound hard if you dont understand where the problem is.
Anyway I'll share the code for you to try if you had the same issue I did. This is the solution I came up with. maybe I over thought and the problem was much easier to resolve, if that please let me know hahahaha


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject joystick;
    public GameObject joystickBG;
    public Vector2 joystickVec;
    private Vector2 joystickTouchPos;
    private Vector2 joystickOriginalPos;
    private float joystickRadius;
    public float radius = 4;

    void Start()
    {
        joystickOriginalPos = joystickBG.transform.position;
        joystickRadius = joystickBG.GetComponent<RectTransform>().sizeDelta.y / radius;
    }
    public void PointerDown()
    {
        int i = Input.touches.Length;;

        if (i == 1)
        {
            joystick.transform.position = Input.mousePosition;
            joystickBG.transform.position = Input.mousePosition;
            joystickTouchPos = Input.mousePosition;
        }
        else
        {
            int id = SortArray();
            joystick.transform.position = Input.touches[id].position;
            joystickBG.transform.position = Input.touches[id].position;
            joystickTouchPos = Input.touches[id].position;
        }
            
        
    }
    private int SortArray()
    {
        Touch[] inputlist = Input.touches;
        float[] listpositioninputX = new float[inputlist.Length];
        int id = 0;
        for (int i = 0; i < inputlist.Length; i++)
        {
            listpositioninputX[i] = inputlist[i].position.x;
        }
        Comparison<float> compare = new Comparison<float>((numero1, numero2) => numero1.CompareTo(numero2));
        Array.Sort<float>(listpositioninputX, compare);

        for (int i = 0; i < inputlist.Length; i++)
        {
            if (listpositioninputX[0] == inputlist[i].position.x)
            {
                return id = inputlist[i].fingerId;
            }
        }
        return id;

    }
    public void Drag(BaseEventData baseEventData)
    {
        PointerEventData pointerEventData = baseEventData as PointerEventData;
        Vector2 dragPos = pointerEventData.position;

        joystickVec = (dragPos - joystickTouchPos).normalized;
        
        float joystickDist = Vector2.Distance(dragPos, joystickTouchPos);

        if (joystickDist < joystickRadius)
        {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickDist;
        }
        else
        {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickRadius;
        }
    }
    public void PointerUp()
    {
        joystickVec = Vector2.zero;
        joystick.transform.position = joystickOriginalPos;
        joystickBG.transform.position = joystickOriginalPos;
    }
    
}
*/