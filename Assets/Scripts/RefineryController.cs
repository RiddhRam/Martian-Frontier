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
    public AudioSource vehicleSoundEffects;
    public AudioSource UISoundEffects;
    public AudioClip oreSaleSoundEffect;
    public AudioClip batteryRechargeSoundEffect;

    [SerializeField]
    private float refineryBattery;
    [SerializeField]
    private float initialBattery;
    [SerializeField]
    private float refineryInefficiency = 1;
    private int[] materialPrices;
    public GameObject capacityUpgrades;
    public GameObject efficiencyUpgrades;
    private float profitMultiplier = 1;

    void Start() {
        materialPrices = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().GetMaterialPrices();
    }

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
                if (refineryBattery <= 0) {
                    break;
                }
                
                refineryBattery -= refineryInefficiency;
                materialCount[i]--;
                playerState.GetComponent<PlayerState>().NewMaterialSold();
                savedMaterialCount[i]++;
            }
        }

        // Reset the mine if needed
        if (refineryBattery <= 0) {
            // Stop user from entering user dropoff or mine
            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
            StartCoroutine(ResetMine());
        }

        UpdateRefineryProgressBars();

        // Calculate how much money to add
        float cashToAdd = 0;
        for (int i = 0; i != savedMaterialCount.Length; i++) {
            cashToAdd += savedMaterialCount[i] * materialPrices[i];
        }

        // Should never be less than
        if (cashToAdd <= 0) {
            return;
        }

        cashToAdd = (long) (cashToAdd * profitMultiplier);

        // Verify that this is the right amount
        playerState.GetComponent<PlayerState>().AddCash((long) cashToAdd, savedMaterialCount);
        AnalyticsDelegator.Instance.DropOffOres(collision.name, haulerController.GetTotalMaterialCount(), cashToAdd);
        haulerController.SetMaterialCount(materialCount);
        haulerController.ShowFloatingText("$" + FormatPrice((long) cashToAdd));
        vehicleSoundEffects.clip = oreSaleSoundEffect;
        // Try to keep max volume at around -23dB
        vehicleSoundEffects.volume = 0.4f;
        vehicleSoundEffects.Play();
    }

    private IEnumerator ResetMine() {
        mine.GetComponent<MineRenderer>().mineInitialization = 0;
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

        mine.GetComponent<MineRenderer>().InitializeMine();

        // Create the new mine
        GameObject genTrigGameObject = Instantiate(generationTriggers);
        genTrigGameObject.transform.SetParent(mine.transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigGameObject.name = genTrigGameObject.name.Substring(0, genTrigGameObject.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigGameObject.transform.childCount; i++) {
            genTrigGameObject.transform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(mine);
        }

        mine.GetComponent<MineRenderer>().mineInitialization = 1;
        SaveGame();

        // Wait for this to be done
        yield return StartCoroutine(GraduallyIncreaseBattery(initialBattery));

        mine.GetComponent<MineRenderer>().mineInitialization = 2;
        
        // Renable the mine
        mineEntrance.GetComponent<SpriteRenderer>().sprite = mineEntranceOn;
        mineEntrance.GetComponent<BoxCollider2D>().enabled = false;

        // Let user use dropoff, also flash it in case user was anxiously trying to use it by pressing against it
        gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = true;

        SaveGame();
    }

    private IEnumerator GraduallyIncreaseBattery(float batteryToUse)
    {
        float duration = 6.0f; // Duration of the increase in seconds
        float elapsed = 0f;

        UISoundEffects.clip = batteryRechargeSoundEffect;
        UISoundEffects.volume = 0.45f;
        UISoundEffects.Play();

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

        // Round up to nearest int
        string barText = Mathf.CeilToInt(refineryBattery * 100 / initialBattery) + "%";
        // Unless it's at 99, then don't round up to 100
        if (Mathf.FloorToInt(refineryBattery * 100 / initialBattery) == 99) {
            barText = "99%";
        }

        refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = barText;
    }

    public void UpgradeBattery(float newValue) {
        refineryBattery = newValue - (initialBattery - refineryBattery);
        initialBattery = newValue;
        SaveGame();
        UpdateRefineryProgressBars();
    }

    public void ImproveEfficiency(float newValue) {
        refineryInefficiency = newValue / 100f;
        SaveGame();
    }

    public void LoadData(GameData data) {
        // This will call LoadCorrectUpgrade in RefineryUpgrades
        capacityUpgrades.GetComponent<RefineryUpgrades>().InitializeRefinery(data.refineryCapacity, gameObject);
        efficiencyUpgrades.GetComponent<RefineryUpgrades>().InitializeRefinery(data.refineryInefficiency, gameObject);
        
        this.refineryInefficiency = data.refineryInefficiency / 100;
        this.initialBattery = data.refineryCapacity;
        this.refineryBattery = data.refineryBattery;
        // If refinery controller bar was in reset animation, then skip it and go straight to 100%
        if (data.mineInitialization == 1) {
            this.refineryBattery = initialBattery;
        }

        UpdateRefineryProgressBars();
        StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
    }

    public void SaveData(ref GameData data) {
        data.refineryBattery = this.refineryBattery;
        data.refineryCapacity = this.initialBattery;
        data.refineryInefficiency = Mathf.Round(this.refineryInefficiency * 100 * 10) / 10;
    }

    private void SaveGame() {
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor(price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor(price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor(price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor(price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor(price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor(price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public void SetProfitMultipler(float newMultiplier) {
        profitMultiplier = newMultiplier;
    }

    public float GetProfitMultipler() {
        return profitMultiplier;
    }
}