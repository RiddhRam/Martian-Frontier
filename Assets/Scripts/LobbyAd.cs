using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyAd : MonoBehaviour
{
    public AdDelegator adDelegator;
    public RefineryController refineryController;
    public GameObject SuccessText;
    public TextMeshPro SuccessTextMeshPro;
    private bool collectedReward = false;

    void OnTriggerEnter2D() {
        if (collectedReward) {
            return;
        }
        adDelegator.ShowLobbyRewardedAd();
    }

    public void ShowRewardSuccess(string rewardAmount) {
        if (collectedReward) {
            return;
        }

        refineryController.PlaySaleNoise();
        SuccessText.SetActive(true);
        ShowFloatingText(rewardAmount);
        collectedReward = true;
    }

    public void ShowFloatingText(string amount)
    {
        SuccessTextMeshPro.text = $"+${amount}";
        // Start fading out the text after a delay
        StartCoroutine(FadeOutText(SuccessText));
    }

    private IEnumerator FadeOutText(GameObject floatingText)
    {
        TextMeshPro textComponent = floatingText.GetComponent<TextMeshPro>();

        textComponent.alpha = 1;

        // Hold for 0.5 seconds at alpha 1, but keep the rotation straight
        float holdDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < holdDuration) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Fade out by reducing alpha over time and also keep rotation straight
        float fadeDuration = 1f;
        float startAlpha = 1;
        elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            textComponent.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
    
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it's fully transparent
        textComponent.alpha = 0f;
        collectedReward = false;
        gameObject.SetActive(false);
    }

}
