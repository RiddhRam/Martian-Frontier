using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class HoldButton : MonoBehaviour,
                           IPointerDownHandler,
                           IPointerExitHandler
{
    /// <summary>Action executed while the pointer is held down.</summary>
    public Func<bool> OnHold;
    const float initialDelay = 0.8f;
    const float repeatRate   = 0.1f;

    Coroutine _loop;


    public void OnPointerDown(PointerEventData _) { _loop = StartCoroutine(HoldLoop()); }
    //public void OnPointerUp  (PointerEventData _)   => StopHold();
    public void OnPointerExit(PointerEventData _)   => StopHold();

    IEnumerator HoldLoop()
    {

        yield return new WaitForSecondsRealtime(initialDelay);

        while (true)
        {
            // If there was an issue with executing the function, then stop executing
            if (!Fire())
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(repeatRate);
        }
    }

    bool Fire()
    {
        return OnHold.Invoke();
    }

    void StopHold()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    /// All function must return a bool
    public void SetAction(Func<bool> action) => OnHold = action;
}