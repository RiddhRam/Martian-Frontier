using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour, IDataPersistence
{
    public GameObject[] cashDisplays;
    public GameObject[] gemDisplays;
    public GameObject[] xpDisplays;
    public GameObject garagePanel;
    public GameObject materialProfitPanel;
    // Can't serialize field on BigIntegers
    private BigInteger userCash;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private long savedAmountSubtract;
    private BigInteger userXP;
    private BigInteger blocksMined;
    private BigInteger materialsSold;
    private BigInteger moneyEarned;
    private BigInteger userGems;
    private BigInteger gemsEarned;
    private List<string> vehiclesOwned = new();
    private int[] materialPrices;
    private bool loading = true;
    [SerializeField]
    private float rebirthProfitMultiplier;
    public RefineryController refineryController;
    public DataPersistenceManager dataPersistenceManager;
    public ProfitPanelDelegator profitPanelDelegator;
    public UIDelegation uIDelegation;
    public AnalyticsDelegator analyticsDelegator;
    public DailyChallengeDelegator dailyChallengeDelegator;
    public LeaderboardDelegator leaderboardDelegator;
    private int freeMoneyToAdd = 0;
    [SerializeField]
    private GameObject cashSliderGO;
    [SerializeField]
    private GameObject cashTextGO;
    private Slider cashSlider;
    private TextMeshProUGUI cashText;
    private Slider[] xpDisplaysSliders;
    private TextMeshProUGUI[] xpDisplaysText;
    private GameObject[] drillers;
    private int highestDrillTier = 1;

    int baseXP; 
    int increment; 
    int level; 
    BigInteger remainingXP;
    float profitMultiplier;
    float calculatedValue;
    float tolerance;
    float xpSliderValue;
    string levelString;

    void Awake() {
        xpDisplaysSliders = new Slider[xpDisplays.Length];
        xpDisplaysText = new TextMeshProUGUI[xpDisplays.Length];

        for (int i = 0; i != xpDisplays.Length; i++) {
            xpDisplaysSliders[i] = xpDisplays[i].GetComponent<Slider>();
            xpDisplaysText[i] = xpDisplays[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }

        materialPrices = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().GetMaterialPrices();
        drillers = garagePanel.GetComponent<GarageDelegator>().drillers;
        
    }

    void Start() {
        if (cashSliderGO) {
            cashSlider = cashSliderGO.GetComponent<Slider>();
        }

        if (cashTextGO) {
            cashText = cashTextGO.GetComponent<TextMeshProUGUI>();
        }
    }

    // Validate and add cash
    // This version of AddCash is called when the user drops some materials off at the refinery
    public void AddCash(long cashToAdd, int[] materialCount) {

        // Count the prices of all materials
        long amountToAdd = 0;
        for (int i = 0; i != materialCount.Length; i++) {
            amountToAdd += materialCount[i] * materialPrices[i];
        }

        amountToAdd = (long) (amountToAdd * refineryController.GetTotalProfitMultiplier());

        // If the amounts are correct, add the money
        if (amountToAdd == cashToAdd) {
            userCash += cashToAdd;
            moneyEarned += cashToAdd;
            leaderboardDelegator.AddCashScore(cashToAdd);
        }
        
        UpdateCashDisplays();
    }

    public void AddGems(long gemsToAdd) {

        userGems += gemsToAdd;
        gemsEarned += gemsToAdd;

        UpdateGemDisplays();
    }

    // Validate again and subtract cash
    // Only call if VerifyEnoughCash was called
    // For vehicles
    public void SubtractCash(long amountToSubtract, GameObject objectBeingPurchased) {
        // objectBeingPurchased is some upgrade or vehicle being bought

        if (amountToSubtract == savedAmountSubtract) {
            userCash -= amountToSubtract;
            // This causes an error if anything except a driller or hauler is passed into the gameobject parameter
            // If it has a driller or hauler controller, add it to the list of vehicles owned
            string vehicleType = null;
            int tier = 0;

            if (objectBeingPurchased.GetComponent<HaulerController>()) {
                vehicleType = "Hauler";
                dailyChallengeDelegator.PurchasedVehicle(1);
            } else if (objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>()) {
                vehicleType = "Driller";
                tier = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();
                dailyChallengeDelegator.PurchasedVehicle(0);
            }

            // If it's a hauler or driller, it won't be null
            if (vehicleType == null) {
                return;
            }
            
            vehiclesOwned.Add(objectBeingPurchased.name);
            leaderboardDelegator.AddVehicleScore(1);
            UpdateHighestDrillTier();
            dataPersistenceManager.SaveGame();
            analyticsDelegator.PurchaseVehicle(objectBeingPurchased.name, vehicleType, tier);
        }

        UpdateCashDisplays();
    }

    // For Refinery Upgrade
    public void SubtractCash(long amountToSubtract) { 

        if (amountToSubtract == savedAmountSubtract) {
            userCash -= amountToSubtract;
        }
        UpdateCashDisplays();
    } 

    public void PurchaseCashWithGems(GameObject gemPanel) {
        GemCashPurchasePanel gemCashPurchasePanel = gemPanel.GetComponent<GemCashPurchasePanel>();

        if (gemCashPurchasePanel.gemPrice > userGems) {
            uIDelegation.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        userCash += gemCashPurchasePanel.cashAmount;
        userGems -= gemCashPurchasePanel.gemPrice;

        UpdateCashDisplays();
        UpdateGemDisplays();

        dataPersistenceManager.SaveGame();
    }

    // Validate and add XP
    public void AddXP(int amountToAddXP) {
        // objectReason can be something the user dropped off or rebirth

        userXP += amountToAddXP;
    }

    // Make sure user has enough money to buy something
    public bool VerifyEnoughCash(GameObject objectBeingPurchased) {
        // objectBeingPurchased is some upgrade or vehicle being bought
        UpdateSubtractedAmount(objectBeingPurchased);

        if (userCash - savedAmountSubtract >= 0) {
            return true;
        }

        return false;
    }
    // For Refinery Upgrade
    public bool VerifyEnoughCash(long price) {
        savedAmountSubtract = price;

        if (userCash - savedAmountSubtract >= 0) {
            return true;
        }

        return false;
    }

    public void NewBlockMined(int oresMined, int amount) {
        // Gain 1 xp for mining a block, but gain 4 additional for mining an ore
        // Total 5 xp for mining an ore
        userXP += 4 * oresMined + amount;

        // Simulate asynchronous operation (e.g., if you're doing something async in UpdateXPDisplays())
        UpdateXPDisplays();
        
        blocksMined += amount;
    }

    public void NewMaterialSold() {
        materialsSold++;
    }

    private void UpdateSubtractedAmount(GameObject objectBeingPurchased) {
        // If hauler
        if (objectBeingPurchased.GetComponent<HaulerController>()) {
            savedAmountSubtract = objectBeingPurchased.GetComponent<HaulerController>().GetPrice();
        } 
        // If driller
        else {
            savedAmountSubtract = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetPrice();
        }
    }

    public bool CheckVehicleOwnerShip(string vehicleName) {

        if (vehiclesOwned.Contains(vehicleName)) {
            return true;
        }

        return false;
    }

    public void UpdateHighestDrillTier() {
        for (int i = 0; i != drillers.Length; i++) {
            if (!CheckVehicleOwnerShip(drillers[i].name)) {
                continue;
            }

            if (drillers[i].transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier() > highestDrillTier) {
                highestDrillTier = drillers[i].transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();
                dailyChallengeDelegator.ScaleAllTiers();
            }
        }
    }
    // Update all UI elements that show the user's money
    public void UpdateCashDisplays() {
        string cashText = "$" + FormatPrice(userCash);

        for (int i = 0; i != cashDisplays.Length; i++) {
            cashDisplays[i].GetComponent<TextMeshProUGUI>().text = cashText;
        }
    }

    public void UpdateGemDisplays() {
        string gemText = FormatPrice(userGems);

        for (int i = 0; i != gemDisplays.Length; i++) {
            gemDisplays[i].GetComponent<TextMeshProUGUI>().text = gemText;
        }
    }

    // The FormatPrice in other places is slightly different. 
    // Here we need to purposefully round down so the user doesn't 
    // overestimate their money and buy something they can't afford
    private string FormatPrice(BigInteger price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Se"
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Se";
        }
        else if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public void LoadData(GameData data) {
        
        analyticsDelegator = AnalyticsDelegator.Instance;

        loading = true;
        this.userCash = BigInteger.Parse(data.userCash);
        this.userXP = BigInteger.Parse(data.userXP);
        this.blocksMined = BigInteger.Parse(data.blocksMined);
        this.materialsSold = BigInteger.Parse(data.materialsSold);
        this.moneyEarned = BigInteger.Parse(data.moneyEarned);
        this.vehiclesOwned = data.vehiclesOwned;
        this.rebirthProfitMultiplier = data.rebirthProfitMultiplier;
        this.userGems = BigInteger.Parse(data.userGems);
        this.gemsEarned = BigInteger.Parse(data.gemsEarned);
        refineryController.SetRebirthProfitMultiplier(rebirthProfitMultiplier);
        loading = false;
       
        UpdateCashDisplays();
        UpdateGemDisplays();
        UpdateXPDisplays();
        UpdateHighestDrillTier();
    }

    public void SaveData(ref GameData data) {
        data.userCash = this.userCash.ToString();
        data.userXP = this.userXP.ToString();
        data.blocksMined = this.blocksMined.ToString();
        data.materialsSold = this.materialsSold.ToString();
        data.moneyEarned = this.moneyEarned.ToString();
        data.vehiclesOwned = this.vehiclesOwned;
        data.rebirthProfitMultiplier = this.rebirthProfitMultiplier;
        data.userGems = this.userGems.ToString();
        data.gemsEarned = this.gemsEarned.ToString();
    }

    private void UpdateXPDisplays() {
        baseXP = 500; // XP needed for level 0 to 1
        increment = 500; // Additional XP per level
        level = 0; // Start at level 0
        remainingXP = userXP; // Start with total user XP

        while (remainingXP >= baseXP + level * increment) {
            remainingXP -= baseXP + level * increment;
            level++;
        }

        profitMultiplier = refineryController.GetLevelProfitMultiplier();
        calculatedValue = level * 0.01f;
        tolerance = 0.005f;

        if ((profitMultiplier < calculatedValue - tolerance) && !loading) {
            analyticsDelegator.LevelUp(level);
        }

        // For each level, add 1% to the profit multiplier
        refineryController.SetLevelProfitMultiplier(calculatedValue);

        xpSliderValue = (float) ((double) remainingXP/(baseXP + level * increment));
        levelString = level.ToString();
        for (int i = 0; i != xpDisplays.Length; i++) {
            xpDisplaysSliders[i].value = xpSliderValue;
            xpDisplaysText[i].text = levelString;
        }
    }

    public void Rebirth() {
        long rebirthPrice = profitPanelDelegator.GetRebirthPrice();
        if (!VerifyEnoughCash(rebirthPrice)) {
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return;
        }

        rebirthProfitMultiplier += 0.01f;
        
        userXP = 0;
        userCash = 0;
        vehiclesOwned = new List<string> { "GRINDER I", "STUBBY" };
        GameObject newVehicle = garagePanel.GetComponent<GarageDelegator>().drillers[0];
        garagePanel.GetComponent<GarageDelegator>().PlayerRebirth();
        GameObject.Find("Player Vehicle").GetComponent<PlayerVehicleDelegation>().SwitchVehicle(newVehicle);
        // Switch vehicle, then reset mine, to get rid of all materials for sure,
        // because the haulers will drop everything
        refineryController.PlayerRebirth();
        refineryController.SetRebirthProfitMultiplier(rebirthProfitMultiplier);
        dataPersistenceManager.SaveGame();
        analyticsDelegator.Rebirth((int) Mathf.Round(rebirthProfitMultiplier / 0.01f));

        UpdateCashDisplays();
        UpdateXPDisplays();

        highestDrillTier = 1;
        dailyChallengeDelegator.ScaleAllTiers();
    }
   
    public int GetHighestDrillTier() {
        return highestDrillTier;
    }

    public BigInteger GetBlocksMined() {
        return blocksMined;
    }

    // For development only
    public void FreeMoney() {
        userCash += freeMoneyToAdd;
        UpdateCashDisplays();
        analyticsDelegator.TestEvent("Just testing");
    }

    public void FreeMoneyUpdate() {
        freeMoneyToAdd = (int) cashSlider.value;

        cashText.text = "$" + FormatPrice(freeMoneyToAdd);
    }
}