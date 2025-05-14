using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour, IDataPersistence
{
    public GameObject[] cashDisplays;
    public GameObject[] gemDisplays;
    public GameObject[] xpDisplays;
    public GameObject[] creditDisplays;
    public GameObject[] gemCashPurchasePanels;
    public GameObject materialProfitPanel;


    // Can't serialize field on BigIntegers
    private BigInteger userCash;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private long savedAmountSubtract;
    private BigInteger userXP;
    private BigInteger blocksMined;
    public BigInteger materialsSold;
    private BigInteger moneyEarned;
    private BigInteger userGems;
    private BigInteger gemsEarned;
    private BigInteger userCredits;
    private float highestMined;
    private List<string> vehiclesOwned = new();

    [SerializeField] private RefineryController refineryController;
    private DataPersistenceManager dataPersistenceManager;
    [SerializeField] private UIDelegation uIDelegation;
    private AnalyticsDelegator analyticsDelegator;
    private DailyChallengeDelegator dailyChallengeDelegator;
    private LeaderboardDelegator leaderboardDelegator;
    [SerializeField] private SupplyCrateDelegator supplyCrateDelegator;
    [SerializeField] private UpgradesDelegator upgradesDelegator;
    [SerializeField] private PlayerVehicleDelegation playerVehicleDelegation;
    public GarageDelegator garageDelegator;

    private int freeMoneyToAdd = 0;
    [SerializeField] private GameObject cashSliderGO;
    [SerializeField] private GameObject cashTextGO;
    private Slider cashSlider;
    private TextMeshProUGUI cashText;
    private Slider[] xpDisplaysSliders;
    private TextMeshProUGUI[] xpDisplaysText;
    private GameObject[] drillers;

    private bool notSinglePlayerScene;

    [SerializeField] private GameObject ResetMineButton;
    [SerializeField] private GameObject betaScreen;

    float profitMultiplier;

    string levelString;

    public bool loaded = false;
    bool specialGameMode = false;

    // Can't constantly be saving the game when an ore so only call it once in a while
    int miningCount = 0;
    const int miningSaveInterval = 100;

    void Awake() {
        leaderboardDelegator = LeaderboardDelegator.Instance;
        analyticsDelegator = AnalyticsDelegator.Instance;
        dataPersistenceManager = DataPersistenceManager.Instance;
        dailyChallengeDelegator = DailyChallengeDelegator.Instance;
        
        // Credits are used for special game modes
        if (creditDisplays.Length > 0) {
            specialGameMode = true;
        }

        xpDisplaysSliders = new Slider[xpDisplays.Length];
        xpDisplaysText = new TextMeshProUGUI[xpDisplays.Length];

        for (int i = 0; i != xpDisplays.Length; i++) {
            xpDisplaysSliders[i] = xpDisplays[i].GetComponent<Slider>();
            xpDisplaysText[i] = xpDisplays[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }

        if (garageDelegator) {
            drillers = garageDelegator.drillers;
        }
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
    public void AddCash(long cashToAdd, bool fromMining = false) {

        userCash += cashToAdd;
        moneyEarned += cashToAdd;

        UpdateCashDisplays();

        if (fromMining) {
            miningCount++;
            if (miningCount < miningSaveInterval) {
                return;                
            }
            miningCount = 0;
        }

        dataPersistenceManager.SaveGame();
    }

    public void AddGems(long gemsToAdd) {

        userGems += gemsToAdd;
        gemsEarned += gemsToAdd;

        UpdateGemDisplays();
        dataPersistenceManager.SaveGame();
    }

    public void AddGems(int gemsToAdd) {
        Debug.Log("Adding");
        
        userGems += gemsToAdd;
        gemsEarned += gemsToAdd;

        UpdateGemDisplays();
        dataPersistenceManager.SaveGame();
    }

    public void AddCredits(int creditsToAdd) {        
        userCredits += creditsToAdd;

        UpdateCreditDisplays();
        dataPersistenceManager.SaveGame();
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

            if (objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>()) {
                vehicleType = "Driller";
                tier = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();
                dailyChallengeDelegator.PurchasedVehicle(0);
            }

            // If it's a hauler or driller, it won't be null
            if (vehicleType == null) {
                return;
            }
            
            vehiclesOwned.Add(objectBeingPurchased.name);

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

    public void SubtractGems(long amountToSubtract) {
        userGems -= amountToSubtract;
        UpdateGemDisplays();
    }

    public void SubtractCredits(long amountToSubtract) {
        userCredits -= amountToSubtract;
        UpdateCreditDisplays();

        dataPersistenceManager.SaveGame();
    }

    public void ResetCredits() {
        userCredits = 0;
        UpdateCreditDisplays();

        dataPersistenceManager.SaveGame();
    }

    public void UpdateHighestMined(float newMineAmount) {
        if (newMineAmount > highestMined) {
            highestMined = newMineAmount;
            UpdateGemCashPurchasePanels();
        }
    }

    public void UpdateGemCashPurchasePanels() {
        // 4000 gems saves you 2 mins by giving you the same amount as the highest mined value you achieved
        // its a float so it doesnt get rounded down if there's a decimal
        const float mainGemPrice = 4000;

        for (int i = 0; i != gemCashPurchasePanels.Length; i++) {
            GemCashPurchasePanel gemCashPurchasePanel = gemCashPurchasePanels[i].GetComponent<GemCashPurchasePanel>();
            gemCashPurchasePanel.UpdateCashAmount(RoundToSignificantDigits(highestMined * (gemCashPurchasePanel.gemPrice / mainGemPrice), 2));
        }
    }

    public float GetHighestMined() {
        return highestMined;
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

    // True if player has sufficient funds
    public bool VerifyEnoughGems(long price) {
        if (userGems - price >= 0) {
            return true;
        }

        return false;
    }

    public bool VerifyEnoughCredits(int price) {
        if (userCredits < price) {
            return false;
        }
        
        return true;
    }

    public void NewBlockMined(int oresMined, int amount) {
        int userLevel = (int) GetUserLevel();
        // Gain 1 xp for mining a block, but gain 4 additional for mining an ore
        // Total 5 xp for mining an ore
        userXP += 4 * oresMined + amount;
        supplyCrateDelegator.ChangeProgressToNextCrate(amount);

        UpdateXPDisplays();

        // If player leveled up update power visibility
        int newLevel = (int) GetUserLevel();
        if (newLevel > userLevel) {
            upgradesDelegator.UpdatePowerVisibility(newLevel);
        }
    
        blocksMined += amount;
    }

    public void NewMaterialsSold(int amount, bool isNPC) {
        materialsSold++;

        if (!isNPC) {
            leaderboardDelegator.AddOreScore(amount);
        }
    }

    private void UpdateSubtractedAmount(GameObject objectBeingPurchased) {
        // If driller
        savedAmountSubtract = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetPrice();
    }

    public bool CheckVehicleOwnerShip(string vehicleName) {

        (string secondaryName, bool checkSecondaryName) = playerVehicleDelegation.GetMergedVehicleName(vehicleName);

        foreach (string vehicleOwned in vehiclesOwned) {
            if (vehicleOwned.Contains(vehicleName)) {
                return true;
            } else if (checkSecondaryName && vehicleOwned.Contains(secondaryName)) {
                return true;
            }
        }

        return false;
    }
    
    // Update all UI elements that show the user's money
    public void UpdateCashDisplays() {
        string cashText = "$" + FormatPrice(userCash);

        for (int i = 0; i != cashDisplays.Length; i++) {
            cashDisplays[i].GetComponent<TextMeshProUGUI>().text = cashText;
        }
    }

    public void UpdateCreditDisplays() {
        string creditText = FormatPrice(userCredits);

        for (int i = 0; i != creditDisplays.Length; i++) {
            creditDisplays[i].GetComponent<TextMeshProUGUI>().text = creditText;
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
    public string FormatPrice(BigInteger price)
    {
        if (price >= 1_000_000_000_000_000_000)
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

    public BigInteger RoundToSignificantDigits(float num, int n)
    {
        if (num == 0)
            return 0;

        double d = Math.Ceiling(Math.Log10(num < 0 ? -num : num));
        int power = n - (int)d;
        double magnitude = Math.Pow(10, power);
        double shifted = Math.Round(num * magnitude);
        return (BigInteger) (shifted / magnitude);
    }

    public void LoadData(GameData data) {
        // Only players from the beta will have this (this was one of the defaults, along with GRINDER I)
        if (data.vehiclesOwned.Contains("STUBBY")) {
            // Set this so we know when the game restarts
            PlayerPrefs.SetInt("Beta", 200);
            dataPersistenceManager.ResetBetaPlayer();
            return;
        }

        // We previously found this was a beta player
        if (PlayerPrefs.GetInt("Beta") == 200) {
            betaScreen.SetActive(true);
        }

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
        }

        this.userCash = BigInteger.Parse(data.userCash);
        this.userXP = BigInteger.Parse(data.userXP);
        this.blocksMined = BigInteger.Parse(data.blocksMined);
        this.materialsSold = BigInteger.Parse(data.materialsSold);
        this.moneyEarned = BigInteger.Parse(data.moneyEarned);
        this.vehiclesOwned = data.vehiclesOwned;
        this.userGems = BigInteger.Parse(data.userGems);
        this.gemsEarned = BigInteger.Parse(data.gemsEarned);
        this.userCredits = BigInteger.Parse(data.userCredits);

        this.highestMined = data.highestMined;

        loaded = true;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            ResetMineButton.SetActive(false);
        }

        // Called from this load function and upgrades delegator, whichever loads second works
        if (upgradesDelegator) {
            upgradesDelegator.UpdatePowerVisibility((int) GetUserLevel());
        }
       
        UpdateCashDisplays();
        UpdateGemDisplays();
        UpdateXPDisplays();
        UpdateCreditDisplays();
        UpdateGemCashPurchasePanels();
    }

    public void SaveData(ref GameData data) {
        data.userCash = this.userCash.ToString();
        data.userXP = this.userXP.ToString();
        data.blocksMined = this.blocksMined.ToString();
        data.materialsSold = this.materialsSold.ToString();
        data.moneyEarned = this.moneyEarned.ToString();
        data.vehiclesOwned = this.vehiclesOwned;
        data.userGems = this.userGems.ToString();
        data.gemsEarned = this.gemsEarned.ToString();
        data.userCredits = this.userCredits.ToString();
        data.highestMined = this.highestMined;
    }

    private void UpdateXPDisplays() {
        if (specialGameMode) {
            return;
        }

        float userLevel = GetUserLevel();
        int level = (int) userLevel;

        profitMultiplier = refineryController.GetLevelProfitMultiplier();

        float calculatedValue = level * 0.005f;
        // For each level, add 0.5% to the profit multiplier
        refineryController.SetLevelProfitMultiplier(calculatedValue);

        float xpSliderValue = userLevel - level;
        levelString = level.ToString();
        for (int i = 0; i != xpDisplays.Length; i++) {
            xpDisplaysSliders[i].value = xpSliderValue;
            xpDisplaysText[i].text = levelString;
        }
    }

    public float GetUserLevel() {
        const int baseXP = 500; // XP needed for level 0 to 1
        const int increment = 500; // Additional XP per level
        int currentLevel = 0; // Start at level 0
        BigInteger remainingXP = userXP; // Start with total user XP

        while (remainingXP >= baseXP + currentLevel * increment) {
            remainingXP -= baseXP + currentLevel * increment;
            currentLevel++;
        }

        float percentageToNextLevel = (float) ((double) remainingXP/(baseXP + currentLevel * increment));

        return currentLevel + percentageToNextLevel;
    }

    public int GetRecommendedDrillTier() {
        // Roughly based on the median of the value the total value of each tier in each mine
        // These numbers are lower than the median, roughly a third of tier 1 and tier 2 respectively
        if (highestMined < 20_000) {
            return 1;
        } else if (highestMined < 150_000) {
            return 2;
        }

        return 3;
    }

    public BigInteger GetBlocksMined() {
        return blocksMined;
    }

    public void RewardPlayerWithGems(int amount, string message = null) {

        leaderboardDelegator.gemRewardsToCollect += amount;
        leaderboardDelegator.CheckForRewards(message);
    }

    public BigInteger GetUserGems() {
        return userGems;
    }

    public BigInteger GetMoneyEarned() {
        return moneyEarned;
    }

    public BigInteger GetUserCash() {
        return userCash;
    }

    public BigInteger GetUserCredits() {
        return userCredits;
    }

    public void CollectBetaReward() {
        // Cannot be used again
        PlayerPrefs.SetInt("Beta", 0);
        AddGems(800_000);

        betaScreen.SetActive(false);
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