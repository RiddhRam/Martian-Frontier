using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;
using System.Collections.Generic;

public class DailyChallengeDelegator : MonoBehaviour, IDataPersistence
{
    public GameObject mineRenderGO;
    public GameObject dailyTimer;
    public GameObject[] challengeDescriptionTexts;
    public GameObject[] rewardTexts;
    public GameObject[] challengeProgressSlidersGO;
    public GameObject[] challengeProgressSlidersTextGO;
    public GameObject superChallengeSliderGO;
    public GameObject superChallengeTimerTextGO;

    private System.Random rng;
    private AnalyticsDelegator analyticsDelegator;
    private TextMeshProUGUI dailyTimerText;
    private MineRenderer mineRenderer;
    private TextMeshProUGUI[] challengeTextMeshes = new TextMeshProUGUI[6];
    private TextMeshProUGUI[] rewardTextMeshes = new TextMeshProUGUI[6];
    private Slider[] challengeProgressSliders = new Slider[6];
    private TextMeshProUGUI[] challengeProgressSlidersText = new TextMeshProUGUI[6];
    private Slider superChallengeSlider;
    private TextMeshProUGUI superChallengeTimerText;
    private string timerMessage;
    private DateTime endTime;
    private TimeSpan timeRemaining;
    private string timeString;
    // Used to check index
    private string[] challengeTypes = {"COMPLETE ALL DAILY CHALLENGES", "MINE {0} ORES", "MINE {0} ORES OF {1}", "BUY {0} DRILLERS", "BUY {0} HAULERS", "SELL {0} ORES"};
    private string[][] oreNames;
    // This one is not related to challenge types, just order of challenges display
    private int[] difficulty = {5, 1, 2, 5, 3, 1};
    private int baseGemReward = 3;
    // Related to the above challenge types
    private int[] baseGoalAmount = {5, 50, 30, 1, 3, 60};
    // Can be retrieved through seed generation
    private int[] selectedChallenges = new int[6];
    private int[] challengeValues = new int[6];
    private int[] rewardAmounts = new int[6];
    // Save these
    // Last time user generated challenges, in seconds since last point (birthday)
    private int lastChallengeDate;
    private int[] challengeProgress = new int[6];
    private bool[] challengeCollection = new bool[6];
    private int superChallengeTimer;
    private List<string> oreNeeded = new();

    // Listen for language changes
    private void OnEnable()
    {
        // Subscribe to the locale change event
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void Initialize() {
        analyticsDelegator = AnalyticsDelegator.Instance;
        dailyTimerText = dailyTimer.GetComponent<TextMeshProUGUI>();
        mineRenderer = mineRenderGO.GetComponent<MineRenderer>();

        for (int i = 0; i != challengeDescriptionTexts.Length; i++) {
            challengeTextMeshes[i] = challengeDescriptionTexts[i].GetComponent<TextMeshProUGUI>();
            rewardTextMeshes[i] = rewardTexts[i].GetComponent<TextMeshProUGUI>();
            challengeProgressSliders[i] = challengeProgressSlidersGO[i].GetComponent<Slider>();
            challengeProgressSlidersText[i] = challengeProgressSlidersTextGO[i].GetComponent<TextMeshProUGUI>();
        }

        superChallengeSlider = superChallengeSliderGO.GetComponent<Slider>();
        superChallengeTimerText = superChallengeTimerTextGO.GetComponent<TextMeshProUGUI>();

        oreNames = new string[mineRenderer.tierThresholds.Length][];

        /*if (lastChallengeDate == TimeSinceBirthday()) {
            LoadChallenges();
        } else {
            GenerateChallenges();
        }*/

        SetDailyTimer();
        GenerateChallenges();
    }

    void Update() {
        timeRemaining = endTime - DateTime.UtcNow;
        timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
        timerMessage = GetLocalizedValue("RESETS IN {0}", timeString);
        dailyTimerText.text = timerMessage;

        if (timeRemaining.TotalSeconds <= 0) {
            SetDailyTimer();
        }

        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public void SetDailyTimer() {
        DateTime now = DateTime.UtcNow;
        DateTime targetTime = new(now.Year, now.Month, now.Day, 21, 20, 0, DateTimeKind.Utc);

        // If the current time is already past 8:50 PM, set it for tomorrow
        if (now > targetTime) {
            targetTime = targetTime.AddDays(1);
        }

        endTime = targetTime;
    }

    public void GenerateChallenges() {

        lastChallengeDate = TimeSinceBirthday();

        // Don't set the last one
        for (int i = 0; i != selectedChallenges.Length - 1; i++) {
            // Put seed inside brackets
            rng = new System.Random();
            selectedChallenges[i] = rng.Next(1, challengeTypes.Length);

            challengeValues[i] = difficulty[i] * baseGoalAmount[selectedChallenges[i]];
            rewardAmounts[i] = difficulty[i] * baseGemReward;

            // This option needs 2 variables
            if (selectedChallenges[i] == 2) {
                oreNeeded.Add(mineRenderer.GetTier1OreNames()[0]);
            }

            string oreValue = oreNeeded.Count > 0 ? oreNeeded[^1].ToString() : "";

            //Debug.Log(GetLocalizedValue(challengeTypes[selectedChallenges[i]], challengeValues[i], oreValue) + " FOR " + rewardAmounts[i] + " GEMS");
        }

        rewardAmounts[0] *= 2;
        // COMPLETE ALL DAILY CHALLENGES
        selectedChallenges[5] = 0;
        rewardAmounts[5] = 10;
        challengeProgress[5] = 0;
        challengeValues[5] = 5;

        UpdateDisplay();
    }

    public void LoadChallenges() {
        Debug.Log("Loading");
    }

    public void UpdateDisplay() {
        int oreNeededCounter = 0;
        string oreName = "";

        for (int i = 0; i != challengeDescriptionTexts.Length; i++) {
            if (selectedChallenges[i] == 2) {
                oreName = oreNeeded[oreNeededCounter];
                oreNeededCounter++;
            }

            challengeTextMeshes[i].text = GetLocalizedValue(challengeTypes[selectedChallenges[i]], challengeValues[i], oreName);
            rewardTextMeshes[i].text = rewardAmounts[i].ToString();
            challengeProgressSliders[i].maxValue = challengeValues[i];
            challengeProgressSliders[i].value = challengeProgress[i];
            challengeProgressSlidersText[i].text = challengeProgress[i].ToString();
        }

        superChallengeSlider.maxValue = challengeValues[0];
        superChallengeSlider.value = challengeProgress[0];
    }

    private int TimeSinceBirthday() {
        return (int) (DateTime.UtcNow.Date - new DateTime(2024, 12, 8, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public void LoadData(GameData data)
    {
        this.lastChallengeDate = data.lastChallengeDate;
        Initialize();

        try {
             StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
        } catch {
        }
    }

    public void SaveData(ref GameData data)
    {
        data.lastChallengeDate = this.lastChallengeDate;
    }

    // Method that is called when the locale changes
    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateDisplay();
    }
}