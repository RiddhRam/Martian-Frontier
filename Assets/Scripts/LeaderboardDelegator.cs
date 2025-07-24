using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class LeaderboardDelegator : MonoBehaviour, IDataPersistence
{
    private static LeaderboardDelegator _instance;
    public static LeaderboardDelegator Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindObjectOfType<LeaderboardDelegator>();
            }
            return _instance;
        }
    }

    public PlayerState playerState;

    public GameObject oreTournamentPanel;

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
    private int lastUpdateTimer = 0;
    public long gemRewardsToCollect = 0;

    // Two day intervals leaderboard
    [SerializeField] private int twoDIL;
    private int previousInterval;

    //private const string oreLeaderboardID = "Ores";
    private static readonly string[] leaderboardTiers = { "BRONZE TIER", "SILVER TIER", "GOLD TIER" };
    private static readonly string[] leaderboardTiersMatching = { "Bronze", "Silver", "Gold" };
    private static readonly int[][] rewardAmounts = new int[][] {
            new int[] {2_000, 1_600, 1_400, 1_200, 1_000, 800, 800, 800, 600, 600},
            new int[] {12_000, 8_000, 6_400, 5_000, 4_000, 3_200, 3_200, 3_200, 2_800, 2_800},
            new int[] {64_000, 50_000, 40_000, 32_000, 24_000, 20_000, 20_000, 20_000, 16_000, 16_000}
            };
    
    private static readonly DateTime resetDate = new DateTime(1970, 1, 1, 12, 0, 0, DateTimeKind.Utc); // Start at 12:00 PM UTC on Epoch

    LeaderboardResults oreLeaderboardResultsPage;
    List<LeaderboardPlayer> oreLeaderboardScores;

    private IEnumerator TimerController()
    {
        if (this.twoDIL > this.previousInterval)
        {
            ResetLeaderboard();
        }

        while (true)
        {
            timeRemaining = endTime - DateTime.UtcNow;
            timeString = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            tournamentTimer.text = GetLocalizedValue("RESETS IN {0}", timeString);
            lastUpdateText.text = lastUpdateTimer + "s";
            lastUpdateTimer++;

            if (timeRemaining.TotalSeconds <= 0)
            {
                SetLeaderBoardTimer();

                yield return new WaitForSeconds(5);

                previousInterval = this.twoDIL;
                this.twoDIL = CalculateTwoDayIntervals();

                ResetLeaderboard();
            }

            if (timeRemaining.Seconds == 0 || timeRemaining.Seconds == 30)
            {
                UpdateLeaderBoardData();
            }

            yield return new WaitForSecondsRealtime(1);
        }
    }

    public void SetLeaderBoardTimer() {
        DateTime epoch = resetDate;
        DateTime now = DateTime.UtcNow;

        // Calculate how many full 2-day cycles have passed since the epoch
        long daysSinceEpoch = (long)(now - epoch).TotalDays;
        long cyclesSinceEpoch = daysSinceEpoch / 2;

        // Find the next reset time by adding cycles * 2 days back to the epoch
        DateTime nextResetTime = epoch.AddDays((cyclesSinceEpoch + 1) * 2);

        endTime = nextResetTime;
    }

    public int CalculateTwoDayIntervals() {
        TimeSpan timeSinceEpoch = DateTime.UtcNow - resetDate;
        return (int)(timeSinceEpoch.TotalDays / 2); // Numbers of days / 2
    }
    
    public DateTime ReverseTwoDayInterval(int intervals) {
        // Returns the start time, NOT the end sime. To get end time, just add 2 days
        DateTime epoch = resetDate;
        return epoch.AddDays(intervals * 2); // Days after epoch
    }

    public void CheckForRewards(string message = null)
    {

        if (gemRewardsToCollect > 0)
        {
            collectRewardText.text = playerState.FormatPrice(gemRewardsToCollect);
            collectReward.SetActive(true);

            if (message != null)
            {
                collectRewardMessage.text = GetLocalizedValue(message);
            }
        }

        // There's no cloud data to retrieve for now
        /*try
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
        }*/
    }

    public void CollectLeaderboardRewards() {
        long gemValue = gemRewardsToCollect;
        gemRewardsToCollect = 0;
        playerState.AddGems(gemValue);
        collectReward.SetActive(false);
        collectReward.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("CONGRATULATIONS! YOU RECEIVED SOME REWARDS!");
    }

    public void UpdateLeaderBoardData()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return;
        }

        try
        {
            oreLeaderboardScores = oreLeaderboardResultsPage.GetLeaderboardScores();

            int playerTier = 0;
            int results = oreLeaderboardScores.Count;

            for (int i = 0; i != 10; i++)
            {
                orePlayerScoreBars[i].SetActive(false);
            }

            // Find player pos so we know the tier to display
            for (int i = 0; i != results; i++)
            {
                if (oreLeaderboardScores[i].GetUUID() != "Player")
                    continue;

                string playerTierString = GetTier(i);
                // Gold
                if (playerTierString == "Gold")
                {
                    playerTier = 2;
                    oreTierImage.sprite = tierSprites[2];
                    oreTierText.text = GetLocalizedValue(leaderboardTiers[2]);
                }
                // Silver
                else if (playerTierString == "Silver")
                {
                    playerTier = 1;
                    oreTierImage.sprite = tierSprites[1];
                    oreTierText.text = GetLocalizedValue(leaderboardTiers[1]);
                }
                // Bronze
                else if (playerTierString == "Bronze")
                {
                    playerTier = 0;
                    oreTierImage.sprite = tierSprites[0];
                    oreTierText.text = GetLocalizedValue(leaderboardTiers[0]);
                }

                break;
            }

            // Lowest player in next tier
            int firstPlayerIndex = 0;
            // Highest player in last tier
            int lastPlayerIndex = 0;
            int playerBarCounter = 0;
            for (int i = 0; i != results; i++)
            {

                // If current player is outside of the local players tier, save the index so we can determine the threshold score for the next and last tier
                // Also don't display this player because they are in another tier
                string currentTier = GetTier(i);
                if (currentTier != leaderboardTiersMatching[playerTier])
                {
                    // Gold
                    if (currentTier == "Gold")
                    {
                        // If gold, then we know its the next tier
                        firstPlayerIndex = i;
                    }
                    // Bronze
                    else if (currentTier == "Bronze")
                    {
                        // If bronze, then we know its the last tier
                        if (lastPlayerIndex == 0)
                        {
                            lastPlayerIndex = i;
                        }
                    }
                    // Silver
                    else if (currentTier == "Silver")
                    {
                        // If silver we need to determine if player is in gold or bronze first.
                        // If gold, then silver is last tier, if bronze then its next tier
                        if (leaderboardTiersMatching[playerTier] == "Gold")
                        {
                            if (lastPlayerIndex == 0)
                            {
                                lastPlayerIndex = i;
                            }
                        }
                        else
                        {
                            firstPlayerIndex = i;
                        }
                    }
                    continue;
                }

                orePlayerScoreBars[playerBarCounter].SetActive(true);

                orePlayerNameTextMeshes[playerBarCounter].text = oreLeaderboardScores[i].GetPlayerName();
                oreScoreTextMeshes[playerBarCounter].text = playerState.FormatPrice(oreLeaderboardScores[i].GetScore());

                // If local player, then highlight
                if (oreLeaderboardScores[i].GetUUID() == "Player")
                {
                    orePlayerScoreImages[playerBarCounter].color = new(255 / 255f, 204 / 255f, 0 / 255f);
                    orePlayerNameTextMeshes[playerBarCounter].text = PlayerPrefs.GetString("PlayerName");
                }
                else
                {
                    orePlayerScoreImages[playerBarCounter].color = new(1, 1, 1);
                }

                oreRewardTextMeshes[playerBarCounter].text = playerState.FormatPrice(rewardAmounts[playerTier][playerBarCounter]);

                playerBarCounter++;
            }

            // Display score of highest player in last tier if a last tier exists
            if (lastPlayerIndex != 0)
            {
                oreLastTierText.text = GetLocalizedValue("LAST TIER: {0}", playerState.FormatPrice(oreLeaderboardScores[lastPlayerIndex].GetScore()));
                oreLastTierText.gameObject.SetActive(true);
            }
            else
            {
                oreLastTierText.gameObject.SetActive(false);
            }

            // Display score of lowest player in next tier if a next tier exists
            if (firstPlayerIndex != 0)
            {
                oreNextTierText.text = GetLocalizedValue("NEXT TIER: {0}", playerState.FormatPrice(oreLeaderboardScores[firstPlayerIndex].GetScore()));
                oreNextTierText.gameObject.SetActive(true);
            }
            else
            {
                oreNextTierText.gameObject.SetActive(false);
            }

            firstPlayerIndex = 0;
            lastPlayerIndex = 0;
            playerBarCounter = 0;

            lastUpdateTimer = 0;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private string GetTier(int position)
    {
        if (position < 10)
        {
            return "Gold";
        }
        else if (position < 20)
        {
            return "Silver";
        }

        return "Bronze";
    }

    public void AddOreScore(double amount)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return;
        }

        oreLeaderboardResultsPage.AddPlayerScore(new BigInteger(amount));
    }

    private void ResetLeaderboard()
    {
        gemRewardsToCollect += CalculatePlayerRewards();
        CheckForRewards();

        // Start next leaderboard
        int uniqueUserInt = oreLeaderboardResultsPage.GetUniqueUserInt();
        oreLeaderboardResultsPage = new(0, endTime, uniqueUserInt);
    }

    public int CalculatePlayerRewards()
    {
        // Happens upon new game save
        if (previousInterval == 0)
        {
            return 0;
        }

        // Use previous interval to calculate scores of last leaderboard
        LeaderboardResults previousLeaderboard = new(oreLeaderboardResultsPage.playerLS, ReverseTwoDayInterval(previousInterval).AddDays(2), oreLeaderboardResultsPage.GetUniqueUserInt());

        List<LeaderboardPlayer> previousResults = previousLeaderboard.GetLeaderboardScores();

        for (int i = 0; i != previousResults.Count; i++)
        {
            if (previousResults[i].GetUUID() != "Player") {
                continue;
            }
            
            int counter = 0;

            // Iterate rewards in reverse, because results start from first place and go to last, but reward array goes from bronze to gold
            for (int j = rewardAmounts.Length - 1; j != -1; j--)
            {
                // Iterate forward here, but each subarray goes from first to last
                for (int k = 0; k != rewardAmounts[j].Length; k++)
                {
                    // Find the reward at the same position of the player
                    if (counter == i)
                    {
                        return rewardAmounts[j][k];
                    }

                    counter++;
                }
            }
        }

        // Fallback should never be reached, give 600 gems if somehow reached
        return 600;
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key); ;

        // If no translation, just return the key
        if (entry == null)
        {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public void LoadData(GameData data)
    {
        SetLeaderBoardTimer();

        oreLeaderboardResultsPage = new(BigInteger.Parse(data.playerLS), endTime, data.uniqueUserInt);
        this.gemRewardsToCollect = data.gemRewardsToCollect;

        this.twoDIL = CalculateTwoDayIntervals();
        previousInterval = data.twoDIL;

        StartCoroutine(TimerController());
        UpdateLeaderBoardData();
    }

    public void SaveData(ref GameData data)
    {
        data.gemRewardsToCollect = this.gemRewardsToCollect;
        data.playerLS = oreLeaderboardResultsPage.playerLS.ToString();
        data.uniqueUserInt = oreLeaderboardResultsPage.uniqueUserInt;
        data.twoDIL = this.twoDIL;
    }
}