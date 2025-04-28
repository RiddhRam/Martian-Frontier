using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class OreMagnetUpgrades : MonoBehaviour, IDataPersistence
{
    [SerializeField] private PlayerState playerState;
    [SerializeField] private CreditMagnet creditMagnet;

    [SerializeField] private TextMeshProUGUI rangeLevelText;
    [SerializeField] private TextMeshProUGUI strengthLevelText;

    [SerializeField] private TextMeshProUGUI rangeValueText;
    [SerializeField] private TextMeshProUGUI strengthValueText;

    [SerializeField] private TextMeshProUGUI rangePriceText;
    [SerializeField] private TextMeshProUGUI strengthPriceText;

    private SerializableDictionary<string, int> oreMagnetUpgrades;
    private readonly int[] upgradePrices = { 300, 500, 700, 1000, 1300, 1700, 2200, 2800, 3500, 4300, 5200, 6200, 7400, 8700, 10100, 11800, 13600, 15700, 18000, 20500 };

    public void UpgradeMagnet(string upgradeType) {
        int level = GetUpgradeLevel(upgradeType);

        if (!playerState.VerifyEnoughCredits(upgradePrices[level])) {
            Debug.Log("No Credits");
            return;
        }

        playerState.SubtractCredits(upgradePrices[level]);

        level++;
        oreMagnetUpgrades[upgradeType] = level;
        
        UpdatePowerPanels();
        SetPowers();
    }

    public int GetUpgradeLevel(string key) {
        if (!oreMagnetUpgrades.ContainsKey(key)) {
            return 0;
        }

        return oreMagnetUpgrades[key];
    }

    public void SetPowers() {
        creditMagnet.magnetRadius = 10 + GetUpgradeLevel("Range");
        creditMagnet.pullForce = 8 + GetUpgradeLevel("Strength");
    }

    public void UpdatePowerPanels() {
        // Range
        int rangeLevel = GetUpgradeLevel("Range");
        
        rangeLevelText.text = GetLocalizedValue("LEVEL {0}", rangeLevel);
        rangeValueText.text = GetLocalizedValue("{0} BLOCKS", 10 + rangeLevel);

        if (rangeLevel == upgradePrices.Length) {
            rangePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            rangePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            rangePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            rangePriceText.text = "MAX";
        } else {
            rangePriceText.text = FormatPrice(upgradePrices[rangeLevel]);
        }
        

        // Strength
        int strengthLevel = GetUpgradeLevel("Strength");

        strengthLevelText.text = GetLocalizedValue("LEVEL {0}", strengthLevel);
        strengthValueText.text = GetLocalizedValue("{0} FORCE", 8 + strengthLevel);

        if (strengthLevel == upgradePrices.Length) {
            strengthPriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            strengthPriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            strengthPriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            strengthPriceText.text = "MAX";
        } else {
            strengthPriceText.text = FormatPrice(upgradePrices[strengthLevel]);
        }
    }

    public void ResetUpgrades() {
        oreMagnetUpgrades.Clear();
    }

    public void LoadData(GameData data) {
        this.oreMagnetUpgrades = data.oreMagnetUpgrades;

        UpdatePowerPanels();
        SetPowers();
    }

    public void SaveData(ref GameData data) {
        data.oreMagnetUpgrades = this.oreMagnetUpgrades;
    }

    public string FormatPrice(int price)
    {
        if (price >= 1_000_000_000)
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

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key);;

        // If no translation, just return the key
        if (entry == null) {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }


}