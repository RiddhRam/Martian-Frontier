using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Localization.Tables;

public class DailyChallengeDelegator : MonoBehaviour, IDataPersistence
{
    private static DailyChallengeDelegator _instance;
    public static DailyChallengeDelegator Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<DailyChallengeDelegator>();
            }
            return _instance;
        }
    }

    public GameObject dailyTimer;
    public GameObject challengePanel;
    public GameObject[] challengeButtons;
    public GameObject superChallengeStartButtonGO;
    public GameObject superChallengeStartButtonTextGO;
    public GameObject superChallengeSliderGO;
    public GameObject superChallengeTimerTextGO;

    private System.Random rng;
    private TextMeshProUGUI dailyTimerText;
    public MineRenderer mineRenderer;
    public PlayerState playerState;

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
    private string[] challengeTypes = {"COLLECT ALL DAILY CHALLENGES", "MINE {0} ORES", "MINE {0} ORES OF {1}"};
    // This one is not related to challenge types, just order of challenges display
    private int[] difficulty = {8, 2, 3, 6, 4, 3};
    private int baseGemReward = 180;
    // Related to the above challenge types
    // This will be multiplied to determine the goal the player needs to reach, 
    // then multiplied by the difficulty to determine the reward
    private int[] baseGoalAmount = {5, 250, 90};
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
    public int superChallengeTimer = 1200;

    private int initializeCount = 0;
    // initializeCount must be at least this number in order for daily challenges to initialize
    private const int minimumInitializeCount = 2;

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

    public void Initialize() {
        
        // Initialize is called twice, once from MineRenderer, one from here. Only initialize once both calls are made
        initializeCount++;
        if (initializeCount < minimumInitializeCount)
        {
            return;
        }
        
        // Load user's progress for today
        if (lastChallengeDate == TimeSinceBirthday())
        {
            GenerateChallenges(true);
        }
        // Its a new day, load new challenges
        else
        {
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
                int[] possibleValues = { 1, 2 };
                selectedChallenges[i] = possibleValues[rng.Next(0, possibleValues.Length)];
            }

            // Determine goal
            challengeValues[i] = difficulty[i] * baseGoalAmount[selectedChallenges[i]];

            // Set value to 0 if new and show as uncollected
            if (!loading)
            {
                challengeProgress[i] = 0;
                challengeCollection[i] = false;

                challengeStatusIcons[i].color = new(255 / 255, 0, 0);
                challengeStatusIcons[i].transform.parent.parent.GetComponent<Button>().interactable = true;
            }

            // Determine reward
            rewardAmounts[i] = difficulty[i] * baseGemReward;

            // This option needs 2 variables
            if (selectedChallenges[i] == 2 && i != 5) {
                AddOreBasedOnTier(i);
            }
        }

        // Super challenge has increased rewards and difficulty (higher goal and limited time)
        challengeValues[0] = (int) (1.5f * challengeValues[0]);
        rewardAmounts[0] *= 2;
        // COMPLETE ALL DAILY CHALLENGES
        selectedChallenges[5] = 0;
        challengeValues[5] = 5;

        UpdateDisplay();
    }

    public void UpdateDisplay() {
        // Wait for everything to be initialized
        if (challengeTextMeshes[0] == null || initializeCount < minimumInitializeCount)
        {
            return;
        }

        int oreNeededCounter = 0;
        string oreName = "";

        bool uncollectedReward = false;

        for (int i = 0; i != selectedChallenges.Length; i++) {
            if (selectedChallenges[i] == 2) {
                // For using random ores (set in AddOreBasedOnTier)
                //oreName = oreNeeded[oreNeededCounter];
                // For using the required ore to move to next level
                oreName = RefineryUpgradePad.Instance.GetRequiredOreName();
                oreNeededCounter++;
            }

            challengeProgress[i] = Math.Clamp(challengeProgress[i], 0, challengeValues[i]);

            if (challengePanel.activeSelf)
            {
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
                if (i == 0 && (superChallengeTimer == superChallengeStartTimer || superChallengeTimer == 0)) {
                    continue;
                }
                if (selectedChallenges[i] != 1 && selectedChallenges[i] != 2) {
                    continue;
                }
                if (selectedChallenges[i] == 1) {
                    challengeProgress[i] += quantities[key];
                    continue;
                }

                // For using random ores (set in AddOreBasedOnTier)
                /*if (selectedChallenges[i] == 2 && oreNeeded[oreNeededCounter] == key)
                {
                    challengeProgress[i] += quantities[key];
                    oreNeededCounter++;
                }*/
                // For using the required ore to move to next level

                if (selectedChallenges[i] == 2 && RefineryUpgradePad.Instance.GetRequiredOreName() == key)
                {
                    challengeProgress[i] += quantities[key];
                    oreNeededCounter++;
                }
                else if (selectedChallenges[i] == 2)
                {
                    oreNeededCounter++;
                }
            }
        }

        UpdateDisplay();
    }

    public void CollectReward(int challengeIndex) {
        if (challengeProgress[challengeIndex] != challengeValues[challengeIndex]) {
            return;
        }

        playerState.AddGems((long) rewardAmounts[challengeIndex]);
        DisableChallengeButton(challengeIndex);
        challengeProgress[5]++;
        AnalyticsDelegator.Instance.CollectChallengeReward(selectedChallenges[challengeIndex]);

        UpdateDisplay();
    }

    private void DisableChallengeButton(int challengeIndex)
    {
        challengeStatusIcons[challengeIndex].transform.parent.parent.GetComponent<Button>().interactable = false;
        challengeCollection[challengeIndex] = true;
    }

    private int TimeSinceBirthday() {
        return (int)(DateTime.UtcNow.Date - new DateTime(2024, 12, 8, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public void AddOreBasedOnTier(int challengeIndex) {
        string[] oreList;

        /*if (playerState.GetRecommendedDrillTier() == 1)
        {
            oreList = mineRenderer.GetTier1OreNames();
        }
        else if (playerState.GetRecommendedDrillTier() == 2)
        {
            oreList = mineRenderer.GetTier2OreNames();
        }
        else
        {
            oreList = mineRenderer.GetTier3OreNames();
        }*/

        // Always use tier 2 for now
        oreList = mineRenderer.GetTier2OreNames();

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
    }

    public void StartSuperChallenge() {
        StartCoroutine(CountdownSuperChallengeTimer(superChallengeStartTimer));
        AnalyticsDelegator.Instance.StartSuperChallenge(selectedChallenges[0]);
    }

    private IEnumerator CountdownSuperChallengeTimer(int startTime) {
        superChallengeTimer = startTime;
        int minutes;
        int seconds;

        superChallengeStartButtonGO.GetComponent<Button>().interactable = false;
        superChallengeSliderGO.SetActive(true);
        superChallengeTimerTextGO.SetActive(true);

        // Wait for initialization
        while (initializeCount < minimumInitializeCount)
        {
            yield return null;
        }

        while (superChallengeTimer > 0 && challengeProgress[0] < challengeValues[0])
        {
            superChallengeTimer--;
            // Calculate minutes and seconds
            minutes = superChallengeTimer / 60;
            seconds = superChallengeTimer % 60;
            superChallengeTimerText.text = $"{minutes}:{seconds:D2}";
            superChallengeStartButtonText.text = $"{minutes}:{seconds:D2}";
            yield return new WaitForSeconds(1f);
        }

        superChallengeSliderGO.SetActive(false);
        superChallengeTimerTextGO.SetActive(false);
        
        if (challengeProgress[0] < challengeValues[0]) {
            challengeProgress[0] = 0;
            superChallengeStartButtonGO.GetComponent<Button>().interactable = true;
        } else {
            // If successfully completed then log how long it took
            AnalyticsDelegator.Instance.CompleteSuperChallenge(selectedChallenges[0], superChallengeTimer);
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
        // Initialization happens in mine renderer, this just loads the data
        this.lastChallengeDate = data.lastChallengeDate;
        this.challengeProgress = data.challengeProgress;
        this.challengeCollection = data.challengeCollection;
        this.superChallengeTimer = data.superChallengeTimer;

        // Don't let players collect a reward if already collected
        for (int i = 0; i != challengeCollection.Length; i++)
        {
            if (challengeCollection[i])
            {
                DisableChallengeButton(i);

                if (i == 0)
                {
                    superChallengeStartButtonGO.SetActive(false);
                }
            }
        }

        if (superChallengeTimer < superChallengeStartTimer)
        {
            StartCoroutine(CountdownSuperChallengeTimer(superChallengeTimer));
        }

        Initialize();
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