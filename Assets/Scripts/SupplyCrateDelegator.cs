using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SupplyCrateDelegator : MonoBehaviour, IDataPersistence
{
    
    public GameObject openCratePanel;
    public GameObject extractingSuppliesPanel;
    public GameObject collectRewardPanel;
    public GameObject doubleRewardsButtons;

    public TextMeshProUGUI[] crateDisplays;
    public PlayerState playerState;
    public UpgradesDelegator upgradesDelegator;
    public Slider crateExtractionProgressBar;
    public TextMeshProUGUI crateExtractionPercentageText;

    public TextMeshProUGUI cashReward;
    public TextMeshProUGUI gemReward;

    public Slider[] blocksNeededBars;
    public TextMeshProUGUI blocksNeededMiniBarText;
    public TextMeshProUGUI blocksNeededMainBarText;

    public AudioClip crateUnlockSoundEffect;
    public AudioSource UISoundEffects;

    private BigInteger cashRewardAmount;
    private BigInteger gemRewardAmount;

    public Image supplyCrateButtonIcon;
    public GameObject crateNoticeIcon;

    public bool disableAds = false;

    private int cratesAvailable = 1;
    private int progressToNextCrate = 0;
    private const int blocksNeededToDestroy = 4000;

    public bool adWatchedAlready = false;

    public void UpdateBlocksNeededBars() {
        int blocksLeft = blocksNeededToDestroy - progressToNextCrate;

        for (int i = 0; i != blocksNeededBars.Length; i++) {
            blocksNeededBars[i].value = (float) progressToNextCrate / blocksNeededToDestroy;
        }

        blocksNeededMiniBarText.text = blocksLeft.ToString();
        blocksNeededMainBarText.text = GetLocalizedValue("{0} BLOCKS LEFT", blocksLeft);
    }

    public void UpdateProgressToNextCrate(int amount) {
        progressToNextCrate = amount;
        CheckIfEarnedNewCrate();
    }

    public void ChangeProgressToNextCrate(int amount) {
        progressToNextCrate += amount;
        CheckIfEarnedNewCrate();
    }

    public void CheckIfEarnedNewCrate() {
        if (progressToNextCrate < blocksNeededToDestroy) {
            UpdateBlocksNeededBars();
            return;
        }

        ChangeCrateCount(1);
        // Doesn't cause an infinite recursion loop because of the if statement above
        UpdateProgressToNextCrate(0);
    }

    public void UpdateCrateCount(int newCount) {
        cratesAvailable = newCount;
        UpdateCrateDisplay();
    }

    public void ChangeCrateCount(int amount) {
        cratesAvailable += amount;
        UpdateCrateDisplay();
    }

    public void UpdateCrateDisplay() {
        string cratesAvailableText = cratesAvailable.ToString();
        for (int i = 0; i != crateDisplays.Length; i++) {
            crateDisplays[i].text = cratesAvailableText;
        }

        if (cratesAvailable > 0) {
            crateNoticeIcon.SetActive(true);
        } else {
            crateNoticeIcon.SetActive(false);
        }
    }

    public void OpenAllCrates() {
        if (cratesAvailable <= 0) {
            UIDelegation.Instance.ShowError("NO CRATES AVAILABLE!");
            return;
        }
        StartOpeningCrate(true);
    }

    public void OpenOneCrate() {
        if (cratesAvailable <= 0) {
            UIDelegation.Instance.ShowError("NO CRATES AVAILABLE!");
            return;
        }
        StartOpeningCrate(false);
    }

    public void StartOpeningCrate(bool openAll) {
        openCratePanel.SetActive(false);
        extractingSuppliesPanel.SetActive(true);

        StartCoroutine(CrateExtraction(openAll));
    }

    private IEnumerator CrateExtraction(bool openAll) {
        adWatchedAlready = false;
        
        float duration = 5.0f; // Duration of the increase in seconds
        float elapsed = 0f;
        float extractionProgress;

        AudioDelegator.Instance.PlayAudio(UISoundEffects, crateUnlockSoundEffect, 1);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            extractionProgress = (int) Mathf.Lerp(0, crateExtractionProgressBar.maxValue, elapsed / duration);

            crateExtractionProgressBar.value = extractionProgress;

            // Round up to nearest int
            crateExtractionPercentageText.text = Mathf.CeilToInt(extractionProgress * 100 / crateExtractionProgressBar.maxValue) + "%";

            yield return null; // Wait for the next frame
        }

        crateExtractionProgressBar.value = crateExtractionProgressBar.maxValue;
        crateExtractionPercentageText.text = "100%";

        System.Random random = new System.Random();

        // Randomly choose reward amounts
        // Cash reward is based on the highest value of ores the player mined in a single mine
        double minReward = playerState.GetHighestMined() * 0.1;
        double maxReward = playerState.GetHighestMined() * 0.2f;

        double rawCashReward = minReward + random.NextDouble() * (maxReward - minReward);
        cashRewardAmount = new BigInteger(rawCashReward * (1 + upgradesDelegator.crateMultiplier));
        gemRewardAmount = (int) (random.Next(600, 1200) * (1 + upgradesDelegator.crateMultiplier));

        if (openAll) {
            cashRewardAmount *= cratesAvailable;
            gemRewardAmount *= cratesAvailable;
            try {
                AnalyticsDelegator.Instance.OpenCrate(true, cratesAvailable);
            } catch {
            }

            UpdateCrateCount(0);
        } else {
            try {
                AnalyticsDelegator.Instance.OpenCrate(false, 1);
            } catch {
            }

            ChangeCrateCount(-1);
        }

        cashReward.text = playerState.FormatPrice(cashRewardAmount);
        gemReward.text = playerState.FormatPrice(gemRewardAmount);

        if (Application.internetReachability != NetworkReachability.NotReachable && !disableAds) {
            doubleRewardsButtons.SetActive(true);
        }

        extractingSuppliesPanel.SetActive(false);
        collectRewardPanel.SetActive(true);
    }

    public void CollectRewards() {
        playerState.AddCash((long) cashRewardAmount);
        playerState.AddGems((long) gemRewardAmount);

        cashRewardAmount = 0;
        gemRewardAmount = 0;

        collectRewardPanel.SetActive(false);
        openCratePanel.SetActive(true);
    }

    public void DoubleRewardsActivated() {
        adWatchedAlready = true;
        cashRewardAmount *= 2;
        gemRewardAmount *= 2;

        cashReward.text = playerState.FormatPrice(cashRewardAmount);
        gemReward.text = playerState.FormatPrice(gemRewardAmount);

        if (!disableAds) {
            doubleRewardsButtons.SetActive(false);
        }
        
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

    public int GetCratesAvailable() {
        return cratesAvailable;
    }
    
    public void LoadData(GameData data)
    {

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            disableAds = true;
        }
        
        this.cratesAvailable = data.cratesAvailable;
        this.progressToNextCrate = data.progressToNextCrate;

        if (progressToNextCrate > blocksNeededToDestroy)
        {
            progressToNextCrate = blocksNeededToDestroy;
        }

        UpdateCrateCount(cratesAvailable);
        UpdateProgressToNextCrate(progressToNextCrate);
    }

    public void SaveData(ref GameData data)
    {
        data.cratesAvailable = this.cratesAvailable;
        data.progressToNextCrate = this.progressToNextCrate;
    }
}