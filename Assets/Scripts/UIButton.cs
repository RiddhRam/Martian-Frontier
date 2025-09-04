using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    public float shrinkFactor = 0.95f; // Scale factor when shrinking
    public float shrinkSpeed = 10f;  // How fast the scaling happens

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(Shrink());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ResetScale());
    }

    private IEnumerator Shrink()
    {
        while (rectTransform.localScale.magnitude > (originalScale * shrinkFactor).magnitude)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale * shrinkFactor, Time.deltaTime * shrinkSpeed);
            yield return null;
        }
    }

    public IEnumerator ResetScale()
    {
        // If cargo button has not been active at least once, it's rectTransform is null 
        // since it's not initalized so dont need to do this
        if (!rectTransform) {
            yield break;
        }
        
        while (rectTransform.localScale.magnitude < originalScale.magnitude)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale, Time.deltaTime * shrinkSpeed);
            yield return null;
        }
    }
}
