using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class RefineryUpgrades : MonoBehaviour
{
    [SerializeField]
    private long[] upgradeValues;
    [SerializeField]
    private long[] upgradePrices;
    private long currentValue;
    public GameObject Refinery;
    public GameObject PlayerState;

    void Start() {
        currentValue = upgradeValues[0];
        UpdateDisplay(0);
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
            return;
        }

        PlayerState.GetComponent<PlayerState>().SubtractCash(upgradePrices[currentIndex]);

        if (upgradeValues[currentIndex] != upgradeValues[^1]) {
            currentValue = upgradeValues[currentIndex + 1];
            currentIndex++;
        }

        UpdateDisplay(currentIndex);

        if (gameObject.name == "Capacity Panel") {
            Refinery.GetComponent<RefineryController>().UpgradeBattery(currentValue);
            return;
        }

        Refinery.GetComponent<RefineryController>().ImproveEfficiency(currentValue);
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
}
