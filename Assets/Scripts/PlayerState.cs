using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;

public class PlayerState : MonoBehaviour, IDataPersistence
{
    public GameObject[] cashDisplays;

    [SerializeField]
    private BigInteger userCash;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private long savedAmountSubtract;
    private BigInteger userXP;
    [SerializeField]
    private BigInteger blocksMined;
    [SerializeField]
    private BigInteger materialsSold;
    [SerializeField]
    private BigInteger moneyEarned;
    [SerializeField]
    private List<string> vehiclesOwned = new();
    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    // REMEMBER TO UPDATE IN RefineryController TOO
    private readonly int[] materialPrices = {50, 150, 250, 5000, 15000, 25000, 500000, 1500000, 2500000};

    void Start() {
        UpdateCashDisplays();
    }

    // Validate and add cash
    // This version of AddCash is called when the user drops some materials off at the refinery
    public void AddCash(long cashToAdd, int[] materialCount) {

        // Count the prices of all materials
        long amountToAdd = 0;
        for (int i = 0; i != materialCount.Length; i++) {
            amountToAdd += materialCount[i] * materialPrices[i];
        }

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
            if (objectBeingPurchased.GetComponent<HaulerController>() || objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>()) {
                vehiclesOwned.Add(objectBeingPurchased.name);
            }
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
        this.userCash = BigInteger.Parse(data.userCash);
        this.userXP = BigInteger.Parse(data.userXP);
        this.blocksMined = BigInteger.Parse(data.blocksMined);
        this.materialsSold = BigInteger.Parse(data.materialsSold);
        this.moneyEarned = BigInteger.Parse(data.moneyEarned);
        this.vehiclesOwned = data.vehiclesOwned;
    }

    public void SaveData(ref GameData data) {
        data.userCash = this.userCash.ToString();
        data.userXP = this.userXP.ToString();
        data.blocksMined = this.blocksMined.ToString();
        data.materialsSold = this.materialsSold.ToString();
        data.moneyEarned = this.moneyEarned.ToString();
        data.vehiclesOwned = this.vehiclesOwned;
    }
}