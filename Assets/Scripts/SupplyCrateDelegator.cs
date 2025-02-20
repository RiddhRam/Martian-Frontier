using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SupplyCrateDelegator : MonoBehaviour
{
    
    public GameObject openCratePanel;
    public GameObject extractingSuppliesPanel;
    public GameObject collectRewardPanel;

    public TextMeshProUGUI cratesAvailableText;
    public UIDelegation uIDelegation;
    public PlayerState playerState;
    public Slider crateExtractionProgressBar;
    public TextMeshProUGUI crateExtractionPercentageText;

    public TextMeshProUGUI cashReward;
    public TextMeshProUGUI gemReward;

    private BigInteger cashRewardAmount;
    private BigInteger gemRewardAmount;


    private int cratesAvailable = 1;
    private int progressToNextCrate = 0;
    private readonly int blocksNeededToDestroy = 5000;

    public void Start()
    {
        UpdateCrateCount(cratesAvailable);
    }

    public void UpdateCrateCount(int newCount) {
        cratesAvailable = newCount;
        cratesAvailableText.text = GetLocalizedValue("{0} CRATES AVAILABLE", cratesAvailable);
    }

    public void ChangeCrateCount(int amount) {
        cratesAvailable += amount;
        cratesAvailableText.text = GetLocalizedValue("{0} CRATES AVAILABLE", cratesAvailable);
    }

    public void OpenAllCrates() {
        if (cratesAvailable <= 0) {
            uIDelegation.ShowError("NO CRATES AVAILABLE!");
            return;
        }
        StartOpeningCrate();
        UpdateCrateCount(0);
    }

    public void OpenOneCrate() {
        if (cratesAvailable <= 0) {
            uIDelegation.ShowError("NO CRATES AVAILABLE!");
            return;
        }
        StartOpeningCrate();
        ChangeCrateCount(-1);
    }

    public void StartOpeningCrate() {
        openCratePanel.SetActive(false);
        extractingSuppliesPanel.SetActive(true);

        StartCoroutine(CrateExtraction());
    }

    private IEnumerator CrateExtraction() {

        float duration = 4.0f; // Duration of the increase in seconds
        float elapsed = 0f;
        float extractionProgress;

        //audioDelegator.PlayAudio(UISoundEffects, batteryRechargeSoundEffect, 0.45f);

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

        cashRewardAmount = random.Next(10000, 200001);
        gemRewardAmount = random.Next(200, 1500);

        cashReward.text = playerState.FormatPrice(cashRewardAmount);
        gemReward.text = playerState.FormatPrice(gemRewardAmount);

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

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

}