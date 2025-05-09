using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class LeaderboardDelegator : MonoBehaviour, IDataPersistence
{
    public PlayerState playerState;

    public GameObject oreTournamentPanel;
    public GameObject oreTournamentButton;

    public GameObject collectReward;
    public TextMeshProUGUI collectRewardMessage;
    public TextMeshProUGUI collectRewardText;

    public Sprite[] tierSprites;
    public TextMeshProUGUI oreTierText;

    public TextMeshProUGUI tournamentTimer;
    public TextMeshProUGUI oreNextTierText;
    public TextMeshProUGUI oreLastTierText;


    public Image oreTierImage;
    public TextMeshProUGUI lastUpdateText;

    public TextMeshProUGUI[] orePlayerNameTextMeshes;
    public TextMeshProUGUI[] oreScoreTextMeshes;
    public TextMeshProUGUI[] oreRewardTextMeshes;
    public Image[] orePlayerScoreImages;
    public GameObject[] orePlayerScoreBars;

    private DateTime endTime;
    private TimeSpan timeRemaining;
    private string timeString;
    private PlayerProfile playerProfile;
    private int lastUpdateTimer = 0;
    public long gemRewardsToCollect = 0;

    private readonly string oreLeaderboardID = "Ores";
    private readonly string[] leaderboardTiers = {"BRONZE TIER", "SILVER TIER", "GOLD TIER"};
    private readonly string[] leaderboardTiersMatching = {"Bronze", "Silver", "Gold"};
    private readonly string[][] rewardAmounts = new string[][] {
            new string[] {"2K", "1.6K", "1.4K", "1.2K", "1K", "800", "800", "800", "600", "600"}, 
            new string[] {"12K", "8K", "6.4K", "5K", "4K", "3.2K", "3.2K", "3.2K", "2.8K", "2.8K"}, 
            new string[] {"64K", "50K", "40K", "32k", "24k", "20K", "20K", "20K", "16K", "16K"}
            };
    LeaderboardScores oreLeaderboardScoresPage;

    private void OnEnable()
    {
        SetLeaderBoardTimer();
        StartCoroutine(TimerController());
    }
    
    private IEnumerator TimerController() {
        while (true) {
            timeRemaining = endTime - DateTime.UtcNow;
            timeString = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            tournamentTimer.text = GetLocalizedValue("RESETS IN {0}", timeString);
            lastUpdateText.text = lastUpdateTimer + "s";
            lastUpdateTimer++;

            if (timeRemaining.TotalSeconds <= 0) {
                SetLeaderBoardTimer();
                yield return new WaitForSeconds(60);
                CheckForRewards();
            }
 
            if (timeRemaining.Seconds == 0 || timeRemaining.Seconds == 30) {
                UpdateLeaderBoardData();
            }

            yield return new WaitForSecondsRealtime(1);
        }
    }

    public void SetLeaderBoardTimer() {
        DateTime epoch = new DateTime(1970, 1, 1, 12, 0, 0, DateTimeKind.Utc); // Start at 12:00 PM UTC on Epoch
        DateTime now = DateTime.UtcNow;

        // Calculate how many full 2-day cycles have passed since the epoch
        long daysSinceEpoch = (long)(now - epoch).TotalDays;
        long cyclesSinceEpoch = daysSinceEpoch / 2;

        // Find the next reset time by adding cycles * 2 days back to the epoch
        DateTime nextResetTime = epoch.AddDays((cyclesSinceEpoch + 1) * 2);

        endTime = nextResetTime;
    }

    public async void CheckForRewards(string message = null) {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            if (gemRewardsToCollect > 0) {
                collectRewardText.text = gemRewardsToCollect.ToString();
                collectReward.SetActive(true);

                if (message != null) {
                    collectRewardMessage.text = GetLocalizedValue(message);
                }
                
            }
            return;
        }

        try
        {
            // Load the file from the cloud
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"leaderboard_gems"});

            if (data.TryGetValue("leaderboard_gems", out var keyName)) {
                gemRewardsToCollect += keyName.Value.GetAs<long>();

                if (keyName.Value.GetAs<long>() > 0) {
                    var newData = new Dictionary<string, object>{{"leaderboard_gems", 0}};
                    await CloudSaveService.Instance.Data.Player.SaveAsync(newData);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Reward check failed: {e.Message}");
        }

        if (gemRewardsToCollect > 0) {
            collectRewardText.text = FormatPrice(gemRewardsToCollect);
            collectReward.SetActive(true);
            
            if (message != null) {
                collectRewardMessage.text = GetLocalizedValue(message);
            }
        }
    }

    public void CollectLeaderboardRewards() {
        long gemValue = gemRewardsToCollect;
        gemRewardsToCollect = 0;
        playerState.AddGems(gemValue);
        collectReward.SetActive(false);
        collectReward.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("CONGRATULATIONS! YOU RECEIVED SOME REWARDS!");
        
    }

    public async void UpdateLeaderBoardData() {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            return;
        }

        try {
            oreLeaderboardScoresPage = await LeaderboardsService.Instance.GetPlayerRangeAsync(
                                                oreLeaderboardID,
                                                new GetPlayerRangeOptions{ RangeLimit = 11 }
                                            );

            int playerTier = 0;
            int results = oreLeaderboardScoresPage.Results.Count;

            for (int i = 0; i != 10; i++) {
                orePlayerScoreBars[i].SetActive(false);
            }

            for (int i = 0; i != results; i++) {
                if (oreLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    switch (oreLeaderboardScoresPage.Results[i].Tier) {
                        case "Bronze":
                            oreTierImage.sprite = tierSprites[0];
                            oreTierText.text = GetLocalizedValue(leaderboardTiers[0]);
                            break;
                        case "Silver":
                            playerTier = 1;
                            oreTierImage.sprite = tierSprites[1];
                            oreTierText.text = GetLocalizedValue(leaderboardTiers[1]);
                            break;
                        case "Gold":
                            playerTier = 2;
                            oreTierImage.sprite = tierSprites[2];
                            oreTierText.text = GetLocalizedValue(leaderboardTiers[2]);
                            break;
                    }
                    break;
                }
            }
            
            int firstPlayerIndex = 0;
            int lastPlayerIndex = 0;
            int playerBarCounter = 0;
            for (int i = 0; i != results; i++) {

                if (oreLeaderboardScoresPage.Results[i].Tier != leaderboardTiersMatching[playerTier]) {
                    if (oreLeaderboardScoresPage.Results[i].Tier == "Gold") {
                        firstPlayerIndex = i;
                    } else if (oreLeaderboardScoresPage.Results[i].Tier == "Bronze") {

                        if (lastPlayerIndex == 0) {
                            lastPlayerIndex = i;
                        } 
                    } else if (oreLeaderboardScoresPage.Results[i].Tier == "Silver") {
                        if (leaderboardTiersMatching[playerTier] == "Gold") {
                            if (lastPlayerIndex == 0) {
                                lastPlayerIndex = i;
                            } 
                        } else {
                            firstPlayerIndex = i;
                        }
                    }
                    continue;
                }

                orePlayerScoreBars[playerBarCounter].SetActive(true);

                orePlayerNameTextMeshes[playerBarCounter].text = oreLeaderboardScoresPage.Results[i].PlayerName.Substring(0, oreLeaderboardScoresPage.Results[i].PlayerName.Length - 5);;
                oreScoreTextMeshes[playerBarCounter].text = FormatPrice(oreLeaderboardScoresPage.Results[i].Score);

                if (oreLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    orePlayerScoreImages[playerBarCounter].color = new(255/255f, 204/255f, 0/255f);
                } else {
                    orePlayerScoreImages[playerBarCounter].color = new(1, 1, 1);
                }

                oreRewardTextMeshes[playerBarCounter].text = rewardAmounts[playerTier][playerBarCounter];

                playerBarCounter++;
            }

            if (lastPlayerIndex != 0) {
                oreLastTierText.text = GetLocalizedValue("LAST TIER: {0}", FormatPrice(oreLeaderboardScoresPage.Results[lastPlayerIndex].Score));
                oreLastTierText.gameObject.SetActive(true);
            } else {
                oreLastTierText.gameObject.SetActive(false);
            }
            if (firstPlayerIndex != 0) {
                oreNextTierText.text = GetLocalizedValue("NEXT TIER: {0}", FormatPrice(oreLeaderboardScoresPage.Results[firstPlayerIndex].Score));
                oreNextTierText.gameObject.SetActive(true);
            } else {
                oreNextTierText.gameObject.SetActive(false);
            }

            firstPlayerIndex = 0;
            lastPlayerIndex = 0;
            playerBarCounter = 0;

            lastUpdateTimer = 0;
        } catch (Exception ex) {
            // In case no score submitted
            try {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(oreLeaderboardID, 0);
            } catch {
            }

            Debug.Log(ex);
        }
    }

    public async Task InitializeLeaderboard(PlayerProfile newPlayerProfile) {
        playerProfile = newPlayerProfile;

        await LeaderboardsService.Instance.AddPlayerScoreAsync(oreLeaderboardID, 0);

        UpdateLeaderBoardData();
    }

    public void AddOreScore(double amount) {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            return;
        }

        LeaderboardsService.Instance.AddPlayerScoreAsync(oreLeaderboardID, amount);
    }

    // TODO: If an anonymous player creates an account, their old account stays in the leaderboard and their new account
    // Picks off from where they left off. The old anonymous account should be removed, so someone else can earn the reward
    // For that spot
    public void RemoveFromLeaderBoard() {
        
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

    private string FormatPrice(double price)
    {
        if (price >= 1_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("0.##") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public void LoadData(GameData data)
    {
        this.gemRewardsToCollect = data.gemRewardsToCollect;
    }

    public void SaveData(ref GameData data)
    {
        data.gemRewardsToCollect = this.gemRewardsToCollect;
    }
}
