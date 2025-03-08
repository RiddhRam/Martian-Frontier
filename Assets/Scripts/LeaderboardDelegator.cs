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
using UnityEngine.UI;

public class LeaderboardDelegator : MonoBehaviour, IDataPersistence
{
    public PlayerState playerState;


    public GameObject cashTournamentPanel;
    public GameObject vehicleTournamentPanel;
    public GameObject cashTournamentButton;
    public GameObject vehicleTournamentButton;

    public GameObject collectReward;
    public TextMeshProUGUI collectRewardMessage;
    public TextMeshProUGUI collectRewardText;

    public Sprite[] tierSprites;
    public TextMeshProUGUI cashTierText;
    public TextMeshProUGUI vehiclesTierText;
    public TextMeshProUGUI tournamentTimer;
    public TextMeshProUGUI cashNextTierText;
    public TextMeshProUGUI cashLastTierText;
    public TextMeshProUGUI vehiclesNextTierText;
    public TextMeshProUGUI vehiclesLastTierText;

    public Image cashTierImage;
    public Image vehiclesTierImage;
    public TextMeshProUGUI lastUpdateText;

    public TextMeshProUGUI[] cashPlayerNameTextMeshes;
    public TextMeshProUGUI[] cashScoreTextMeshes;
    public TextMeshProUGUI[] cashRewardTextMeshes;
    public Image[] cashPlayerScoreImages;
    public GameObject[] cashPlayerScoreBars;

    public TextMeshProUGUI[] vehiclesPlayerNameTextMeshes;
    public TextMeshProUGUI[] vehiclesScoreTextMeshes;
    public TextMeshProUGUI[] vehiclesRewardTextMeshes;
    public Image[] vehiclesPlayerScoreImages;
    public GameObject[] vehiclesPlayerScoreBars;

    private DateTime endTime;
    private TimeSpan timeRemaining;
    private string timeString;
    private PlayerProfile playerProfile;
    private int lastUpdateTimer = 0;
    public long gemRewardsToCollect = 0;

    private readonly string cashLeaderboardID = "Cash";
    private readonly string vehiclesLeaderboardID = "Vehicles";
    private readonly string[] leaderboardTiers = {"BRONZE TIER", "SILVER TIER", "GOLD TIER"};
    private readonly string[] leaderboardTiersMatching = {"Bronze", "Silver", "Gold"};
    private readonly string[][] rewardAmounts = new string[][] {
            new string[] {"2K", "1.6K", "1.4K", "1.2K", "1K", "800", "800", "800", "600", "600"}, 
            new string[] {"12K", "8K", "6.4K", "5K", "4K", "3.2K", "3.2K", "3.2K", "2.8K", "2.8K"}, 
            new string[] {"64K", "50K", "40K", "32k", "24k", "20K", "20K", "20K", "16K", "16K"}
            };
    LeaderboardScores cashLeaderboardScoresPage;
    LeaderboardScores vehicleLeaderboardScoresPage;

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
            cashLeaderboardScoresPage = await LeaderboardsService.Instance.GetPlayerRangeAsync(
                                                cashLeaderboardID,
                                                new GetPlayerRangeOptions{ RangeLimit = 11 }
                                            );

            int playerTier = 0;
            int results = cashLeaderboardScoresPage.Results.Count;

            for (int i = 0; i != 10; i++) {
                cashPlayerScoreBars[i].SetActive(false);
            }

            for (int i = 0; i != results; i++) {
                if (cashLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    switch (cashLeaderboardScoresPage.Results[i].Tier) {
                        case "Bronze":
                            cashTierImage.sprite = tierSprites[0];
                            cashTierText.text = GetLocalizedValue(leaderboardTiers[0]);
                            break;
                        case "Silver":
                            playerTier = 1;
                            cashTierImage.sprite = tierSprites[1];
                            cashTierText.text = GetLocalizedValue(leaderboardTiers[1]);
                            break;
                        case "Gold":
                            playerTier = 2;
                            cashTierImage.sprite = tierSprites[2];
                            cashTierText.text = GetLocalizedValue(leaderboardTiers[2]);
                            break;
                    }
                    break;
                }
            }
            
            int firstPlayerIndex = 0;
            int lastPlayerIndex = 0;
            int playerBarCounter = 0;
            for (int i = 0; i != results; i++) {

                if (cashLeaderboardScoresPage.Results[i].Tier != leaderboardTiersMatching[playerTier]) {
                    if (cashLeaderboardScoresPage.Results[i].Tier == "Gold") {
                        firstPlayerIndex = i;
                    } else if (cashLeaderboardScoresPage.Results[i].Tier == "Bronze") {

                        if (lastPlayerIndex == 0) {
                            lastPlayerIndex = i;
                        } 
                    } else if (cashLeaderboardScoresPage.Results[i].Tier == "Silver") {
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

                cashPlayerScoreBars[playerBarCounter].SetActive(true);

                cashPlayerNameTextMeshes[playerBarCounter].text = cashLeaderboardScoresPage.Results[i].PlayerName.Substring(0, cashLeaderboardScoresPage.Results[i].PlayerName.Length - 5);;
                cashScoreTextMeshes[playerBarCounter].text = $"${FormatPrice(cashLeaderboardScoresPage.Results[i].Score)}";

                if (cashLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    cashPlayerScoreImages[playerBarCounter].color = new(255/255f, 204/255f, 0/255f);
                } else {
                    cashPlayerScoreImages[playerBarCounter].color = new(1, 1, 1);
                }

                cashRewardTextMeshes[playerBarCounter].text = rewardAmounts[playerTier][playerBarCounter];

                playerBarCounter++;
            }

            if (lastPlayerIndex != 0) {
                cashLastTierText.text = GetLocalizedValue("LAST TIER: {0}", $"${FormatPrice(cashLeaderboardScoresPage.Results[lastPlayerIndex].Score)}");
                cashLastTierText.gameObject.SetActive(true);
            } else {
                cashLastTierText.gameObject.SetActive(false);
            }
            if (firstPlayerIndex != 0) {
                cashNextTierText.text = GetLocalizedValue("NEXT TIER: {0}", $"${FormatPrice(cashLeaderboardScoresPage.Results[firstPlayerIndex].Score)}");
                cashNextTierText.gameObject.SetActive(true);
            } else {
                cashNextTierText.gameObject.SetActive(false);
            }

            vehicleLeaderboardScoresPage = await LeaderboardsService.Instance.GetPlayerRangeAsync(
                                                vehiclesLeaderboardID,
                                                new GetPlayerRangeOptions{ RangeLimit = 10 }
                                            );

            for (int i = 0; i != 10; i++) {
                vehiclesPlayerScoreBars[i].SetActive(false);
            }
                                            
            playerTier = 0;
            results = vehicleLeaderboardScoresPage.Results.Count;

            for (int i = 0; i != results; i++) {
                if (vehicleLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    switch (vehicleLeaderboardScoresPage.Results[i].Tier) {
                        case "Bronze":
                            vehiclesTierImage.sprite = tierSprites[0];
                            vehiclesTierText.text = GetLocalizedValue(leaderboardTiers[0]);
                            break;
                        case "Silver":
                            playerTier = 1;
                            vehiclesTierImage.sprite = tierSprites[1];
                            vehiclesTierText.text = GetLocalizedValue(leaderboardTiers[1]);
                            break;
                        case "Gold":
                            playerTier = 2;
                            vehiclesTierImage.sprite = tierSprites[2];
                            vehiclesTierText.text = GetLocalizedValue(leaderboardTiers[2]);
                            break;
                    }
                    break;
                }
            }

            firstPlayerIndex = 0;
            lastPlayerIndex = 0;
            playerBarCounter = 0;

            for (int i = 0; i != results; i++) {

                if (vehicleLeaderboardScoresPage.Results[i].Tier != leaderboardTiersMatching[playerTier]) {
                    if (vehicleLeaderboardScoresPage.Results[i].Tier == "Gold") {
                        firstPlayerIndex = i;
                    } else if (vehicleLeaderboardScoresPage.Results[i].Tier == "Bronze") {

                        if (lastPlayerIndex == 0) {
                            lastPlayerIndex = i;
                        } 
                    } else if (vehicleLeaderboardScoresPage.Results[i].Tier == "Silver") {
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

                vehiclesPlayerScoreBars[playerBarCounter].SetActive(true);

                vehiclesPlayerNameTextMeshes[playerBarCounter].text = vehicleLeaderboardScoresPage.Results[i].PlayerName.Substring(0, vehicleLeaderboardScoresPage.Results[i].PlayerName.Length - 5);;
                vehiclesScoreTextMeshes[playerBarCounter].text = FormatPrice(vehicleLeaderboardScoresPage.Results[i].Score);

                if (vehicleLeaderboardScoresPage.Results[i].PlayerName == playerProfile.Name) {
                    vehiclesPlayerScoreImages[playerBarCounter].color = new(255/255f, 204/255f, 0/255f);
                } else {
                    vehiclesPlayerScoreImages[playerBarCounter].color = new(1, 1, 1);
                }

                vehiclesRewardTextMeshes[playerBarCounter].text = rewardAmounts[playerTier][playerBarCounter];

                playerBarCounter++;
            }

            if (lastPlayerIndex != 0) {
                vehiclesLastTierText.text = GetLocalizedValue("LAST TIER: {0}", FormatPrice(vehicleLeaderboardScoresPage.Results[lastPlayerIndex].Score));
                vehiclesLastTierText.gameObject.SetActive(true);
            } else {
                vehiclesLastTierText.gameObject.SetActive(false);
            }
            if (firstPlayerIndex != 0) {
                vehiclesNextTierText.text = GetLocalizedValue("NEXT TIER: {0}", FormatPrice(vehicleLeaderboardScoresPage.Results[firstPlayerIndex].Score));
                vehiclesNextTierText.gameObject.SetActive(true);
            } else {
                vehiclesNextTierText.gameObject.SetActive(false);
            }

            lastUpdateTimer = 0;
        } catch (Exception ex) {
            // In case no score submitted
            try {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(cashLeaderboardID, 0);
            } catch {
            }

            try {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(vehiclesLeaderboardID, 0);
            } catch {
            }
            Debug.Log(ex);
        }
    }

    public async Task InitializeLeaderboard(PlayerProfile newPlayerProfile) {
        playerProfile = newPlayerProfile;

        await LeaderboardsService.Instance.AddPlayerScoreAsync(cashLeaderboardID, 0);

        await LeaderboardsService.Instance.AddPlayerScoreAsync(vehiclesLeaderboardID, 0);

        UpdateLeaderBoardData();
    }

    public void AddCashScore(double amount) {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            return;
        }

        LeaderboardsService.Instance.AddPlayerScoreAsync(cashLeaderboardID, amount);
    }

    public void AddVehicleScore(int amount) {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            return;
        }

        LeaderboardsService.Instance.AddPlayerScoreAsync(vehiclesLeaderboardID, amount);
    }

    // TODO: If an anonymous player creates an account, their old account stays in the leaderboard and their new account
    // Picks off from where they left off. The old anonymous account should be removed, so someone else can earn the reward
    // For that spot
    public void RemoveFromLeaderBoard() {
        
    }

    public void TogglePanel(string type) {
        if (type == "Cash") {
            vehicleTournamentPanel.SetActive(false);
            vehicleTournamentButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            vehicleTournamentButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            cashTournamentPanel.SetActive(true);
            cashTournamentButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            cashTournamentButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        } else {
            cashTournamentPanel.SetActive(false);
            cashTournamentButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            cashTournamentButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            vehicleTournamentPanel.SetActive(true);
            vehicleTournamentButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            vehicleTournamentButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
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

    private string FormatPrice(double price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Se";
        }
        else if (price >= 1_000_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
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
