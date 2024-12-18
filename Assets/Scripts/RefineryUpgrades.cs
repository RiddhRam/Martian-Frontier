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
    public GameObject PlayerState;
    private RefineryController refineryController;

    public void InitializeRefinery(float newValue, GameObject refineryDropOffGO) {
        refineryController = refineryDropOffGO.GetComponent<RefineryController>();

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

        if (!PlayerState.GetComponent<PlayerState>().VerifyEnoughCash(upgradePrices[currentIndex])) {
            transform.parent.parent.parent.parent.GetComponent<UIDelegation>().ShowError("NOT ENOUGH CASH!");
            return;
        }

        PlayerState.GetComponent<PlayerState>().SubtractCash(upgradePrices[currentIndex]);

        if (upgradeValues[currentIndex] != upgradeValues[^1]) {
            currentIndex++;
            currentValue = upgradeValues[currentIndex];
        }

        UpdateDisplay(currentIndex);

        if (gameObject.name == "Capacity Panel") {
            refineryController.UpgradeBattery(currentValue);
            AnalyticsDelegator.Instance.RefineryUpgrade("Capacity", currentIndex + 1);
            return;
        }

        refineryController.ImproveEfficiency(currentValue);
        AnalyticsDelegator.Instance.RefineryUpgrade("Efficiency", currentIndex + 1);
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
            transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = upgradeValues[currentIndex].ToString();
            transform.GetChild(4).GetComponent<Button>().interactable = false;
            transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = "MAX";
            transform.GetChild(4).GetComponent<Image>().color = new(255, 0, 0);
        } else {
            transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = upgradeValues[currentIndex].ToString();
            if (gameObject.name == "Efficiency Panel") {
                transform.GetChild(3).GetComponent<TextMeshProUGUI>().text += "%";
            }
            transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(upgradePrices[currentIndex]);
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
