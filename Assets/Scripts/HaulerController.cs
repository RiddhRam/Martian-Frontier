using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro
using System.Collections;
using UnityEngine.UI;

public class HaulerController : MonoBehaviour
{
    private string[] materialNames;
    [SerializeField]
    // Initializes array with all values at 0
    private int[] materialCount;
    [SerializeField]
    private int maxMaterials;
    [SerializeField]
    // This is how much of the battery each material of this hauler will use. The compacter hauler uses half the amount as the others ones
    private float materialEnergyUsage;
    [SerializeField]
    private float playerSpeed;
    private GameObject floatingText; // Display the amount picked up
    // This never gets reset back to 0, it just keeps going up, but I don't think it will be an issue
    private int concurrentFadeEvents = 0;
    // Does nothing, just for the Garage
    public int width;
    [SerializeField]
    private long price;
    private UncollectedMaterialsDelegator materialsDelegator;
    private AudioSource vehicleSoundEffects;
    private AudioClip orePickUpSoundEffect;
    private AudioDelegator audioDelegator;
    private UIDelegation UIDelegation;
    private MineRenderer mineRenderer;
    private GameObject[] cargoProgressBars = new GameObject[2];
    private GameObject[] cargoCounters = new GameObject[2];

    void Awake() {
        UIDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();
        mineRenderer = GameObject.Find("Mine Renderer").GetComponent<MineRenderer>();
    }

    void Start() {
        floatingText = transform.GetChild(0).gameObject;
        materialNames = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().materialNames;
        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
        if (materialCount == null || materialCount.Length != materialNames.Length) {
            materialCount = new int[materialNames.Length];
        }

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        orePickUpSoundEffect = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().orePickupSoundEffect;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();
        UpdateCargoUI();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure it's a material
        if (!other.CompareTag("Material Tag")) {
            return;
        }

        // Check for the material's index
        MaterialManager materialManager = other.GetComponent<MaterialManager>();
        string materialName = materialManager.materialName.ToUpper();

        for (int i = 0; i < materialNames.Length; i++)
        {
            // If not matching
            if (materialNames[i] != materialName)
            {
                continue;
            }
            // If it matches
            int amountPickedUp = materialManager.count;

            // If max material limit of the hauler is exceeded then just reduce the count of the material
            if (amountPickedUp + GetTotalMaterialCount() > maxMaterials) {
                // Only pick up what we can
                amountPickedUp = maxMaterials - GetTotalMaterialCount();
                // then increase the count at that index
                materialCount[i] += amountPickedUp;

                if (amountPickedUp == 0) {
                    return;
                }

                // Reduce the count of the material
                materialManager.SetCount(materialManager.count - amountPickedUp);
                materialsDelegator.UpdateMaterial(materialManager);
                PickUpOre(amountPickedUp);
                return;
            }

            // If limit isn't exceeded then destroy the game object
            materialCount[i] += amountPickedUp;
            mineRenderer.ReturnObject(other.gameObject, i, materialManager.id);
            PickUpOre(amountPickedUp);
        }
    }

    private void PickUpOre(int amountPickedUp) {
        ShowFloatingText(amountPickedUp.ToString());
        PlayAudio();
        UpdateCargoUI();
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

    public int GetTotalMaterialCount() {
        int count = 0;
        for (int i = 0; i != materialCount.Length; i++) {
            count += materialCount[i];
        }
        return count;
    }

    public int GetMaxMaterials() {
        return maxMaterials;
    }

    public int[] GetMaterialCount() {
        return materialCount;
    }

    public void SetMaterialCount(int[] newMaterialCount) {
        materialCount = newMaterialCount;

        UpdateCargoUI();
    }

    public float GetMaterialEnergyUsage() {
        return materialEnergyUsage;
    }

    public float GetPlayerSpeed() {
        return playerSpeed;
    }

    public long GetPrice() {
        return price;
    }

    private void PlayAudio() {
        audioDelegator.PlayAudio(vehicleSoundEffects, orePickUpSoundEffect, 0.6f);
    }

    public void UpdateCargoUI() {
        if (cargoProgressBars[0] == null) {
            cargoProgressBars = UIDelegation.GetCargoProgressBars();
            cargoCounters = UIDelegation.GetCargoCounters();
        }

        for (int i = 0; i != cargoProgressBars.Length; i++) {
            cargoProgressBars[i].GetComponent<Slider>().maxValue = maxMaterials;
            cargoProgressBars[i].GetComponent<Slider>().value = GetTotalMaterialCount();
            cargoCounters[i].GetComponent<TextMeshProUGUI>().text = GetTotalMaterialCount().ToString();
        }
    }
}