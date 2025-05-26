using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class HoldButton : MonoBehaviour,
                           IPointerDownHandler,
                           IPointerUpHandler,
                           IPointerExitHandler
{
    /// <summary>Action executed while the pointer is held down.</summary>
    public Action OnHold;
    const float initialDelay = 0.8f;
    const float repeatRate   = 0.1f;

    Coroutine _loop;

    bool heldDown = false;

    public void OnPointerDown(PointerEventData _) { _loop = StartCoroutine(HoldLoop()); }
    public void OnPointerUp  (PointerEventData _)   => StopHold();
    public void OnPointerExit(PointerEventData _)   => StopHold();

    IEnumerator HoldLoop()
    {
        yield return new WaitForSecondsRealtime(initialDelay);
        while (true)
        {
            Fire();
            yield return new WaitForSecondsRealtime(repeatRate);
        }
    }

    void Fire() => OnHold?.Invoke();

    void StopHold()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    /// <summary>Assign action at runtime if desired.</summary>
    public void SetAction(Action action) => OnHold = action;
}