using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Localization.Tables;

public class OreBlasterDailyChallengeDelegator : MonoBehaviour, IDataPersistence
{

    public GameObject challengePanel;
    public GameObject[] challengeStatusIconsGO;
    public GameObject[] challengeDescriptionTexts;
    public GameObject[] rewardTexts;
    public GameObject[] challengeProgressSlidersGO;
    public GameObject[] challengeProgressSlidersTextGO;
    public GameObject superChallengeStartButtonGO;
    public GameObject superChallengeStartButtonTextGO;
    public GameObject superChallengeSliderGO;
    public GameObject superChallengeTimerTextGO;

    private System.Random rng;
    private AnalyticsDelegator analyticsDelegator;
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

    // Used to check index
    private string[] challengeTypes = {"COLLECT ALL DAILY CHALLENGES", "COLLECT {0} ORES", "UPGRADE EVERYTHING TO LEVEL {0}"};
    // This one is not related to challenge types, just order of challenges display
    private int[] difficulty = {8, 1, 2, 5, 3, 3};
    private int baseGemReward = 180;
    // Related to the above challenge types
    // This will be multiplied to determine the goal the player needs to reach, 
    // then multiplied by the difficulty to determine the reward
    private int[] baseGoalAmount = {5, 150, 1};
    // Can be retrieved through seed generation
    private int[] selectedChallenges = new int[6];
    private int[] challengeValues = new int[6];
    private int[] rewardAmounts = new int[6];
    private readonly List<string> oreNeeded = new();
    private int twoDayIntervals;
    // Save these
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

        for (int i = 0; i != challengeDescriptionTexts.Length; i++) {
            challengeStatusIcons[i] = challengeStatusIconsGO[i].GetComponent<Image>();
            challengeTextMeshes[i] = challengeDescriptionTexts[i].GetComponent<TextMeshProUGUI>();
            rewardTextMeshes[i] = rewardTexts[i].GetComponent<TextMeshProUGUI>();
            challengeProgressSliders[i] = challengeProgressSlidersGO[i].GetComponent<Slider>();
            challengeProgressSlidersText[i] = challengeProgressSlidersTextGO[i].GetComponent<TextMeshProUGUI>();
        }

        superChallengeSlider = superChallengeSliderGO.GetComponent<Slider>();
        superChallengeTimerText = superChallengeTimerTextGO.GetComponent<TextMeshProUGUI>();
        superChallengeStartButtonText = superChallengeStartButtonTextGO.GetComponent<TextMeshProUGUI>();
    }

    void Initialize() {
        analyticsDelegator = AnalyticsDelegator.Instance;

        GenerateChallenges(true);
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

    public void GenerateChallenges(bool loading) {
        Debug.Log("Generating");

        // Don't set the last one
        for (int i = 0; i != selectedChallenges.Length; i++) {
            // Put seed inside brackets
            rng = new System.Random(twoDayIntervals + i);
            selectedChallenges[i] = rng.Next(1, challengeTypes.Length);
            
            if (i == 0) {
                int[] possibleValues = { 1, 2, 5 };
                selectedChallenges[i] = possibleValues[rng.Next(0, possibleValues.Length)];
            }

            challengeValues[i] = difficulty[i] * baseGoalAmount[selectedChallenges[i]];

            if (!loading) {
                challengeProgress[i] = 0;
                challengeCollection[i] = false;
            }

            rewardAmounts[i] = difficulty[i] * baseGemReward;

            challengeStatusIcons[i].color = new(255/255, 0, 0);
            challengeStatusIcons[i].transform.parent.parent.GetComponent<Button>().interactable = true;
        }

        rewardAmounts[0] *= 2;
        // COMPLETE ALL DAILY CHALLENGES
        selectedChallenges[5] = 0;
        challengeValues[5] = 5;

        UpdateDisplay();
    }

    public void UpdateDisplay() {
        if (challengeTextMeshes[0] == null) {
            Debug.Log("Returning");
            return;
        }

        bool uncollectedReward = false;

        for (int i = 0; i != selectedChallenges.Length; i++) {

            challengeProgress[i] = Math.Clamp(challengeProgress[i], 0, challengeValues[i]);

            if (challengePanel.activeSelf) {
                challengeTextMeshes[i].text = GetLocalizedValue(challengeTypes[selectedChallenges[i]], challengeValues[i]);
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

    public void CollectedOres(Dictionary<string, int> quantities) {

        // Find all Collect {0} ores challenges and add the value of each key to it
        foreach (string key in quantities.Keys) {

            for (int i = 0; i != selectedChallenges.Length; i++) {
                if (i == 0 && superChallengeTimer == superChallengeStartTimer) {
                    continue;
                }
                if (selectedChallenges[i] != 1) {
                    continue;
                }

                // Collect {0} ores is at index 1
                if (selectedChallenges[i] == 1) {
                    challengeProgress[i] += quantities[key];
                    continue;
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

    public void LoadData(GameData data)
    {
        this.twoDayIntervals = data.twoDayIntervals;
        this.challengeProgress = data.oreBlasterChallengeProgress;
        this.challengeCollection = data.oreBlasterChallengeCollection;
        this.superChallengeTimer = data.oreBlasterSuperChallengeTimer;

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
        data.oreBlasterChallengeProgress = this.challengeProgress;
        data.oreBlasterChallengeCollection = this.challengeCollection;
        data.oreBlasterSuperChallengeTimer = this.superChallengeTimer;
    }

    // Method that is called when the locale changes
    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateDisplay();
    }
}