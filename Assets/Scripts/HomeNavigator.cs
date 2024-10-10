using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HomeNavigator : MonoBehaviour
{
    public GameObject gameSaveCanvas;
    private readonly float fadeDuration = 0.25f;

    void Start() {
        // Initialize the app properly
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public void GoToGameSaves() {
        TransitionFromHome();

        TransitionNewScreen(gameSaveCanvas);
    }

    public void TransitionFromHome() {
        // Loop through each child
        foreach (Transform child in transform)
        {
            GameObject childGameObject = child.gameObject;
 
            StartCoroutine(FadeOut(childGameObject));

        }
    }

    public void TransitionNewScreen(GameObject obj) {
        obj.SetActive(true);
        StartCoroutine(FadeIn(obj));
    }

    IEnumerator FadeOut(GameObject obj)
    {
        Button buttonComponent = obj.GetComponent<Button>();

        // If it's a button, make it uninteractable
        if (buttonComponent) {
            buttonComponent.interactable = false;
        }
        
        Image spriteRenderer = obj.GetComponent<Image>();     

        // Get the sprite's initial color
        Color spriteColor = spriteRenderer.color;
        float fadeSpeed = fadeDuration * 4 / fadeDuration;
        float alphaValue = spriteColor.a;

        // Fade out over time
        while (alphaValue > 0.0f)
        {
            alphaValue -= Time.deltaTime * fadeSpeed;
            spriteRenderer.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alphaValue);
            yield return null;
        }

        // Disable the object after fading
        obj.SetActive(false);
    }

    IEnumerator FadeIn(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();     

        // Get the sprite's initial color
        float alphaValue = canvasGroup.alpha;
        float fadeSpeed = fadeDuration * 2 / fadeDuration;

        // Fade out over time
        while (alphaValue < 1.0f)
        {
            alphaValue += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = alphaValue;
            yield return null;
        }

        // Enable buttons and stuff
        obj.GetComponent<CanvasGroup>().interactable = true;
    }

    IEnumerator PushUp(Transform obj) {
        Vector2 startPosition = obj.position;
        float elapsedTime = 0;

        Vector2 targetPosition = new(Screen.width/2, Screen.height - (Screen.height / 13) );

        while (elapsedTime < fadeDuration)
        {
            // Lerp position over time
            obj.position = Vector2.Lerp(startPosition, targetPosition, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
