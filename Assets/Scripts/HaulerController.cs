using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro
using System.Collections;

public class HaulerController : MonoBehaviour
{
    private string[] materialNames;
    [SerializeField]
    // Initializes array with all values at 0
    private int[] materialCount = new int[3];
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
    private DataPersistenceManager dataPersistenceManager;

    void Start() {
        floatingText = transform.GetChild(0).gameObject;
        materialNames = GameObject.Find("Mine").GetComponent<MineRenderer>().GetMaterialNames();
        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
        dataPersistenceManager = GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure it's a material
        if (!other.CompareTag("Material Tag")) {
            return;
        }

        // Check for the material's index
        MaterialManager materialManager = other.GetComponent<MaterialManager>();
        string materialName = materialManager.materialName;

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

                ShowFloatingText(amountPickedUp);
                // Reduce the count of the material
                materialManager.SetCount(materialManager.count - amountPickedUp);
                materialsDelegator.UpdateMaterial(materialManager);
                return;
            }

            // If limit isn't exceeded then destroy the game object
            materialCount[i] += amountPickedUp;
            // Show floating text
            ShowFloatingText(amountPickedUp);
            materialsDelegator.RemoveMaterial(materialManager.id);
            Destroy(other.gameObject); // Destroy the material object
        }
        //dataPersistenceManager.SaveGame();
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
}