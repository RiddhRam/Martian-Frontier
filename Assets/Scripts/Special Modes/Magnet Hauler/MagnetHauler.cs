using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro
using System.Collections;

public class MagnetHauler : MonoBehaviour
{
    [SerializeField] private CreditMagnet creditMagnet;
    [SerializeField] private CreditsDelegator creditsDelegator;
    [SerializeField] private TextMeshProUGUI creditCounterText;

    [SerializeField] private Transform magnetArea;

    public int collectedCredits;

    private GameObject floatingText; // Display the amount picked up
    // This never gets reset back to 0, it just keeps going up, but I don't think it will be an issue
    private int concurrentFadeEvents = 0;

    private AudioSource vehicleSoundEffects;
    private AudioClip orePickUpSoundEffect;

    private readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

    private LineRenderer lineRenderer;
    [SerializeField] private Color lineRendererColor;
    [SerializeField] private Material defaultMaterial;
    readonly float multplier = 360f / 50;

    void Start() {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;

        lineRenderer.loop = true;
        lineRenderer.positionCount = 50;
        lineRenderer.material = defaultMaterial;
        lineRenderer.sortingOrder = 1;
        lineRenderer.startColor = lineRendererColor;
        lineRenderer.endColor = lineRendererColor;   

        floatingText = transform.GetChild(0).gameObject;

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        orePickUpSoundEffect = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().oreSaleSoundEffect;

        // Do this so front wheels are found
        PlayerMovement playerMovement = transform.parent.GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(playerMovement.GetSpeed());
    }

    void Update()
    {
        DrawRing();
    }

    void DrawRing()
    {
        float radiusToUse = creditMagnet.magnetRadius;

        for (int i = 0; i < 50; i++)
        {
            float angle = i * multplier * Mathf.Deg2Rad;

            float x = Mathf.Cos(angle) * radiusToUse;
            float y = Mathf.Sin(angle) * radiusToUse;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0) + transform.position);
        }

        float radius = radiusToUse * 2;
        magnetArea.localScale = new(radius, radius, radius);
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
   
    private void PlayAudio() {
        AudioDelegator.Instance.PlayAudio(vehicleSoundEffects, orePickUpSoundEffect, 0.3f);
    }

}