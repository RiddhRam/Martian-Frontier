using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour, IDataPersistence
{
    public GameObject[] cashDisplays;
    public GameObject[] xpDisplays;
    public GameObject garagePanel;
    // Can't serialize field on BigIntegers
    private BigInteger userCash;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private long savedAmountSubtract;
    private BigInteger userXP;
    private BigInteger blocksMined;
    private BigInteger materialsSold;
    private BigInteger moneyEarned;
    private List<string> vehiclesOwned = new();
    private int[] materialPrices;
    private RefineryController refineryController;
    private bool loading = true;
    [SerializeField]
    private float rebirthProfitMultiplier;

    void Start() {
        materialPrices = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().GetMaterialPrices();
        UpdateCashDisplays();
        UpdateXPDisplays();
    }

    // Validate and add cash
    // This version of AddCash is called when the user drops some materials off at the refinery
    public void AddCash(long cashToAdd, int[] materialCount) {

        // Count the prices of all materials
        long amountToAdd = 0;
        for (int i = 0; i != materialCount.Length; i++) {
            amountToAdd += materialCount[i] * materialPrices[i];
        }

        amountToAdd = (int) (amountToAdd * refineryController.GetTotalProfitMultiplier());

        // If the amounts are correct, add the money
        if (amountToAdd == cashToAdd) {
            userCash += cashToAdd;
            moneyEarned += cashToAdd;
        }
        
        UpdateCashDisplays();
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
            } else if (objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>()) {
                vehicleType = "Driller";
                tier = objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();
            }

            // If it's a hauler or driller, it won't be null
            if (vehicleType == null) {
                return;
            }
            
            vehiclesOwned.Add(objectBeingPurchased.name);
            AnalyticsDelegator.Instance.PurchaseVehicle(objectBeingPurchased.name, vehicleType, tier);
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

    public void NewBlockMined(bool oreMined) {
        // Gain 1 xp for mining a block, but gain 4 additional for mining an ore
        // Total 5 xp for mining an ore
        if (oreMined) {
            userXP += 4;
        }
        userXP++;

        UpdateXPDisplays();
        
        blocksMined++;
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

    // Update all UI elements that show the user's money
    public void UpdateCashDisplays() {
        string cashText = "$" + FormatPrice(userCash);

        for (int i = 0; i != cashDisplays.Length; i++) {
            cashDisplays[i].GetComponent<TextMeshProUGUI>().text = cashText;
        }
    }

    // The FormatPrice in other places is slightly different. 
    // Here we need to purposefully round down so the user doesn't 
    // overestimate their money and buy something they can't afford
    private string FormatPrice(BigInteger price)
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

    public void LoadData(GameData data) {
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        
        loading = true;
        this.userCash = BigInteger.Parse(data.userCash);
        this.userXP = BigInteger.Parse(data.userXP);
        this.blocksMined = BigInteger.Parse(data.blocksMined);
        this.materialsSold = BigInteger.Parse(data.materialsSold);
        this.moneyEarned = BigInteger.Parse(data.moneyEarned);
        this.vehiclesOwned = data.vehiclesOwned;
        this.rebirthProfitMultiplier = data.rebirthProfitMultiplier;
        refineryController.SetRebirthProfitMultiplier(rebirthProfitMultiplier);
        
        loading = false;
        StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
    }

    public void SaveData(ref GameData data) {
        data.userCash = this.userCash.ToString();
        data.userXP = this.userXP.ToString();
        data.blocksMined = this.blocksMined.ToString();
        data.materialsSold = this.materialsSold.ToString();
        data.moneyEarned = this.moneyEarned.ToString();
        data.vehiclesOwned = this.vehiclesOwned;
        data.rebirthProfitMultiplier = this.rebirthProfitMultiplier;
    }

    private void UpdateXPDisplays() {
        int baseXP = 500; // XP needed for level 0 to 1
        int increment = 500; // Additional XP per level
        int level = 0; // Start at level 0
        BigInteger remainingXP = userXP; // Start with total user XP

        while (remainingXP >= baseXP + level * increment) {
            remainingXP -= baseXP + level * increment;
            level++;
        }

        float profitMultiplier = refineryController.GetLevelProfitMultiplier();
        float calculatedValue = level * 0.01f;
        float tolerance = 0.005f;

        if ((profitMultiplier < calculatedValue - tolerance) && !loading) {
            AnalyticsDelegator.Instance.LevelUp(level);
        }

        // For each level, add 1% to the profit multiplier
        refineryController.SetLevelProfitMultiplier(calculatedValue);

        for (int i = 0; i != xpDisplays.Length; i++) {
            xpDisplays[i].GetComponent<Slider>().value = (float) ((double) remainingXP/(baseXP + level * increment));
            xpDisplays[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = level.ToString();
        }
    }

    public void Rebirth() {
        long rebirthPrice = GameObject.Find("Material Profit Panel").GetComponent<ProfitPanelDelegator>().GetRebirthPrice();
        if (!VerifyEnoughCash(rebirthPrice)) {
            GameObject.Find("UI").GetComponent<UIDelegation>().ShowError("NOT ENOUGH CASH!");
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

        AnalyticsDelegator.Instance.Rebirth((int) Mathf.Round(rebirthProfitMultiplier / 0.01f));

        UpdateCashDisplays();
        UpdateXPDisplays();
    }

    // For development only
    public void FreeMoney() {
        userCash += 1_000_000_000;
        UpdateCashDisplays();
        AnalyticsDelegator.Instance.TestEvent("Just testing");
    }

}