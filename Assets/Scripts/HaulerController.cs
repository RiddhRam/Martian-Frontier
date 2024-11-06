using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro
using System.Collections;
using System;

public class HaulerController : MonoBehaviour
{
    private readonly string[] materialNames = { "Limestone", "Sulfur", "Iron" };
    public int[] materialCount = new int[3];
    private GameObject floatingText; // Display the amount picked up
    private int concurrentFadeEvents = 0;

    void Start() {
        floatingText = transform.GetChild(0).gameObject;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure it's a material
        if (other.CompareTag("Material Tag"))
        {
            // Check for the material's index
            MaterialManager materialManager = other.GetComponent<MaterialManager>();
            string materialName = materialManager.materialName;

            for (int i = 0; i < materialNames.Length; i++)
            {
                // If it matches, then increase the count at that index and destroy the game object
                if (materialNames[i] == materialName)
                {
                    int amountPickedUp = materialManager.count;
                    materialCount[i] += amountPickedUp;

                    // Show floating text
                    ShowFloatingText(amountPickedUp);
                    Destroy(other.gameObject); // Destroy the material object
                    break;
                }
            }
        }
    }

    private void ShowFloatingText(int amount)
    {
        // Set the text to show the picked up amount
        TextMeshPro textComponent = floatingText.GetComponent<TextMeshPro>();
        textComponent.text = $"+{amount}";

        // Start fading out the text after a delay
        StartCoroutine(FadeOutText(floatingText));
    }

    private IEnumerator FadeOutText(GameObject floatingText)
    {
        TextMeshPro textComponent = floatingText.GetComponent<TextMeshPro>();

        Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

        textComponent.transform.rotation = normalRotation;
        textComponent.alpha = 1;
        
        concurrentFadeEvents++;
        int currentFadeEvents = concurrentFadeEvents;

        // Hold for 0.5 seconds at alpha 1, but keep the rotation straight
        float holdDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < holdDuration) {
            if (concurrentFadeEvents > currentFadeEvents) {
                elapsedTime = 0;
                currentFadeEvents++;
            }
            textComponent.transform.rotation = normalRotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Fade out by reducing alpha over time and also keep rotation straight
        float fadeDuration = 3f;
        float startAlpha = 1;
        elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            if (concurrentFadeEvents > currentFadeEvents) {
                elapsedTime = 0;
                currentFadeEvents++;
            }

            textComponent.transform.rotation = normalRotation;
            textComponent.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
    
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it's fully transparent
        textComponent.alpha = 0f;
    }
}