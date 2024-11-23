using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RefineryController : MonoBehaviour, IDataPersistence
{
    public GameObject mineEntrance;
    public Sprite mineEntranceOn;
    public Sprite mineEntranceOff;
    public GameObject generationTriggers;
    public GameObject mine;
    public GameObject refineryProgressSliderWorld;
    public GameObject refineryProgressSliderUI;
    public GameObject refineryProgressSliderUIPercentageText;
    public GameObject playerState;

    [SerializeField]
    private float refineryBattery;
    [SerializeField]
    private float initialBattery;
    [SerializeField]
    private float refineryInefficiency = 1;

    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    // REMEMBER TO UPDATE IN PlayerState TOO
    private readonly int[] materialPrices = {50, 150, 250};
    public GameObject capacityUpgrades;
    public GameObject efficiencyUpgrades;

    void OnTriggerEnter2D(Collider2D collision)
    {
        HaulerController haulerController = collision.gameObject.GetComponent<HaulerController>();

        // Make sure it was a hauler that collides, only haulers will have a HaulerController
        if (!haulerController) {
            return;
        }
        
        int[] materialCount = haulerController.GetMaterialCount();

        // Track what's being added so we can verify the cash amount
        int[] savedMaterialCount = new int[materialCount.Length];

        // Refinery each ore by reducing refinery battery and adding money to user's account
        for (int i = 0; i != materialCount.Length; i++) {
            // Have to start j in the negative because the values will meet in the middle at 0
            // j increases by 1, but materialCount[i] also decreases by 1
            for (int j = -materialCount[i]; j < materialCount[i]; j++) {
                if (refineryBattery != 0) {
                    refineryBattery -= refineryInefficiency;
                    materialCount[i]--;
                    playerState.GetComponent<PlayerState>().NewMaterialSold();
                    savedMaterialCount[i]++;
                    continue;
                }
                break;
            }
        }

        // Calculate how much money to add
        int cashToAdd = 0;
        for (int i = 0; i != savedMaterialCount.Length; i++) {
            cashToAdd += savedMaterialCount[i] * materialPrices[i];
        }

        // Verify that this is the right amount
        playerState.GetComponent<PlayerState>().AddCash(cashToAdd, savedMaterialCount);

        haulerController.SetMaterialCount(materialCount);

        UpdateRefineryProgressBars();

        // Reset the mine if needed
        if (refineryBattery == 0) {
            // Stop user from user dropoff or mine
            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
            StartCoroutine(ResetMine());
        }

        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    private IEnumerator ResetMine() {

        // Disable mine temporarily
        mineEntrance.GetComponent<SpriteRenderer>().sprite = mineEntranceOff;
        // Move player off the dropoff area, and move all players inside the mine to the outside
        GameObject playerVehicle = GameObject.Find("Player Vehicle");
        playerVehicle.transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
        mineEntrance.GetComponent<BoxCollider2D>().enabled = true;

        // Cover the map
        GameObject.Find("Large Fog Of War").transform.position = new(0, -220, 0);

        // Reset the mine
        for (int i = 0; i != mine.transform.childCount; i++) {
            GameObject child = mine.transform.GetChild(i).gameObject;

            // Will certainly run into many nulls since a lot of objects get destroyed
            if (!child) {
                yield break;
            }

            // If a row, row generation trigger, or GenerationTriggers parent
            if (child.name.Contains("Row") || child.name.Contains("Generation")) {
                Destroy(child);
            }
            
            // Destroy all leftover materials, we do it this way, in case someone mined something 
            // just as the mine was shutting down, and the ore didn't have enough time to have 
            // the mine set as its parent
            MaterialManager[] materials = FindObjectsOfType<MaterialManager>();

            foreach (var material in materials) {
                Destroy(material.gameObject);
            }
            
        }

        StartCoroutine(GraduallyIncreaseBattery(initialBattery));

        // Sleep for 3 seconds
        yield return new WaitForSeconds(3);        

        // Create the new mine
        GameObject genTrigGameObject = Instantiate(generationTriggers);
        genTrigGameObject.transform.SetParent(mine.transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigGameObject.name = genTrigGameObject.name.Substring(0, genTrigGameObject.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigGameObject.transform.childCount; i++) {
            genTrigGameObject.transform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(mine);
        }

        mine.GetComponent<MineRenderer>().InitializeMine();
        
        // Renable the mine
        mineEntrance.GetComponent<SpriteRenderer>().sprite = mineEntranceOn;
        mineEntrance.GetComponent<BoxCollider2D>().enabled = false;

        // Let user use dropoff, also flash it in case user was anxiously trying to use it by pressing against it
        gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = true;

        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    private IEnumerator GraduallyIncreaseBattery(float batteryToUse)
    {
        float duration = 3f; // Duration of the increase in seconds
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refineryBattery = (int) Mathf.Lerp(0, batteryToUse, elapsed / duration);
            UpdateRefineryProgressBars();
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exactly the target
        refineryBattery = initialBattery;
        refineryProgressSliderWorld.GetComponent<Slider>().value = refineryBattery;
        refineryProgressSliderUI.GetComponent<Slider>().value = refineryBattery;
        refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = "100%";
    }

    private void UpdateRefineryProgressBars() {
        refineryProgressSliderWorld.GetComponent<Slider>().maxValue = initialBattery;
        refineryProgressSliderWorld.GetComponent<Slider>().value = refineryBattery;
        refineryProgressSliderUI.GetComponent<Slider>().maxValue = initialBattery;
        refineryProgressSliderUI.GetComponent<Slider>().value = refineryBattery;
        refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = (int) (refineryBattery * 100 / initialBattery) + "%";
    }

    public void UpgradeBattery(float newValue) {
        refineryBattery = newValue - (initialBattery - refineryBattery);
        initialBattery = newValue;
        UpdateRefineryProgressBars();
         GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    public void ImproveEfficiency(float newValue) {
        refineryInefficiency = newValue / 100f;
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    public void LoadData(GameData data) {
        // This will call LoadCorrectUpgrade in RefineryUpgrades
        capacityUpgrades.GetComponent<RefineryUpgrades>().InitializeRefinery(data.refineryCapacity, gameObject);
        efficiencyUpgrades.GetComponent<RefineryUpgrades>().InitializeRefinery(data.refineryInefficiency, gameObject);
        
        this.refineryInefficiency = data.refineryInefficiency / 100;
        this.refineryBattery = data.refineryBattery;
        this.initialBattery = data.refineryCapacity;

        UpdateRefineryProgressBars();
    }

    public void SaveData(ref GameData data) {
        data.refineryBattery = this.refineryBattery;
        data.refineryCapacity = this.initialBattery;
        data.refineryInefficiency = Mathf.Round(this.refineryInefficiency * 100 * 10) / 10;
    }
}
