using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackToHomeMenu : MonoBehaviour
{
    public GameObject menuCanvas;

    private readonly float fadeDuration = 0.25f;

    public void GoBackToHome() {

        // Disable the buttons
        GetComponent<CanvasGroup>().interactable = false;

        // Transition from Page
        StartCoroutine(FadeOut());

        StartCoroutine(FadeInAwaitCoroutine());
    }

    public IEnumerator FadeInAwaitCoroutine() {
        // Collect FadeIn coroutines for all menuCanvas children
        List<Coroutine> fadeInCoroutines = new List<Coroutine>();

        foreach (Transform child in menuCanvas.GetComponentsInChildren<Transform>(true))
        {
            GameObject childGameObject = child.gameObject;
            fadeInCoroutines.Add(StartCoroutine(FadeIn(childGameObject)));

        }

        foreach (Coroutine fadeinCoroutine in fadeInCoroutines) {
            yield return fadeinCoroutine;
        }
 
        gameObject.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>();     

        // Get the sprite's initial color
        float alphaValue = canvasGroup.alpha;
        float fadeSpeed = fadeDuration * 2 / fadeDuration;

        // Fade out over time
        while (alphaValue > 0.0f)
        {
            alphaValue -= Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = alphaValue;
            yield return null;
        }
    }

    IEnumerator FadeIn(GameObject obj)
    {
        obj.SetActive(true);

        Image spriteRenderer = obj.GetComponent<Image>();   

        // If child has an image component
        if (spriteRenderer) {
            // Get the sprite's initial color
            Color spriteColor = spriteRenderer.color;
            float fadeSpeed = fadeDuration * 2 / fadeDuration;
            float alphaValue = spriteColor.a;

            // Fade in over time
            while (alphaValue < 1.0f)
            {
                alphaValue += Time.deltaTime * fadeSpeed;
                spriteRenderer.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alphaValue);
                yield return null;
            }
        }

        Button buttonComponent = obj.GetComponent<Button>();

        if (buttonComponent) {
            buttonComponent.interactable = true;
        }

    }

}
