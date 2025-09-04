using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour, IDataPersistence
{
    private static PlayerState _instance;
    public static PlayerState Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<PlayerState>();
            }
            return _instance;
        }
    }

    [Header("Displays")]
    public GameObject[] cashDisplays;
    public GameObject[] gemDisplays;
    public GameObject[] xpDisplays;
    public GameObject[] creditDisplays;
    public GameObject[] gemCashPurchasePanels;

    [Header("Player Data")]
    // Can't serialize field on BigIntegers
    private BigInteger userCash;
    // Use this to verify the amount of money to add or subtract across verifications
    [SerializeField] private BigInteger savedAmountSubtract;
    private BigInteger userXP;
    private BigInteger blocksMined;
    public BigInteger materialsSold;
    private BigInteger moneyEarned;
    private BigInteger userGems;
    private BigInteger gemsEarned;
    private BigInteger userCredits;
    private double highestMined;
    private List<string> vehiclesOwned = new();
    public int targetDepth;

    [Header("Scripts")]
    [SerializeField] private RefineryController refineryController;
    [SerializeField] private SupplyCrateDelegator supplyCrateDelegator;
    [SerializeField] private PlayerVehicleDelegation playerVehicleDelegation;

    [Header("Cheats")]
    private int freeMoneyToAdd = 0;
    [SerializeField] private GameObject cashSliderGO;
    [SerializeField] private GameObject cashTextGO;
    private Slider cashSlider;
    private TextMeshProUGUI cashText;

    [Header("Other")]
    private Slider[] xpDisplaysSliders;
    private TextMeshProUGUI[] xpDisplaysText;
    public Slider targetDepthSlider;
    public TextMeshProUGUI targetDepthText;
    [SerializeField] private GameObject betaScreen;

    string levelString;

    public bool loaded = false;
    bool specialGameMode = false;

    // Can't constantly be saving the game when an ore is mined so only call it once in a while
    int miningCount = 0;
    const int miningSaveInterval = 150;

    void Awake()
    {
        // Credits are used for special game modes
        if (creditDisplays.Length > 0)
        {
            specialGameMode = true;
        }

        xpDisplaysSliders = new Slider[xpDisplays.Length];
        xpDisplaysText = new TextMeshProUGUI[xpDisplays.Length];

        for (int i = 0; i != xpDisplays.Length; i++)
        {
            xpDisplaysSliders[i] = xpDisplays[i].GetComponent<Slider>();
            xpDisplaysText[i] = xpDisplays[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        if (cashSliderGO)
        {
            cashSlider = cashSliderGO.GetComponent<Slider>();
        }

        if (cashTextGO)
        {
            cashText = cashTextGO.GetComponent<TextMeshProUGUI>();
        }
    }

    // Validate and add cash
    // This version of AddCash is called when the user drops some materials off at the refinery
    public void AddCash(double cashToAdd, bool fromMining = false)
    {
        userCash += new BigInteger(cashToAdd);
        moneyEarned += new BigInteger(cashToAdd);

        UpdateCashDisplays();

        if (fromMining)
        {
            miningCount++;
            if (miningCount < miningSaveInterval)
            {
                return;
            }
            miningCount = 0;
        }

        DataPersistenceManager.Instance.SaveGame();
    }

    public void AddGems(long gemsToAdd)
    {

        userGems += gemsToAdd;
        gemsEarned += gemsToAdd;

        UpdateGemDisplays();
        DataPersistenceManager.Instance.SaveGame();
    }

    public void AddGems(int gemsToAdd)
    {
        userGems += gemsToAdd;
        gemsEarned += gemsToAdd;

        UpdateGemDisplays();
        DataPersistenceManager.Instance.SaveGame();
    }

    public void AddCredits(int creditsToAdd)
    {
        userCredits += creditsToAdd;

        UpdateCreditDisplays();
        DataPersistenceManager.Instance.SaveGame();
    }

    // Validate again and subtract cash
    // Only call if VerifyEnoughCash was called
    // For vehicles
    public void SubtractCash(long amountToSubtract, GameObject objectBeingPurchased)
    {
        // objectBeingPurchased is some upgrade or vehicle being bought

        if (amountToSubtract == savedAmountSubtract)
        {
            userCash -= amountToSubtract;
            // This causes an error if anything except a driller or hauler is passed into the gameobject parameter
            // If it has a driller or hauler controller, add it to the list of vehicles owned
            string vehicleType = null;
            int tier = 0;

            if (objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>())
            {
                vehicleType = "Driller";
                tier = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();
            }

            // If it's a hauler or driller, it won't be null
            if (vehicleType == null)
            {
                return;
            }

            vehiclesOwned.Add(objectBeingPurchased.name);

            DataPersistenceManager.Instance.SaveGame();
            AnalyticsDelegator.Instance.PurchaseVehicle(objectBeingPurchased.name, vehicleType, tier);
        }

        UpdateCashDisplays();
    }

    // For Refinery Upgrade
    public void SubtractCash(BigInteger amountToSubtract)
    {

        if (amountToSubtract == savedAmountSubtract)
        {
            userCash -= amountToSubtract;
        }
        UpdateCashDisplays();
    }

    public void SubtractGems(long amountToSubtract)
    {
        userGems -= amountToSubtract;
        UpdateGemDisplays();
    }

    public void SubtractCredits(long amountToSubtract)
    {
        userCredits -= amountToSubtract;
        UpdateCreditDisplays();

        DataPersistenceManager.Instance.SaveGame();
    }

    public void ResetCredits()
    {
        userCredits = 0;
        UpdateCreditDisplays();

        DataPersistenceManager.Instance.SaveGame();
    }

    public void UpdateHighestMined(double newMineAmount)
    {
        if (newMineAmount > highestMined)
        {
            highestMined = newMineAmount;
            UpdateGemCashPurchasePanels();
        }
    }

    public void UpdateGemCashPurchasePanels()
    {
        // 2000 gems saves you 1 mins by giving you half the amount as the highest mined value you achieved
        // its a float so it doesnt get rounded down if there's a decimal
        const float mainGemPrice = 2000;
        double baseCashAmount = highestMined * 0.5;

        for (int i = 0; i != gemCashPurchasePanels.Length; i++)
        {
            GemCashPurchasePanel gemCashPurchasePanel = gemCashPurchasePanels[i].GetComponent<GemCashPurchasePanel>();
            gemCashPurchasePanel.UpdateCashAmount(RoundToSignificantDigits(baseCashAmount * (gemCashPurchasePanel.gemPrice / mainGemPrice), 2));
        }
    }

    public double GetHighestMined()
    {
        return highestMined;
    }

    // Make sure user has enough money to buy something
    public bool VerifyEnoughCash(GameObject objectBeingPurchased)
    {
        // objectBeingPurchased is some upgrade or vehicle being bought
        UpdateSubtractedAmount(objectBeingPurchased);

        if (userCash - savedAmountSubtract >= 0)
        {
            return true;
        }

        return false;
    }

    // For Refinery Upgrade
    public bool VerifyEnoughCash(BigInteger price)
    {
        savedAmountSubtract = price;

        if (userCash - savedAmountSubtract >= 0)
        {
            return true;
        }

        return false;
    }

    // True if player has sufficient funds
    public bool VerifyEnoughGems(long price)
    {
        if (userGems - price >= 0)
        {
            return true;
        }

        return false;
    }

    public bool VerifyEnoughCredits(int price)
    {
        if (userCredits < price)
        {
            return false;
        }

        return true;
    }

    public void NewBlockMined(int oresMined, int amount)
    {
        int userLevel = (int)GetUserLevel();
        // Gain 1 xp for mining a block, but gain 4 additional for mining an ore
        // Total 5 xp for mining an ore
        userXP += 4 * oresMined + amount;
        supplyCrateDelegator.ChangeProgressToNextCrate(amount);

        UpdateXPDisplays();

        blocksMined += amount;
    }

    public void NewMaterialsSold(int amount)
    {
        materialsSold++;
        LeaderboardDelegator.Instance.AddOreScore(amount);
    }

    private void UpdateSubtractedAmount(GameObject objectBeingPurchased)
    {
        // If driller
        savedAmountSubtract = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetPrice();
    }

    public bool CheckVehicleOwnerShip(string vehicleName)
    {

        (string secondaryName, bool checkSecondaryName) = playerVehicleDelegation.GetMergedVehicleName(vehicleName);

        foreach (string vehicleOwned in vehiclesOwned)
        {
            if (vehicleOwned.Contains(vehicleName))
            {
                return true;
            }
            else if (checkSecondaryName && vehicleOwned.Contains(secondaryName))
            {
                return true;
            }
        }

        return false;
    }

    // Update all UI elements that show the user's money
    public void UpdateCashDisplays()
    {
        string cashText = FormatPrice(userCash);

        for (int i = 0; i != cashDisplays.Length; i++)
        {
            cashDisplays[i].GetComponent<TextMeshProUGUI>().text = cashText;
        }
    }

    public void UpdateCreditDisplays()
    {
        string creditText = FormatPrice(userCredits);

        for (int i = 0; i != creditDisplays.Length; i++)
        {
            creditDisplays[i].GetComponent<TextMeshProUGUI>().text = creditText;
        }
    }

    public void UpdateGemDisplays()
    {
        string gemText = FormatPrice(userGems);

        for (int i = 0; i != gemDisplays.Length; i++)
        {
            gemDisplays[i].GetComponent<TextMeshProUGUI>().text = gemText;
        }
    }

    private void UpdateXPDisplays()
    {
        if (specialGameMode)
        {
            return;
        }

        float userLevel = GetUserLevel();
        int level = (int)userLevel;

        float calculatedValue = level * 0.005f;
        // For each level, add 0.5% to the profit multiplier
        refineryController.SetLevelProfitMultiplier(calculatedValue);

        float xpSliderValue = userLevel - level;
        levelString = level.ToString();
        for (int i = 0; i != xpDisplays.Length; i++)
        {
            xpDisplaysSliders[i].value = xpSliderValue;
            xpDisplaysText[i].text = levelString;
        }
    }

    // The FormatPrice in other places is slightly different. 
    public string FormatPrice(BigInteger price, int decimalPoints = 2)
    {
        string decimalTags = "0";

        // needs this to indicate decimals
        if (decimalPoints > 0)
        {
            decimalTags = "0.";
        }

        // Default is 2 if not specified
        for (int i = 0; i != decimalPoints; i++)
        {
            decimalTags += "#"; // 1 # = 1 extra decimal point
        }

        if (price >= 1_000_000_000_000_000)
        {
            // determine the 1000-power group
            BigInteger divisor = new BigInteger(1_000_000_000_000_000);   // 10¹⁵  → “aa”
            int group = 0;                                                // 0 → aa, 1 → ab …

            while (price / divisor >= 1000)
            {
                divisor *= 1000;   // next power of 10³
                group++;           // next suffix slot
            }

            // numeric part, truncated to two decimals
            double value = Math.Floor((double)(price * 100) / (double)divisor) / 100d;

            // two-letter suffix: “aa”, “ab” … “zz”
            int first = group / 26;           // 0–25 → ‘a’–‘z’
            int second = group % 26;
            if (first > 25) first = 25;        // clamp; beyond “zz” not supported
            char c1 = (char)('a' + first);
            char c2 = (char)('a' + second);
            string suffix = $"{c1}{c2}";

            return value.ToString(decimalTags) + suffix;
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 2 decimal places and format with "T"
            return (Mathf.Floor((float)price / 1_000_000_000_000f * 1000) / 1000).ToString(decimalTags) + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 2 decimal places and format with "B"
            return (Mathf.Floor((float)price / 1_000_000_000f * 1000) / 1000).ToString(decimalTags) + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 2 decimal places and format with "M"
            return (Mathf.Floor((float)price / 1_000_000f * 1000) / 1000).ToString(decimalTags) + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 2 decimal places and format with "K"
            return (Mathf.Floor((float)price / 1_000f * 1000) / 1000).ToString(decimalTags) + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public BigInteger RoundToSignificantDigits(double num, int n)
    {
        if (num == 0)
            return 0;

        double d = Math.Ceiling(Math.Log10(num < 0 ? -num : num));
        int power = n - (int)d;
        double magnitude = Math.Pow(10, power);
        double shifted = Math.Round(num * magnitude);
        return (BigInteger)(shifted / magnitude);
    }

    public void LoadData(GameData data)
    {
        // Only players from the beta will have this (this was one of the defaults, along with GRINDER I)
        if (data.vehiclesOwned.Contains("STUBBY"))
        {
            // Set this so we know when the game restarts
            PlayerPrefs.SetInt("Beta", 200);
            DataPersistenceManager.Instance.ResetBetaPlayer();
            return;
        }

        // We previously found this was a beta player
        if (PlayerPrefs.GetInt("Beta") == 200)
        {
            betaScreen.SetActive(true);
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
        SetTargetDepth(data.targetDepth);

        loaded = true;

        UpdateCashDisplays();
        UpdateGemDisplays();
        UpdateXPDisplays();
        UpdateCreditDisplays();
        UpdateGemCashPurchasePanels();
    }

    public void SaveData(ref GameData data)
    {
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
        data.targetDepth = this.targetDepth;
    }

    public float GetUserLevel()
    {
        const int baseXP = 500; // XP needed for level 0 to 1
        const int increment = 500; // Additional XP per level
        int currentLevel = 0; // Start at level 0
        BigInteger remainingXP = userXP; // Start with total user XP

        while (remainingXP >= baseXP + currentLevel * increment)
        {
            remainingXP -= baseXP + currentLevel * increment;
            currentLevel++;
        }

        float percentageToNextLevel = (float)((double)remainingXP / (baseXP + currentLevel * increment));

        return currentLevel + percentageToNextLevel;
    }

    public int GetRecommendedDrillTier()
    {
        // Roughly based on the median of the value the total value of each tier in each mine
        // These numbers are lower than the median, roughly a third of tier 1 and tier 2 respectively
        /*if (highestMined < 20_000) {
            return 1;
        } else if (highestMined < 150_000) {
            return 2;
        }

        return 3;*/
        //return 1;
        return targetDepth;
    }

    public BigInteger GetBlocksMined()
    {
        return blocksMined;
    }

    public BigInteger GetUserGems()
    {
        return userGems;
    }

    public BigInteger GetMoneyEarned()
    {
        return moneyEarned;
    }

    public BigInteger GetUserCash()
    {
        return userCash;
    }

    public BigInteger GetUserCredits()
    {
        return userCredits;
    }

    public void CollectBetaReward()
    {
        // Cannot be used again
        PlayerPrefs.SetInt("Beta", 0);
        AddGems(800_000);

        betaScreen.SetActive(false);
    }

    public void ProceedToNextMine()
    {
        // Get game data ref
        ref GameData data = ref DataPersistenceManager.Instance.GetGameDataRef();
        GameData newGameData = new();

        // In case player finished tutorial level early and completed tutorial out of order (they already know the game)
        TutorialManager.Instance.DoneTutorial();
        data.finishedTutorial = true;

        // Modify it directly and then save without calling on other scripts
        // otherwise other scripts will overwrite the modificiations we make here
        data.currentVehicle = VehicleUpgradeBayManager.Instance.GetAllDrillPrefabs()[playerVehicleDelegation.GetNextVehicleIndex(data.mineCount)].name;

        data.highestMined = newGameData.highestMined;

        data.mineCount++;
        data.targetDepth = newGameData.targetDepth;

        data.vehicleUpgradeLevels = newGameData.vehicleUpgradeLevels;
        data.upgradeBayOptionsPurchased = newGameData.upgradeBayOptionsPurchased;
        data.oreUpgrades = newGameData.oreUpgrades;

        data.discoveredOres = newGameData.discoveredOres;

        data.userCash = newGameData.userCash;
        data.cratesAvailable++;

        data.cooldownTimer = newGameData.cooldownTimer;

        // Save and reload
        DataPersistenceManager.Instance.DirectlyWriteSave();

        AnalyticsDelegator.Instance.Rebirth(data.mineCount);

        // Now ask for consent and other things if needed, otherwise AdMob will give a regulatory issue
        SceneManager.LoadScene("iOS ATT");
    }

    // For development only
    public void FreeMoney()
    {
        userCash += freeMoneyToAdd;
        UpdateCashDisplays();
        AnalyticsDelegator.Instance.TestEvent("Just testing");
    }

    public void FreeMoneyUpdate()
    {
        freeMoneyToAdd = (int)cashSlider.value;

        cashText.text = FormatPrice(freeMoneyToAdd);
    }

    public void SetTargetDepth(int newDepth)
    {
        // -1 means it was called from the On Value Changed function of the slider
        if (newDepth == -1)
        {
            if (targetDepthSlider.value == 0)
            {
                targetDepthSlider.value = 1;
            }

            newDepth = (int)targetDepthSlider.value;
        }

        targetDepth = newDepth;

        targetDepthSlider.value = targetDepth;

        targetDepthText.text = GetLocalizedValue("DEPTH: {0}", targetDepth);
    }

    public void SetMaxTargetDepth(int maxDepth)
    {
        targetDepthSlider.maxValue = maxDepth;
    }
    
    public string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key); ;

        // If no translation, just return the key
        if (entry == null)
        {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }
}
