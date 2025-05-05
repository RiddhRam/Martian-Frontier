using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Numerics;
using UnityEngine.Localization.Tables;

public class DailyChallengeDelegator : MonoBehaviour, IDataPersistence
{
    public GameObject dailyTimer;
    public GameObject challengePanel;
    public GameObject[] challengeButtons;
    public GameObject superChallengeStartButtonGO;
    public GameObject superChallengeStartButtonTextGO;
    public GameObject superChallengeSliderGO;
    public GameObject superChallengeTimerTextGO;
    public GameObject[] gemCashPurchasePanels;

    private System.Random rng;
    private AnalyticsDelegator analyticsDelegator;
    private TextMeshProUGUI dailyTimerText;
    public MineRenderer mineRenderer;
    public PlayerState playerState;
    private readonly int[]  baseCashAmountForGemPurchase = {45_000, 100_000, 250_000, 600_000};
    public GameObject challengeNoticeIcon;
    private Image[] challengeStatusIcons = new Image[6];
    private TextMeshProUGUI[] challengeTextMeshes = new TextMeshProUGUI[6];
    private TextMeshProUGUI[] rewardTextMeshes = new TextMeshProUGUI[6];
    private Slider[] challengeProgressSliders = new Slider[6];
    private TextMeshProUGUI[] challengeProgressSlidersText = new TextMeshProUGUI[6];
    private Slider superChallengeSlider;
    private TextMeshProUGUI superChallengeTimerText;
    private TextMeshProUGUI superChallengeStartButtonText;
    private DateTime endTime;
    private TimeSpan timeRemaining;
    private string timeString;
    // Used to check index
    private string[] challengeTypes = {"COLLECT ALL DAILY CHALLENGES", "MINE {0} ORES", "MINE {0} ORES OF {1}", "BUY {0} DRILLERS", "BUY {0} HAULERS"};
    // This one is not related to challenge types, just order of challenges display
    private int[] difficulty = {8, 2, 3, 6, 4, 3};
    private int baseGemReward = 180;
    // Related to the above challenge types
    // This will be multiplied to determine the goal the player needs to reach, 
    // then multiplied by the difficulty to determine the reward
    private int[] baseGoalAmount = {5, 150, 80, 1, 2};
    // Can be retrieved through seed generation
    private int[] selectedChallenges = new int[6];
    private int[] challengeValues = new int[6];
    private int[] rewardAmounts = new int[6];
    private readonly List<string> oreNeeded = new();
    // Save these
    // Last time user generated challenges, in seconds since last point (birthday)
    private int lastChallengeDate;
    private int[] challengeProgress = new int[6];
    private bool[] challengeCollection = new bool[6];
    private readonly int superChallengeStartTimer = 1200;
    private int superChallengeTimer = 1200;

    // Listen for language changes
    private void OnEnable()
    {
        // Subscribe to the locale change event
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void Awake() {
        dailyTimerText = dailyTimer.GetComponent<TextMeshProUGUI>();

        for (int i = 0; i != challengeButtons.Length; i++) {
            challengeStatusIcons[i] = challengeButtons[i].transform.GetChild(0).GetChild(0).GetComponent<Image>();
            challengeTextMeshes[i] = challengeButtons[i].transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
            rewardTextMeshes[i] = challengeButtons[i].transform.GetChild(0).GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>();
            challengeProgressSliders[i] = challengeButtons[i].transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Slider>();
            challengeProgressSlidersText[i] = challengeButtons[i].transform.GetChild(0).GetChild(1).GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>();
        }

        superChallengeSlider = superChallengeSliderGO.GetComponent<Slider>();
        superChallengeTimerText = superChallengeTimerTextGO.GetComponent<TextMeshProUGUI>();
        superChallengeStartButtonText = superChallengeStartButtonTextGO.GetComponent<TextMeshProUGUI>();
    }

    void Initialize() {
        analyticsDelegator = AnalyticsDelegator.Instance;

        if (lastChallengeDate == TimeSinceBirthday()) {
            LoadChallenges();
        } else {
            GenerateChallenges(false);
        }

        SetDailyTimer();
        ScaleAllTiers();
        StartCoroutine(TimerController());
    }

    private IEnumerator TimerController() {
        while (true) {
            timeRemaining = endTime - DateTime.UtcNow;
            timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            dailyTimerText.text = GetLocalizedValue("RESETS IN {0}", timeString);

            if (timeRemaining.TotalSeconds <= 0) {
                ResetDailyChallenges();
                SetDailyTimer();
            }

            yield return new WaitForSecondsRealtime(1);
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

    public void SetDailyTimer() {
        DateTime now = DateTime.UtcNow;
        DateTime targetTime = new(now.Year, now.Month, now.Day, 12, 0, 0, DateTimeKind.Utc);

        // If the current time is already past 12:00 PM, set it for tomorrow
        if (now > targetTime) {
            targetTime = targetTime.AddDays(1);
        }

        endTime = targetTime;
    }

    public void GenerateChallenges(bool loading) {

        lastChallengeDate = TimeSinceBirthday();

        // Don't set the last one
        for (int i = 0; i != selectedChallenges.Length; i++) {
            // Put seed inside brackets
            rng = new System.Random(lastChallengeDate + i);
            selectedChallenges[i] = rng.Next(1, challengeTypes.Length);

            // If its a super challenge, it can only be certain challenges
            if (i == 0) {
                int[] possibleValues = { 1, 2, 5 };
                selectedChallenges[i] = possibleValues[rng.Next(0, possibleValues.Length)];
            }

            // Determine goal
            challengeValues[i] = difficulty[i] * baseGoalAmount[selectedChallenges[i]];

            // Set value to 0 if new
            if (!loading) {
                challengeProgress[i] = 0;
                challengeCollection[i] = false;
            }

            // Determine reward
            rewardAmounts[i] = difficulty[i] * baseGemReward;

            // This option needs 2 variables
            if (selectedChallenges[i] == 2 && i != 5) {
                AddOreBasedOnTier(i);
            }

            challengeStatusIcons[i].color = new(255/255, 0, 0);
            challengeStatusIcons[i].transform.parent.parent.GetComponent<Button>().interactable = true;
        }

        // Super challenge has increased rewards
        rewardAmounts[0] *= 2;
        // COMPLETE ALL DAILY CHALLENGES
        selectedChallenges[5] = 0;
        challengeValues[5] = 5;

        UpdateDisplay();
    }

    public void LoadChallenges() {
        GenerateChallenges(true);
    }

    public void UpdateDisplay() {
        if (challengeTextMeshes[0] == null) {
            return;
        }

        int oreNeededCounter = 0;
        string oreName = "";

        bool uncollectedReward = false;

        for (int i = 0; i != selectedChallenges.Length; i++) {
            if (selectedChallenges[i] == 2) {
                oreName = oreNeeded[oreNeededCounter];
                oreNeededCounter++;
            }

            challengeProgress[i] = Math.Clamp(challengeProgress[i], 0, challengeValues[i]);

            if (challengePanel.activeSelf) {
                challengeTextMeshes[i].text = GetLocalizedValue(challengeTypes[selectedChallenges[i]], challengeValues[i], oreName);
                rewardTextMeshes[i].text = rewardAmounts[i].ToString();

                challengeProgressSliders[i].maxValue = challengeValues[i];
                challengeProgressSliders[i].value = challengeProgress[i];
                challengeProgressSlidersText[i].text = challengeProgress[i].ToString();
            }

            if (challengeProgress[i] >= challengeValues[i] && !challengeCollection[i]) {
                uncollectedReward = true;
            }

            if (challengeProgress[i] != challengeValues[i]) {
                challengeStatusIcons[i].color = new(255/255, 0, 0);
            } else {
                challengeStatusIcons[i].color = new(42/255f, 153/255f, 21/255f);
            }
        }

        if (uncollectedReward) {
            challengeNoticeIcon.SetActive(true);
        } else {
            challengeNoticeIcon.SetActive(false);
        }

        superChallengeSlider.maxValue = challengeValues[0];
        superChallengeSlider.value = challengeProgress[0];
    }

    public void MinedOres(Dictionary<string, int> quantities) {

        int oreNeededCounter = 0;
        foreach (string key in quantities.Keys) {

            for (int i = 0; i != selectedChallenges.Length; i++) {
                if (i == 0 && superChallengeTimer == superChallengeStartTimer) {
                    continue;
                }
                if (selectedChallenges[i] != 1 && selectedChallenges[i] != 2) {
                    continue;
                }
                if (selectedChallenges[i] == 1) {
                    challengeProgress[i] += quantities[key];
                    continue;
                }

                if (selectedChallenges[i] == 2 && oreNeeded[oreNeededCounter] == key) {
                    challengeProgress[i] += quantities[key];
                    oreNeededCounter++;
                } else if (selectedChallenges[i] == 2) {
                    oreNeededCounter++;
                }
            }
        }

        UpdateDisplay();
    }

    public void PurchasedVehicle(int vehicle) {
        // 0 = its a driller
        // 1 = its a hauler

        for (int i = 0; i != selectedChallenges.Length; i++) {
            if (selectedChallenges[i] != 3 && selectedChallenges[i] != 4) {
                continue;
            }

            // 3 if driller
            // 4 if hauler
            if (selectedChallenges[i] == 3 + vehicle) {
                challengeProgress[i]++;
            }
        }

        UpdateDisplay();
    }

    public void SoldOres(int quantity) {
        for (int i = 0; i != selectedChallenges.Length; i++) {
            if (i == 0 && (superChallengeTimer == superChallengeStartTimer || superChallengeTimer == 0)) {
                continue;
            }
            if (selectedChallenges[i] != 5) {
                continue;
            }
            challengeProgress[i] += quantity;
        }

        UpdateDisplay();
    }

    public void CollectReward(int challengeIndex) {
        if (challengeProgress[challengeIndex] != challengeValues[challengeIndex]) {
            return;
        }

        playerState.AddGems((long) rewardAmounts[challengeIndex]);
        challengeStatusIcons[challengeIndex].transform.parent.parent.GetComponent<Button>().interactable = false;
        challengeCollection[challengeIndex] = true;
        challengeProgress[5]++;
        analyticsDelegator.CollectChallengeReward(selectedChallenges[challengeIndex]);

        UpdateDisplay();
    }

    private int TimeSinceBirthday() {
        return (int) (DateTime.UtcNow.Date - new DateTime(2024, 12, 8, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public void AddOreBasedOnTier(int challengeIndex) {
        string[] oreList;

        if (playerState.GetHighestDrillTier() == 1) {
            oreList = mineRenderer.GetTier1OreNames();
        } else if (playerState.GetHighestDrillTier() == 2) {
            oreList = mineRenderer.GetTier2OreNames();
        } else {
            oreList = mineRenderer.GetTier3OreNames();
        }

        rng = new System.Random(lastChallengeDate + challengeIndex);
        oreNeeded.Add(oreList[rng.Next(oreList.Length)]);
    }
 
    public void ScaleAllTiers() {
        oreNeeded.Clear();

        for (int i = 0; i != selectedChallenges.Length; i++) {
            if (selectedChallenges[i] == 2) {
                AddOreBasedOnTier(i);
            } 
        }

        int highestDrillTier = playerState.GetHighestDrillTier();

        for (int i = 0; i != gemCashPurchasePanels.Length; i++) {
            gemCashPurchasePanels[i].GetComponent<GemCashPurchasePanel>().UpdateCashAmount(baseCashAmountForGemPurchase[i] * BigInteger.Pow(100, (-1 + highestDrillTier )));
        }

    }

    public void StartSuperChallenge() {
        StartCoroutine(CountdownSuperChallengeTimer(superChallengeStartTimer));
        analyticsDelegator.StartSuperChallenge(selectedChallenges[0]);
    }

    private IEnumerator CountdownSuperChallengeTimer(int startTime) {
        superChallengeTimer = startTime;
        int minutes;
        int seconds;

        superChallengeStartButtonGO.GetComponent<Button>().interactable = false;
        superChallengeSliderGO.SetActive(true);
        superChallengeTimerTextGO.SetActive(true);

        while (superChallengeTimer > 0 && challengeProgress[0] < challengeValues[0]) {
            superChallengeTimer--;
            // Calculate minutes and seconds
            minutes = superChallengeTimer / 60;
            seconds = superChallengeTimer % 60;
            superChallengeTimerText.text = $"{minutes}:{seconds:D2}";
            superChallengeStartButtonText.text =  $"{minutes}:{seconds:D2}";
            yield return new WaitForSeconds(1f);
        }

        superChallengeSliderGO.SetActive(false);
        superChallengeTimerTextGO.SetActive(false);
        
        if (challengeProgress[0] < challengeValues[0]) {
            challengeProgress[0] = 0;
            superChallengeStartButtonGO.GetComponent<Button>().interactable = true;
        } else {
            // If successfully completed then log how long it took
            analyticsDelegator.CompleteSuperChallenge(selectedChallenges[0], superChallengeTimer);
        }

        superChallengeStartButtonText.text = "START";

        UpdateDisplay();
    }

    private void ResetDailyChallenges() {
        superChallengeTimer = 0;

        GenerateChallenges(false);
    }

    public void LoadData(GameData data)
    {
        this.lastChallengeDate = data.lastChallengeDate;
        this.challengeProgress = data.challengeProgress;
        this.challengeCollection = data.challengeCollection;
        this.superChallengeTimer = data.superChallengeTimer;
        Initialize();

        for (int i = 0; i != challengeCollection.Length; i++) {
            if (challengeCollection[i]) {
                challengeStatusIcons[i].transform.parent.parent.GetComponent<Button>().interactable = false;
                challengeCollection[i] = true;

                if (i == 0) {
                    superChallengeStartButtonGO.SetActive(false);
                }
            }
        }

        if (superChallengeTimer < superChallengeStartTimer) {
            StartCoroutine(CountdownSuperChallengeTimer(superChallengeTimer));
        }
    }

    public void SaveData(ref GameData data)
    {
        data.lastChallengeDate = this.lastChallengeDate;
        data.challengeProgress = this.challengeProgress;
        data.challengeCollection = this.challengeCollection;
        data.superChallengeTimer = this.superChallengeTimer;
    }

    // Method that is called when the locale changes
    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateDisplay();
    }
}