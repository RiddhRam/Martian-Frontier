using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class MiniGameChooser : MonoBehaviour, IDataPersistence
{
    [SerializeField] private string[] minigameNames;
    [SerializeField] private string[] minigameDescriptions;

    [SerializeField] private TextMeshProUGUI resetTimerText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private PlayerState playerState;
    [SerializeField] private SessionDelegator sessionDelegator;

    // Two day intervals minigames
    [SerializeField] private int twoDIM;
    private int previousInterval;
    private readonly DateTime resetDate = new DateTime(1970, 1, 2, 12, 0, 0, DateTimeKind.Utc);

    DateTime endTime;
    TimeSpan timeRemaining;
    string timeString;

    public void LoadData(GameData data)
    {
        this.twoDIM = CalculateTwoDayIntervals();

        previousInterval = data.twoDIM;

        SetMiniGameTimer();
        StartCoroutine(CountdownToNextInterval());
    }

    private IEnumerator CountdownToNextInterval() {

        // Let player state load
        yield return new WaitUntil(() => playerState.loaded);

        // Reset data if there's a new mini game today
        if (this.twoDIM > this.previousInterval) {
            ResetMiniGameData();
        }

        while (true) {
            // Set minigame
            int index = this.twoDIM % minigameNames.Length;
            nameText.text = GetLocalizedValue(minigameNames[index]);
            descriptionText.text = GetLocalizedValue(minigameDescriptions[index]);
            sessionDelegator.minigameName = minigameNames[index];

            timeRemaining = endTime - DateTime.UtcNow;
            timeString = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            resetTimerText.text = GetLocalizedValue("RESETS IN {0}", timeString);

            if (timeRemaining.TotalSeconds <= 0) {
                previousInterval = this.twoDIM;
                this.twoDIM = CalculateTwoDayIntervals();
                
                SetMiniGameTimer();

                ResetMiniGameData();
            }

            yield return new WaitForSecondsRealtime(1);
        }
    }

    private void ResetMiniGameData() {

        playerState.ResetCredits();
        
        ref GameData gameData = ref DataPersistenceManager.Instance.GetGameDataRef();

        gameData.magnetHaulerUpgrades.Clear();
        gameData.magnetHaulerChallengeProgress = new int[6];
        gameData.magnetHaulerChallengeCollection = new bool[6];
        gameData.magnetHaulerSuperChallengeTimer = 1200;

        gameData.oreBlasterUpgrades.Clear();
        gameData.oreBlasterChallengeProgress = new int[6];
        gameData.oreBlasterChallengeCollection = new bool[6];
        gameData.oreBlasterSuperChallengeTimer = 1200;
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

    public int CalculateTwoDayIntervals() {
        // 24 hours after leaderboard resets
        TimeSpan timeSinceEpoch = DateTime.UtcNow - resetDate;
        return (int)(timeSinceEpoch.TotalDays / 2);
    }

    public void SetMiniGameTimer() {
        DateTime epoch = resetDate;
        DateTime now = DateTime.UtcNow;

        // Calculate how many full 2-day cycles have passed 
        long daysSinceEpoch = (long)(now - epoch).TotalDays;
        long cyclesSinceEpoch = daysSinceEpoch / 2;

        // Find the next reset time by adding cycles * 2 days back
        DateTime nextResetTime = epoch.AddDays((cyclesSinceEpoch + 1) * 2);
        endTime = nextResetTime;
    }

    public void SaveData(ref GameData data)
    {
        data.twoDIM = this.twoDIM;
    }
}