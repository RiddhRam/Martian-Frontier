using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class OfflineRewardsManager : MonoBehaviour, IDataPersistence
{
    
    double cashEarnedOffline;
    public GameObject collectRewardPanel;
    public TextMeshProUGUI collectRewardCashText;

    public void CollectOfflineRewards()
    {
        collectRewardPanel.SetActive(false);
    }

    public void LoadData(GameData data)
    {
        DateTime offlineDateTime = DateTimeOffset.FromUnixTimeSeconds(data.offlineTime).UtcDateTime;
        TimeSpan difference = DateTime.UtcNow - offlineDateTime;

        // If bought at least one upgrade, then player (most likely) has at least 1 drone because that's the cheapest upgrade
        // It's also the only affordable upgrade unless you use gems to get cash
        if (data.upgradeBayOptionsPurchased.Count == 0)
        {
            return;
        }

        double minutesGone = difference.TotalMinutes;

        // Must be offline for at least 5 mins
        if (minutesGone < 5)
        {
            return;
        }

        // 5 hours is the max offline reward limit
        if (minutesGone > 300)
        {
            minutesGone = 300;
        }

        cashEarnedOffline = minutesGone * 0.05 * data.highestMined;

        // Reward player after game loads to avoid conflicts with player state loading
        StartCoroutine(GivePlayerCash());
    }

    private IEnumerator GivePlayerCash()
    {
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);
        
        // Save this immediately in case the player logs off again before collecting
        PlayerState.Instance.AddCash(cashEarnedOffline);

        collectRewardCashText.text = PlayerState.Instance.FormatPrice(new System.Numerics.BigInteger(cashEarnedOffline));
        collectRewardPanel.SetActive(true);
    }

    public void SaveData(ref GameData data)
    {
        // Save nothing (the time is automatically save in FileDataHandler.Save())
    }
}
