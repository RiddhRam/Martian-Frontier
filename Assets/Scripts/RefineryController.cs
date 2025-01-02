using System.Collections;
using System.Text.RegularExpressions;
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
    private float refineryInefficiency;
    private int[] materialPrices;
    public GameObject capacityUpgrades;
    public GameObject efficiencyUpgrades;
    private float profitMultiplier = 1;
    private float levelProfitMultiplier = 0;
    [SerializeField]
    private float rebirthProfitMultiplier = 0;
    private Transform largeFogOfWar;
    private AudioDelegator audioDelegator;
    private DataPersistenceManager dataPersistenceManager;
    private GameObject playerVehicle;
    private AnalyticsDelegator analyticsDelegator;
    private MineRenderer mineRenderer;
    string childName;
    bool doneAnimation;

    int y;
    int x;

    void Start() {
        materialPrices = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().GetMaterialPrices();
        largeFogOfWar = GameObject.Find("Large Fog Of War").transform;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();
        dataPersistenceManager = GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>();
        playerVehicle = GameObject.Find("Player Vehicle");
        mineRenderer = mine.GetComponent<MineRenderer>();
        analyticsDelegator = AnalyticsDelegator.Instance;
        if (initialBattery < refineryBattery || initialBattery < 120) {
            initialBattery = 120;
        }

        if (refineryBattery > initialBattery) {
            refineryBattery = initialBattery;
        }
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

        cashToAdd = (long) (cashToAdd * GetTotalProfitMultiplier());

        // Verify that this is the right amount
        playerState.GetComponent<PlayerState>().AddCash((long) cashToAdd, savedMaterialCount);
        haulerController.SetMaterialCount(materialCount);
        haulerController.ShowFloatingText("$" + FormatPrice((long) cashToAdd));
        audioDelegator.PlayAudio(vehicleSoundEffects, oreSaleSoundEffect, 0.4f);
        analyticsDelegator.DropOffOres(collision.name, haulerController.GetTotalMaterialCount(), cashToAdd);
    }

    public void CallResetMineFromButton() {
        StartCoroutine(ResetMine());
    }

    private IEnumerator ResetMine() {
        mineRenderer.mineInitialization = 0;

        SpriteRenderer mineEntranceSpriteRenderer = mineEntrance.GetComponent<SpriteRenderer>();
        BoxCollider2D mineEntranceBoxCollider = mineEntrance.GetComponent<BoxCollider2D>();
        // Disable mine temporarily
        mineEntranceSpriteRenderer.sprite = mineEntranceOff;
        mineEntranceBoxCollider.enabled = true;

        // Move player off the dropoff area, and move all players inside the mine to the outside
        playerVehicle.transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));

        // Cover the map
        largeFogOfWar.position = new(0, -256, 0);
        largeFogOfWar.GetComponent<SpriteRenderer>().sortingOrder = 3;

        doneAnimation = false;
        StartCoroutine(GraduallyIncreaseBattery(initialBattery));

        // Destroy all leftover materials, we do it this way, in case someone mined something 
        // just as the mine was shutting down, and the ore didn't have enough time to have 
        // the mine set as its parent
        // This HAS to go first otherwise the mine will not reset tilemaps properly
        MaterialManager[] materials = FindObjectsOfType<MaterialManager>();

        foreach (var material in materials) {
            mineRenderer.ReturnMaterialObject(material.gameObject, material.materialIndex, material.id);
        }
        materials = null;

        // Reset the mine        
        int counter = 0;

        // Split the mine reset work into intervals
        for (int i = 0; i < mine.transform.childCount; i++)
        {
            GameObject child = mine.transform.GetChild(i).gameObject;

            // Skip null objects
            if (!child)
                continue;

            childName = child.name;

            // If a tilemap row, row generation trigger, or GenerationTriggers parent, or mine background tilemap
            if ((childName.Contains("Row") || childName.Contains("Generation") || childName.Contains("Background")) && child.activeSelf)
            {
                // Repool or destroy
                if (childName.Contains("Row")) {
                    // Define a regex to capture Y and X values
                    var match = Regex.Match(childName, @"Row (\d+), Column (\d+)");

                    y = int.Parse(match.Groups[1].Value);
                    x = int.Parse(match.Groups[2].Value);

                    mineRenderer.ReturnTilemapObject(child, x * 25, y * -12 - 5);

                } else if (childName.Contains("Background")) {

                    mineRenderer.ReturnBackgroundTilemapObject(child);
                } else {

                    Destroy(child);
                    i--;
                }
            

                // Only delete 42 per frame
                if (counter >= 84) {
                    yield return new WaitForSecondsRealtime(0.1f);
                    counter = 0;
                }
                counter++;
            }
        }

        yield return new WaitUntil(() => doneAnimation);

        // Initialize and uncover map
        mineRenderer.InitializeMine();
        largeFogOfWar.GetComponent<SpriteRenderer>().sortingOrder = 0;

        // Create the new mine
        GameObject genTrigGameObject = Instantiate(generationTriggers);
        genTrigGameObject.transform.SetParent(mine.transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigGameObject.name = genTrigGameObject.name.Substring(0, genTrigGameObject.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigGameObject.transform.childCount; i++) {
            genTrigGameObject.transform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(mine);
        }

        mineRenderer.mineInitialization = 1;
        SaveGame();

        // Wait for this to be done

        mineRenderer.mineInitialization = 2;
        
        // Renable the mine
        mineEntranceSpriteRenderer.sprite = mineEntranceOn;
        mineEntranceBoxCollider.enabled = false;

        BoxCollider2D gameObjectBoxCollider2D = gameObject.GetComponent<BoxCollider2D>();
        // Let user use dropoff, also flash it in case user was anxiously trying to use it by pressing against it
        gameObjectBoxCollider2D.isTrigger = true;
        gameObjectBoxCollider2D.enabled = false;
        gameObjectBoxCollider2D.enabled = true;

        SaveGame();
    }

    private IEnumerator GraduallyIncreaseBattery(float batteryToUse)
    {
        float duration = 6.0f; // Duration of the increase in seconds
        float elapsed = 0f;

        audioDelegator.PlayAudio(UISoundEffects, batteryRechargeSoundEffect, 0.45f);

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

        doneAnimation = true;
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

    public void SetBattery(float newValue) {
        refineryBattery = newValue - (initialBattery - refineryBattery);
        initialBattery = newValue;
        SaveGame();
        UpdateRefineryProgressBars();
    }

    public void SetEfficiency(float newValue) {
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
        dataPersistenceManager.SaveGame();
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

    public void SetProfitMultiplier(float newMultiplier) {
        profitMultiplier = newMultiplier;
    }

    public float GetProfitMultiplier() {
        return profitMultiplier;
    }

    public void SetLevelProfitMultiplier(float newLevelMultiplier) {
        // Have to round due to floating point errors
        levelProfitMultiplier = Mathf.Round(newLevelMultiplier * 100f) / 100f;
    }

    public float GetLevelProfitMultiplier() {
        return levelProfitMultiplier;
    }

    public void SetRebirthProfitMultiplier(float newRebirthMultiplier) {
        // Have to round due to floating point errors
        rebirthProfitMultiplier = Mathf.Round(newRebirthMultiplier * 100f) / 100f;
    }

    public float GetRebirthProfitMultiplier() {
        return rebirthProfitMultiplier;
    }

    public float GetTotalProfitMultiplier() {
        // Have to round due to floating point errors
        float multiplier = profitMultiplier + levelProfitMultiplier + rebirthProfitMultiplier;

        return Mathf.Round(multiplier * 100f) / 100f;
    }

    public void PlayerRebirth() {
        capacityUpgrades.GetComponent<RefineryUpgrades>().ResetUpgrade();
        efficiencyUpgrades.GetComponent<RefineryUpgrades>().ResetUpgrade();

        StartCoroutine(ResetMine());
    }
}