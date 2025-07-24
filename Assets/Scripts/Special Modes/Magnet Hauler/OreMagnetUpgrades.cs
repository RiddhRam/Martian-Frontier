using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class OreMagnetUpgrades : MonoBehaviour, IDataPersistence
{
    [SerializeField] private PlayerState playerState;
    [SerializeField] private CreditMagnet creditMagnet;
    [SerializeField] MagnetHaulerDailyChallengeDelegator magnetHaulerDailyChallengeDelegator;

    [SerializeField] private GameObject upgradeNoticeIcon;

    [SerializeField] private TextMeshProUGUI rangeLevelText;
    [SerializeField] private TextMeshProUGUI strengthLevelText;

    [SerializeField] private TextMeshProUGUI rangeValueText;
    [SerializeField] private TextMeshProUGUI strengthValueText;

    [SerializeField] private TextMeshProUGUI rangePriceText;
    [SerializeField] private TextMeshProUGUI strengthPriceText;

    private SerializableDictionary<string, int> magnetHaulerUpgrades;
    private readonly int[] upgradePrices = { 300, 500, 700, 1000, 1300, 1700, 2200, 2800, 3500, 4300, 5200, 6200, 7400, 8700, 10100, 11800, 13600, 15700, 18000, 20500 };


    public void UpgradeMagnet(string upgradeType)
    {
        int level = GetUpgradeLevel(upgradeType);

        if (!playerState.VerifyEnoughCredits(upgradePrices[level]))
        {
            UIDelegation.Instance.ShowError("NOT ENOUGH CREDITS!");
            return;
        }

        playerState.SubtractCredits(upgradePrices[level]);

        level++;
        magnetHaulerUpgrades[upgradeType] = level;

        UpdatePowerPanels();
        SetPowers();

        EnableNoticeIconIfNeeded();

        magnetHaulerDailyChallengeDelegator.LeveledUpPower(GetUpgradeLevel("Range"), GetUpgradeLevel("Strength"));

        AnalyticsDelegator.Instance.TechLabUpgrade(upgradeType);
    }

    public int GetUpgradeLevel(string key) {
        if (!magnetHaulerUpgrades.ContainsKey(key)) {
            return 0;
        }

        return magnetHaulerUpgrades[key];
    }

    public void SetPowers() {
        creditMagnet.magnetRadius = GetMagnetRadius();
        creditMagnet.pullForce = GetMagnetStrength();
    }

    public void UpdatePowerPanels() {
        // Range
        int rangeLevel = GetUpgradeLevel("Range");
        
        rangeLevelText.text = GetLocalizedValue("LEVEL {0}", rangeLevel);
        rangeValueText.text = GetLocalizedValue("{0} BLOCKS", GetMagnetRadius());

        if (rangeLevel == upgradePrices.Length) {
            rangePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            rangePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            rangePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            rangePriceText.text = "MAX";
        } else {
            rangePriceText.text = playerState.FormatPrice(upgradePrices[rangeLevel]);
        }

        // Strength
        int strengthLevel = GetUpgradeLevel("Strength");

        strengthLevelText.text = GetLocalizedValue("LEVEL {0}", strengthLevel);
        strengthValueText.text = GetLocalizedValue("{0} FORCE", GetMagnetStrength());

        if (strengthLevel == upgradePrices.Length) {
            strengthPriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            strengthPriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            strengthPriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            strengthPriceText.text = "MAX";
        } else {
            strengthPriceText.text = playerState.FormatPrice(upgradePrices[strengthLevel]);
        }
    }

    public int GetMagnetRadius() {
        return 10 + GetUpgradeLevel("Range");
    }

    public int GetMagnetStrength() {
        return 8 + GetUpgradeLevel("Strength");
    }

    public void EnableNoticeIconIfNeeded(){
        BigInteger userCredits = playerState.GetUserCredits();

        if ((GetUpgradeLevel("Range") < upgradePrices.Length && userCredits > upgradePrices[GetUpgradeLevel("Range")]) 
        || (GetUpgradeLevel("Strength") < upgradePrices.Length && userCredits > upgradePrices[GetUpgradeLevel("Strength")])) {
            upgradeNoticeIcon.SetActive(true);
        } else {
            upgradeNoticeIcon.SetActive(false);
        }
    }

    public void LoadData(GameData data) {

        this.magnetHaulerUpgrades = data.magnetHaulerUpgrades;

        UpdatePowerPanels();
        SetPowers();
    }

    public void SaveData(ref GameData data) {
        data.magnetHaulerUpgrades = this.magnetHaulerUpgrades;
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