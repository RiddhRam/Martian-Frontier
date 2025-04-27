using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro
using System.Collections;

public class MagnetHauler : MonoBehaviour
{
    [SerializeField] private CreditsDelegator creditsDelegator;
    [SerializeField] private TextMeshProUGUI creditCounterText;

    [SerializeField] private float playerSpeed;

    public int collectedCredits;

    private GameObject floatingText; // Display the amount picked up
    // This never gets reset back to 0, it just keeps going up, but I don't think it will be an issue
    private int concurrentFadeEvents = 0;

    private AudioSource vehicleSoundEffects;
    private AudioClip orePickUpSoundEffect;
    private AudioDelegator audioDelegator;

    private readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

    void Start() {
        floatingText = transform.GetChild(0).gameObject;

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        orePickUpSoundEffect = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().oreSaleSoundEffect;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure it's a material
        if (!other.CompareTag("Material Tag")) {
            return;
        }

        // Check for the material's index
        CreditMaterialsInfo creditMaterial = other.GetComponent<CreditMaterialsInfo>();

        int amountPickedUp = creditMaterial.count;
        UpdateCreditCount(amountPickedUp);

        PickUpOre(amountPickedUp);
        creditsDelegator.ReturnCreditGameObject(other.gameObject);
    }

    public void UpdateCreditCount(int newAmount) {
        collectedCredits += newAmount;
        creditCounterText.text = collectedCredits.ToString();
    }

    private void PickUpOre(int amountPickedUp) {
        ShowFloatingText(amountPickedUp.ToString());
        PlayAudio();
    }    

    public void ShowFloatingText(string amount)
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
        float fadeDuration = 1f;
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

    public float GetPlayerSpeed() {
        return playerSpeed;
    }
   
    private void PlayAudio() {
        audioDelegator.PlayAudio(vehicleSoundEffects, orePickUpSoundEffect, 0.4f);
    }

}