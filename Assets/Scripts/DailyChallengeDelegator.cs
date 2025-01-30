using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using System;

public class DailyChallengeDelegator : MonoBehaviour, IDataPersistence
{
    public GameObject dailyTimer;

    private AnalyticsDelegator analyticsDelegator;
    private TextMeshProUGUI dailyTimerText;
    private string timerMessage;
    private DateTime endTime;
    private TimeSpan timeRemaining;
    private string timeString;
    // Last time user generated challenges
    private int lastChallengeDate;
    // Used to check index
    private string[] challengeTypes = {"MINE {0} ORES", "MINE {0} ORES IN {1}", "BUY {0} DRILLERS", "BUY {0} HAULERS", "EARN ${0}", "SELL {0} ORES"};
    private string[] oreNames;
    // Save these
    private int[] selectedChallenges = new int[6];
    private int[][] challengeValues = new int[6][];
    private int[] challengeProgress = new int[6];
    private bool[] challengeCollection = new bool[6];

    void Initialize() {
        analyticsDelegator = AnalyticsDelegator.Instance;
        dailyTimerText = dailyTimer.GetComponent<TextMeshProUGUI>();

        if (lastChallengeDate == TimeSinceBirthday()) {
            LoadChallenges();
        } else {
            GenerateChallenges();
        }
        SetDailyTimer();
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
        Debug.Log(lastChallengeDate);
        Debug.Log("Generating");
    }

    public void LoadChallenges() {
        Debug.Log("Loading");
    }

    private int TimeSinceBirthday() {
        return (int) (DateTime.UtcNow.Date - new DateTime(2024, 12, 8, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public void LoadData(GameData data)
    {
        this.lastChallengeDate = data.lastChallengeDate;
        Initialize();
    }

    public void SaveData(ref GameData data)
    {
        data.lastChallengeDate = this.lastChallengeDate;
    }
}
