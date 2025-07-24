using System.Collections;
using TMPro;
using UnityEngine;

public class ErrorMessageHandler : MonoBehaviour
{
    public AudioClip errorAudio;
    private AudioSource UISoundEffects;
    private TMP_Text tmpText;
    private RectTransform rectTransform;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();

        // Set anchors to stretch across the screen horizontally
        rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y); // Left edge
        rectTransform.anchorMax = new Vector2(1f, rectTransform.anchorMax.y); // Right edge
        UISoundEffects = GameObject.Find("UI Sound Effects").GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(AnimateMessage());
        AudioDelegator.Instance.PlayAudio(UISoundEffects, errorAudio, 0.4f);
    }

    private IEnumerator AnimateMessage()
    {
        float duration = 4f; // Total duration of the animation
        float elapsedTime = 0f;
        Vector3 initialPosition = rectTransform.anchoredPosition;
        Vector3 targetPosition = initialPosition + new Vector3(0, 200, 0); // Move 200 pixels upwards

        bool fadeStarted = false; // Track if we’ve delayed fading

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Animate position upwards (always animate)
            rectTransform.anchoredPosition = Vector3.Lerp(initialPosition, targetPosition, t);

            // Start fading only after 1 second has passed
            if (elapsedTime > 1f && !fadeStarted)
            {
                fadeStarted = true; // Allow fading to start
            }

            if (fadeStarted)
            {
                // Animate alpha
                Color color = tmpText.color;
                color.a = Mathf.Lerp(1, 0, (elapsedTime - 1f) / (duration - 1f));
                tmpText.color = color;
            }

            yield return null;
        }

        // Ensure final position and alpha are exactly set
        rectTransform.anchoredPosition = targetPosition;
        Color finalColor = tmpText.color;
        finalColor.a = 0;
        tmpText.color = finalColor;

        Destroy(gameObject);
    }

}