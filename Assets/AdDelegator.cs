using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using UnityEngine.UI;

public class AdDelegator : MonoBehaviour
{
    // These ad units are configured to always serve test ads.
    #if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/5224354917";
    #elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/1712485313";
    #else
    private string _adUnitId = "unused";
    #endif

    public GameObject[] adButtons;
    public GameObject noInternetIcon;
    public GameObject[] timerTexts;

    private RewardedAd rewardedAd;
    private int timer = 60;
    private bool internetReachable = false;
    private RefineryController refineryController;
    private PlayerMovement playerMovement;
    // This needs to be seperate because user can swap vehicle while boost active
    public float originalSpeed;
    public bool speedBoostActive;
    private MineRenderer mineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // This callback is called once the MobileAds SDK is initialized.
        });
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
    }

    void Update() {
        timer++;

        if (timer < 60) {
            return;
        }
        timer = 0;
        
        // Make sure there is internet access
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            rewardedAd = null;
            internetReachable = false;
            ToggleDisplay();
            return;
        }

        if (rewardedAd == null) {
            LoadRewardedAd();
        }
        internetReachable = true;
        ToggleDisplay();
    }

    // Loads the rewarded ad.
    public void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        //Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_adUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                    "with error : " + error);
                    return;
                }

                //Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());

                rewardedAd = ad;
            });
    }

    public void ShowRewardedAd(string type)
    {
        //const string rewardMsg = "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                if (type == "Profit") {
                    RewardWithProfit();
                } else if (type == "Speed") {
                    RewardWithSpeed();
                } if (type == "Vision") {
                    RewardWithVision();
                }
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                LoadRewardedAd();
            });

            // Listen to user events during ad
            RegisterEventHandlers(rewardedAd);
        }
    }

    // Listen to user events during ad
    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            //Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            //Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            //Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            RegisterReloadHandler(ad);
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            //Debug.LogError("Rewarded ad failed to open full screen content " + "with error : " + error);
            RegisterReloadHandler(ad);
        };
    }

    // Load a new ad after showing an ad
    private void RegisterReloadHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            //Debug.Log("Rewarded Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " + "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
    }

    // Flip between showing ad buttons, or internet error depending on internet reachability
    private void ToggleDisplay() {
        if (internetReachable) {
            noInternetIcon.SetActive(false);
            for (int i = 0; i != adButtons.Length; i++) {
                adButtons[i].SetActive(true);
            }
            return;
        }

        for (int i = 0; i != adButtons.Length; i++) {
            adButtons[i].SetActive(false);
        }
        noInternetIcon.SetActive(true);
    }

    private void RewardWithProfit() {
        // Reset to 1 after 3 mins
        refineryController.SetProfitMultipler(2);
        StartCoroutine(StartRewardCountdown(0, () => refineryController.SetProfitMultipler(1)));
    }

    private void RewardWithSpeed() {
        originalSpeed = playerMovement.GetSpeed();
        // Reset to original value after 3 mins
        playerMovement.SetSpeed(originalSpeed * 1.5f);
        StartCoroutine(StartSpeedCountdown());
    }

    private void RewardWithVision() {
        // Reset to 3 after 3 mins
        mineRenderer.SetVisionRadius(9);
        StartCoroutine(StartRewardCountdown(2, () => mineRenderer.SetVisionRadius(3)));
    }

    private IEnumerator StartRewardCountdown(int rewardIndex, Action callbackFunc) {
        // Hide the button
        adButtons[rewardIndex].transform.GetChild(0).gameObject.SetActive(false);
        // Show timer
        timerTexts[rewardIndex].SetActive(true);

        // Initialize the timer to 3:00 (3 minutes in seconds)
        int totalTime = 180; 
        while (totalTime > 0) {
            // Calculate minutes and seconds
            int minutes = totalTime / 60;
            int seconds = totalTime % 60;

            string timerText = $"{minutes}:{seconds:D2}";

            // Update the timer text (assuming it's a TMP Text component)
            timerTexts[rewardIndex].GetComponent<TMPro.TextMeshProUGUI>().text = timerText;

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        // Reset
        callbackFunc?.Invoke();
        adButtons[rewardIndex].transform.GetChild(0).gameObject.SetActive(true);
        timerTexts[rewardIndex].SetActive(false);
    }

    // This needs to be seperate because user can swap vehicle while boost active
     private IEnumerator StartSpeedCountdown() {
        speedBoostActive = true;
        // Hide the button
        adButtons[1].transform.GetChild(0).gameObject.SetActive(false);
        // Show timer
        timerTexts[1].SetActive(true);

        // Initialize the timer to 3:00 (3 minutes in seconds)
        int totalTime = 180; 
        while (totalTime > 0) {
            // Calculate minutes and seconds
            int minutes = totalTime / 60;
            int seconds = totalTime % 60;

            string timerText = $"{minutes}:{seconds:D2}";

            // Update the timer text (assuming it's a TMP Text component)
            timerTexts[1].GetComponent<TMPro.TextMeshProUGUI>().text = timerText;

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        adButtons[1].transform.GetChild(0).gameObject.SetActive(true);
        timerTexts[1].SetActive(false);
        speedBoostActive = false;
    }


    public void SetUsingDriller(bool usingDriller) {
        // Vision boost is useless if not using a driller, so make the button uninteractable
#pragma warning disable CS0162 // Unreachable code detected
        for (int i = 0; i != adButtons.Length; i++) {

            // Make sure it's the vision button
            if (!adButtons[i].name.Contains("Vision")) {
                continue;
            }

            adButtons[i].GetComponent<Button>().interactable = usingDriller;
            return;
        }
#pragma warning restore CS0162 // Unreachable code detected
    }
}
