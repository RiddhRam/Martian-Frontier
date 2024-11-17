using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField]
    private int userCash = 0;
    [SerializeField]
    // Use this to verify the amount of money to add or subtract across verifications
    private int savedAmountSubtract = 0;
    private int userXP = 0;
    private int savedAmountXP = 0;
    [SerializeField]
    private int blocksMined = 0;
    [SerializeField]
    private int materialsSold = 0;
    [SerializeField]
    private int moneyEarned = 0;
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

        if (amountToSubtract == savedAmountSubtract) {
            userCash -= amountToSubtract;
            // Complete Action
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
        // savedAmountSubtract = 

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
}
