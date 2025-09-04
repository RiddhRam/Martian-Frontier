using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class OreBlasterUpgrades : MonoBehaviour, IDataPersistence
{
    [SerializeField] private PlayerState playerState;
    [SerializeField] private OreBlaster oreBlaster;
    [SerializeField] private OreBlasterDailyChallengeDelegator oreBlasterDailyChallengeDelegator;

    [SerializeField] private GameObject upgradeNoticeIcon;

    [SerializeField] private TextMeshProUGUI radiusLevelText;
    [SerializeField] private TextMeshProUGUI reloadLevelText;

    [SerializeField] private TextMeshProUGUI radiusValueText;
    [SerializeField] private TextMeshProUGUI reloadValueText;

    [SerializeField] private TextMeshProUGUI radiusPriceText;
    [SerializeField] private TextMeshProUGUI reloadPriceText;

    private SerializableDictionary<string, int> oreBlasterUpgrades;
    private readonly int[] upgradePrices = { 800, 1700, 3000, 5000, 7800, 11400, 16100, 21900, 29300, 38500 };

    public void UpgradeExplosive(string upgradeType) {
        int level = GetUpgradeLevel(upgradeType);

        if (!playerState.VerifyEnoughCredits(upgradePrices[level])) {
            UIDelegation.Instance.ShowError("NOT ENOUGH CREDITS!");
            return;
        }

        playerState.SubtractCredits(upgradePrices[level]);

        level++;
        oreBlasterUpgrades[upgradeType] = level;
        
        UpdatePowerPanels();
        SetPowers();

        EnableNoticeIconIfNeeded();

        oreBlasterDailyChallengeDelegator.LeveledUpPower(GetUpgradeLevel("Radius"), GetUpgradeLevel("Reload"));
        
        AnalyticsDelegator.Instance.TechLabUpgrade(upgradeType);
    }

    public int GetUpgradeLevel(string key) {
        if (!oreBlasterUpgrades.ContainsKey(key)) {
            return 0;
        }

        return oreBlasterUpgrades[key];
    }

    public void SetPowers() {
        oreBlaster.destroyRadius = GetBlastRadius();
        oreBlaster.blastInterval = GetReloadTime();
    }

    public void UpdatePowerPanels() {
        // Radius
        int radiusLevel = GetUpgradeLevel("Radius");
        
        radiusLevelText.text = GetLocalizedValue("LEVEL {0}", radiusLevel);
        radiusValueText.text = GetLocalizedValue("{0} BLOCKS", GetBlastRadius());

        if (radiusLevel == upgradePrices.Length) {
            radiusPriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            radiusPriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            radiusPriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            radiusPriceText.text = "MAX";
        } else {
            radiusPriceText.text = playerState.FormatPrice(upgradePrices[radiusLevel]);
        }
        
        // Reload
        int reloadLevel = GetUpgradeLevel("Reload");

        reloadLevelText.text = GetLocalizedValue("LEVEL {0}", reloadLevel);
        reloadValueText.text = GetLocalizedValue("{0} SECONDS", GetReloadTime());

        if (reloadLevel == upgradePrices.Length) {
            reloadPriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            reloadPriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            reloadPriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            reloadPriceText.text = "MAX";
        } else {
            reloadPriceText.text = playerState.FormatPrice(upgradePrices[reloadLevel]);
        }
    }

    public int GetBlastRadius() {
        return 8 + GetUpgradeLevel("Radius");
    }

    public float GetReloadTime() {
        return 5f - (GetUpgradeLevel("Reload") * 0.25f);
    }
    
    public void EnableNoticeIconIfNeeded(){
        BigInteger userCredits = playerState.GetUserCredits();

        if ((GetUpgradeLevel("Radius") < upgradePrices.Length && userCredits > upgradePrices[GetUpgradeLevel("Radius")]) 
        || (GetUpgradeLevel("Reload") < upgradePrices.Length && userCredits > upgradePrices[GetUpgradeLevel("Reload")])) {
            upgradeNoticeIcon.SetActive(true);
        } else {
            upgradeNoticeIcon.SetActive(false);
        }
    }

    public void LoadData(GameData data) {
        this.oreBlasterUpgrades = data.oreBlasterUpgrades;

        UpdatePowerPanels();
        SetPowers();
    }

    public void SaveData(ref GameData data) {
        data.oreBlasterUpgrades = this.oreBlasterUpgrades;
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