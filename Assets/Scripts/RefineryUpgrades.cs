using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class RefineryUpgrades : MonoBehaviour
{
    [SerializeField]
    private long[] upgradeValues;
    [SerializeField]
    private long[] upgradePrices;
    // Current Value of this upgrade type
    // It is an element of upgradeValues
    private float currentValue;
    public PlayerState PlayerState;
    public RefineryController refineryController;
    public AnalyticsDelegator analyticsDelegator;
    public UIDelegation uIDelegation;
    public TextMeshProUGUI upgradeValueText;
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    public Image upgradeButtonImage;

    public void InitializeRefinery(float newValue) {
        currentValue = newValue;
        LoadCorrectUpgrade();
    }

    public void UpgradeRefinery() {

        int currentIndex = 0;

        // Find the current index
        for (int i = 0; i != upgradeValues.Length; i++) {
            if (upgradeValues[i] != currentValue) {
                continue;
            }
            currentIndex = i;
            break;
        }

        if (!PlayerState.VerifyEnoughGems(upgradePrices[currentIndex])) {
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return;
        }

        PlayerState.SubtractGems(upgradePrices[currentIndex]);

        if (upgradeValues[currentIndex] != upgradeValues[^1]) {
            currentIndex++;
            currentValue = upgradeValues[currentIndex];
        }

        UpdateDisplay(currentIndex);

        refineryController.SetBattery(currentValue);
        analyticsDelegator.RefineryUpgrade("Capacity", currentIndex + 1);
        return;
    }

    // Only called upon when loading game
    public void LoadCorrectUpgrade() {
        int currentIndex = 0;

        // Find the current index
        for (int i = 0; i != upgradeValues.Length; i++) {
            if (upgradeValues[i] == currentValue) {
                currentIndex = i;
                break;
            }
        }

        UpdateDisplay(currentIndex);
    }

    private void UpdateDisplay(int currentIndex) {
        // If the final upgrade, make it unavailable to purchase anymore
        if (upgradeValues[currentIndex] == upgradeValues[^1]) {
            upgradeValueText.text = upgradeValues[currentIndex].ToString();
            upgradeButton.interactable = false;
            upgradeButtonText.text = "MAX";
            upgradeButtonImage.color = new(255, 0, 0);
        } else {
            upgradeValueText.text = upgradeValues[currentIndex].ToString();
            upgradeButton.interactable = true;
            upgradeButtonImage.color = new(57/255f, 255, 20/255f);
            upgradeButtonText.text = FormatPrice(upgradePrices[currentIndex]);
        }
    }

    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000_000_000) {
            return (price / 1_000_000_000_000_000f).ToString("0.#") + "Q"; // For quadrillions
        }
        if (price >= 1_000_000_000_000) {
            return (price / 1_000_000_000_000f).ToString("0.#") + "T"; // For trillions
        }
        else if (price >= 1_000_000_000) {
            return (price / 1_000_000_000f).ToString("0.#") + "B"; // For billions
        }
        else if (price >= 1_000_000)
        {
            return (price / 1_000_000f).ToString("0.#") + "M"; // For millions
        }
        else if (price >= 1_000)
        {
            return (price / 1_000f).ToString("0.#") + "K"; // For thousands
        }

        return price.ToString(); // For smaller numbers
    }

    public float GetUpgradeValue() {
        return currentValue;
    }
}
