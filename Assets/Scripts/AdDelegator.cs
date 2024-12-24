using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using UnityEngine.UI;

public class AdDelegator : MonoBehaviour, IDataPersistence
{
    private string _adUnitId;

    public GameObject[] adButtons;
    public GameObject noInternetIcon;
    public GameObject[] timerTexts;
    [SerializeField]
    private string[] rewardTypes;

    private RewardedAd[] rewardedAds = new RewardedAd[3];
    private int timer = 60;
    private bool internetReachable = false;
    // This needs to be seperate because user can swap vehicle while boost active
    public float originalSpeed;
    public bool speedBoostActive;
    private bool currentlyUsingDriller = true;
    private int[] timerIndexes = new int[3];

    // Start is called before the first frame update
    void Start()
    {
        SetAdUnitId();

        // Need this so rewarded ads actually reward in the real app
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            for (int i = 0; i != rewardedAds.Length; i++) {
                LoadRewardedAd(i);
            }
            
            // This callback is called once the MobileAds SDK is initialized.
            IncrementLoadedItems();
        });
    }

    void Update() {
        timer++;

        if (timer < 60) {
            return;
        }
        timer = 0;
        
        // Make sure there is internet access
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            for (int i = 0; i != rewardedAds.Length; i++) {
                if (rewardedAds[i] != null) {
                    rewardedAds[i].Destroy();
                    rewardedAds[i] = null;
                }
            }
            
            internetReachable = false;
            ToggleDisplay();
            return;
        }

        FillEmptyAdSlots();

        internetReachable = true;
        ToggleDisplay();
    }

    // Choose the right ad unit before doing anything with ads
    private void SetAdUnitId()
    {
        bool isDebugBuild = Debug.isDebugBuild;

        #if UNITY_ANDROID
            if (isDebugBuild)
            {
                _adUnitId = "ca-app-pub-3940256099942544/5224354917"; // Android Test Ad Unit
            }
            else
            {
                _adUnitId = "ca-app-pub-5607588731152504/9913767660"; // Android Real Ad Unit
            }
        #elif UNITY_IPHONE
            if (isDebugBuild)
            {
                _adUnitId = "ca-app-pub-3940256099942544/1712485313"; // iOS Test Ad Unit
            }
            else
            {
                _adUnitId = "ca-app-pub-5607588731152504/4737462608"; // iOS Real Ad Unit
            }
        #else
            _adUnitId = "unused"; // Default for other platforms
        #endif
    }

    // Loads the rewarded ad.
    public void LoadRewardedAd(int rewardIndex)
    {
        // Clean up the old ad before loading a new one.
        if (rewardedAds[rewardIndex] != null)
        {
            rewardedAds[rewardIndex].Destroy();
            rewardedAds[rewardIndex] = null;
        }

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

                    try {
                        IncrementLoadedItems();
                    } catch {
                    }
                    
                    return;
                }

                //Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                rewardedAds[rewardIndex] = ad;

                try {
                    IncrementLoadedItems();
                } catch {
                }
            });
    }

    // Show ad to user
    public void ShowRewardedAd(string type)
    {
        int rewardIndex = 0;

        for (int i = 0; i != rewardTypes.Length; i++) {
            if (type == rewardTypes[i]) {
                rewardIndex = i;
                break;
            }
        }

        if (rewardedAds[rewardIndex] != null && rewardedAds[rewardIndex].CanShowAd())
        {
            rewardedAds[rewardIndex].Show((Reward reward) =>
            {
                if (type == "Profit") {
                    RewardWithProfit();
                } else if (type == "Speed") {
                    RewardWithSpeed();
                } if (type == "Vision") {
                    RewardWithVision();
                }
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                LoadRewardedAd(rewardIndex);
            });

            // Listen to user events during ad
            RegisterEventHandlers(rewardedAds[rewardIndex]);
            return;
        }

        // If unable to show ad, reward user anyways
        if (type == "Profit") {
            RewardWithProfit();
        } else if (type == "Speed") {
            RewardWithSpeed();
        } if (type == "Vision") {
            RewardWithVision();
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
            FillEmptyAdSlots();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " + "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            FillEmptyAdSlots();
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

    private void RewardWithProfit(int? totalTime = 180) {
        RefineryController refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        // Reset to 1 after 3 mins
        refineryController.SetProfitMultiplier(2);
        StartCoroutine(StartRewardCountdown(0, () => refineryController.SetProfitMultiplier(1), (int) totalTime));
        AnalyticsDelegator.Instance.AdWatchAttempt("Profit");
    }

    private void RewardWithSpeed(int? totalTime = 180) {
        PlayerMovement playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        originalSpeed = playerMovement.GetSpeed();
        // Reset to original value after 3 mins
        playerMovement.SetSpeed(originalSpeed * 1.5f);
        StartCoroutine(StartSpeedCountdown((int) totalTime));
        AnalyticsDelegator.Instance.AdWatchAttempt("Speed");
    }

    private void RewardWithVision(int? totalTime = 180) {
        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        // Reset to 3 after 3 mins
        mineRenderer.SetVisionRadius(9);
        StartCoroutine(StartRewardCountdown(2, () => mineRenderer.SetVisionRadius(3), (int) totalTime));
        AnalyticsDelegator.Instance.AdWatchAttempt("Vision");
    }

    private IEnumerator StartRewardCountdown(int rewardIndex, Action callbackFunc, int totalTime) {
        adButtons[rewardIndex].GetComponent<Button>().interactable = false;
        // Hide the button
        adButtons[rewardIndex].transform.GetChild(0).gameObject.SetActive(false);
        // Show timer
        timerTexts[rewardIndex].SetActive(true);

        // Initialize the timer to 3:00 (3 minutes in seconds) 
        while (totalTime > 0) {
            // Calculate minutes and seconds
            int minutes = totalTime / 60;
            int seconds = totalTime % 60;

            string timerText = $"{minutes}:{seconds:D2}";

            // Update the timer text (assuming it's a TMP Text component)
            timerTexts[rewardIndex].GetComponent<TMPro.TextMeshProUGUI>().text = timerText;
            timerIndexes[rewardIndex] = totalTime - 1;
            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        // Reset
        callbackFunc?.Invoke();
        
        timerTexts[rewardIndex].GetComponent<TMPro.TextMeshProUGUI>().text = "0:00";
        timerTexts[rewardIndex].SetActive(false);
        timerIndexes[rewardIndex] = 0;

        adButtons[rewardIndex].transform.GetChild(0).gameObject.SetActive(true);
        // Don't renable the button if its the vision button and user is using something other than a driller
        if (adButtons[rewardIndex].name.Contains("Vision") && !currentlyUsingDriller) {
            yield break;
        }
        adButtons[rewardIndex].GetComponent<Button>().interactable = true;
    }

    // This needs to be seperate because user can swap vehicle while boost active
    private IEnumerator StartSpeedCountdown(int totalTime) {
        speedBoostActive = true;
        adButtons[1].GetComponent<Button>().interactable = false;
        // Hide the button
        adButtons[1].transform.GetChild(0).gameObject.SetActive(false);
        // Show timer
        timerTexts[1].SetActive(true);

        // Initialize the timer to 3:00 (3 minutes in seconds)
        while (totalTime > 0) {
            // Calculate minutes and seconds
            int minutes = (int) totalTime / 60;
            int seconds = (int) totalTime % 60;

            string timerText = $"{minutes}:{seconds:D2}";

            // Update the timer text (assuming it's a TMP Text component)
            timerTexts[1].GetComponent<TMPro.TextMeshProUGUI>().text = timerText;
            timerIndexes[1] = totalTime - 1;
            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        timerTexts[1].GetComponent<TMPro.TextMeshProUGUI>().text = "0:00";
        timerTexts[1].SetActive(false);
        timerIndexes[1] = 0;

        adButtons[1].transform.GetChild(0).gameObject.SetActive(true);
        adButtons[1].GetComponent<Button>().interactable = true;

        speedBoostActive = false;
    }

    public void SetUsingDriller(bool usingDriller) {
        currentlyUsingDriller = usingDriller;
        // Vision boost is useless if not using a driller, so make the button uninteractable
        for (int i = 0; i != adButtons.Length; i++) {

            // Make sure it's the vision button
            if (!adButtons[i].name.Contains("Vision")) {
                continue;
            }

            // If it's currently active don't do anything
            if (timerIndexes[i] > 0) {
                return;
            }

            adButtons[i].GetComponent<Button>().interactable = usingDriller;
            break;
        }
    }

    public void LoadData(GameData data) {
        this.timerIndexes = data.timerIndexes;
        for (int i = 0; i != timerIndexes.Length; i++) {
            // Make sure timerIndex was greater than 0
            if (timerIndexes[i] <= 0) {
                continue;
            }

            // Speed has it's own function
            if (adButtons[i].name.Contains("Speed")) {
                RewardWithSpeed(timerIndexes[i]);
                continue;
            }

            // Call the appropriate function
            if (i == 0) {
                RewardWithProfit(timerIndexes[i]);
                continue;
            }

            RewardWithVision(timerIndexes[i]);
        }

        IncrementLoadedItems();
    }

    public void SaveData(ref GameData data) {
        data.timerIndexes = this.timerIndexes;
    }

    private void IncrementLoadedItems() {
        StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
    }    

    private void FillEmptyAdSlots() {
        for (int i = 0; i != rewardedAds.Length; i++) {
            if (rewardedAds[i] == null) {
                LoadRewardedAd(i);
            }
        }
    }

}
