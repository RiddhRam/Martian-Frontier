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
    private float[] materialProfitMultipliers;
    [SerializeField]
    private int maxMaterials;
    [SerializeField]
    // This is how much of the battery each material of this hauler will use. The compacter hauler uses half the amount as the others ones
    private float materialEnergyUsage;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private float profitMultiplier;
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
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();

        materialNames = mineRenderer.oreDelegation.materialNames;
        materialsDelegator = mineRenderer.materialsDelegator;
    }

    void Start() {
        floatingText = transform.GetChild(0).gameObject;

        if (materialCount == null || materialCount.Length != materialNames.Length) {
            materialCount = new int[materialNames.Length];
        }
        if (materialProfitMultipliers == null || materialProfitMultipliers.Length != materialNames.Length) {
            materialProfitMultipliers = new float[materialNames.Length];
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
            float materialProfitMultiplier = materialManager.drillProfitMultiplier;

            // If max material limit of the hauler is exceeded then just reduce the count of the material
            if (amountPickedUp + GetTotalMaterialCount() > maxMaterials) {
                // Only pick up what we can
                amountPickedUp = maxMaterials - GetTotalMaterialCount();
                // then update the profit multplier and material count
                UpdateMaterialProfitMultiplierIndex(i, materialProfitMultiplier, amountPickedUp);
                materialCount[i] += amountPickedUp;

                if (amountPickedUp == 0) {
                    return;
                }

                // Reduce the count of the material
                materialManager.SetCount(materialManager.count - amountPickedUp);

                mineRenderer.currentMineValue -= UIDelegation.materialPrices[materialManager.materialIndex] * amountPickedUp;
                mineRenderer.mineValueText.text = mineRenderer.FormatPrice(mineRenderer.currentMineValue);

                materialsDelegator.UpdateMaterial(materialManager);
                PickUpOre(amountPickedUp);
                return;
            }

            // If limit isn't exceeded then destroy the game object
            UpdateMaterialProfitMultiplierIndex(i, materialProfitMultiplier, amountPickedUp);
            materialCount[i] += amountPickedUp;

            mineRenderer.ReturnMaterialObject(other.gameObject, i, materialManager.id);
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

        // If value is 0, reset the average multiplier to 0
        for (int i = 0; i != materialCount.Length; i++) {
            if (materialCount[i] == 0) {
                materialProfitMultipliers[i] = 0;
            }
        }

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

    public void SetProfitMultiplier(float newProfitMultiplier) {
        this.profitMultiplier = newProfitMultiplier;
    }

    public float GetProfitMultiplier() {
        return profitMultiplier;
    }
    
    public void SetMaterialProfitMultipliers(float[] newMaterialProfitMultipliers) {
        this.materialProfitMultipliers = newMaterialProfitMultipliers;
    }

    public float[] GetMaterialProfitMultipliers() {
        return materialProfitMultipliers;
    }

    public void UpdateMaterialProfitMultiplierIndex(int materialIndex, float newProfitMultiplier, int newCount) {
        float totalValue = materialCount[materialIndex] * materialProfitMultipliers[materialIndex];

        totalValue += newCount * newProfitMultiplier;

        materialProfitMultipliers[materialIndex] = totalValue / (materialCount[materialIndex] + newCount);
    }
}