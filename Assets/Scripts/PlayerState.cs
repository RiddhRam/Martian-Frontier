using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField]
    private int userCash;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private int savedAmountSubtract;
    private int userXP;
    private int savedAmountXP;
    [SerializeField]
    private int blocksMined;
    [SerializeField]
    private int materialsSold;
    [SerializeField]
    private int moneyEarned;
    [SerializeField]
    private List<string> vehiclesOwned = new();
    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    // REMEMBER TO UPDATE IN RefineryController TOO
    private readonly int[] materialPrices = {50, 150, 250};

    // Validate and add cash
    // This version of AddCash is called when the user drops some materials off at the refinery
    public void AddCash(int cashToAdd, int[] materialCount) {

            // Count the prices of all materials
            int amountToAdd = 0;
            for (int i = 0; i != materialCount.Length; i++) {
                amountToAdd += materialCount[i] * materialPrices[i];
            }

            // If the amounts are correct, add the money
            if (amountToAdd == cashToAdd) {
                userCash += cashToAdd;
                moneyEarned += cashToAdd;
            }
    }

    // Validate again and subtract cash
    // Only call if VerifyEnoughCash was called
    public void SubtractCash(int amountToSubtract, GameObject objectBeingPurchased) {
        // objectBeingPurchased is some upgrade or vehicle being bought
        UpdateSubtractedAmount(objectBeingPurchased);

        if (amountToSubtract == savedAmountSubtract) {
            userCash -= amountToSubtract;
            // If it has a driller or hauler controller, add it to the list of vehicles owned
            if (objectBeingPurchased.transform.GetChild(1).GetComponent<DrillerController>() || objectBeingPurchased.GetComponent<HaulerController>()) {
                vehiclesOwned.Add(objectBeingPurchased.name);
            }
        }
    }

    // Validate and add XP
    public void AddXP(int amountToAddXP, GameObject objectReason) {
        // objectReason can be something the user dropped off or rebirth

        if (amountToAddXP == savedAmountXP) {
            userXP += amountToAddXP;
            // Complete Action
        }
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

    public void NewBlockMined() {
        blocksMined++;
    }

    public void NewMaterialSold() {
        materialsSold++;
    }

    private void UpdateSubtractedAmount(GameObject objectBeingPurchased) {
        // If hauler
        if (objectBeingPurchased.GetComponent<HaulerController>()) {
            //savedAmountSubtract = objectBeingPurchased.GetComponent<HaulerController>().GetPrice();
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
}