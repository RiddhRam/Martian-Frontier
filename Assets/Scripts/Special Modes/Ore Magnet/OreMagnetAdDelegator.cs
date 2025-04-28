using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Mediation.UnityAds.Api;

public class OreMagnetAdDelegator : MonoBehaviour, IDataPersistence
{
    private string _adUnitId = "unused";
    public GameObject adButton;
    public TextMeshProUGUI oreMagnetAdTimerText;
    public GameObject customAdScreen;
    public GameObject signupNoWifi;
    public GameObject signUpButton;
    public GameObject accountNoWifi;
    public GameObject changeNameButton;
    public GameObject deleteAccountButton;
    public GameObject doubleConvertRewardButton;
    public GameObject convertRewardNoWifi;

    private RewardedAd rewardedAd;
    private RewardedAd convertAd;
    private int timer = 0;
    private bool internetReachable = false;

    private int oreMagnetAdTimer = 0;

    public DataPersistenceManager dataPersistenceManager;
    public AnalyticsDelegator analyticsDelegator;
    public CloudDelegator cloudDelegator;
    public PlayerState playerState;
    public OreMagnetRoundManager oreMagnetRoundManager;

    private bool adsInitialized = false;
    private string adPermissionGiven;
    // After 30 seconds of user watching an ad, request a new one.
    // Once user watches an ad, ad boosts are free for the next 30 seconds
    DateTime lastAdShown;
    private bool cloudLoading = false;
    private bool displayStatus = true;

    // Start is called before the first frame update
    void Start()
    {
        adPermissionGiven = PlayerPrefs.GetString("APG");

        if (adPermissionGiven == "Not Allowed") {   
            UnityAds.SetConsentMetaData("gdpr.consent", false);
            UnityAds.SetConsentMetaData("privacy.consent", false);
        } else {
            UnityAds.SetConsentMetaData("gdpr.consent", true);
            UnityAds.SetConsentMetaData("privacy.consent", true);
        }

        SetAdUnitId();

        // Need this so rewarded ads actually reward in the real app
        MobileAds.RaiseAdEventsOnUnityMainThread = true; 
        // ADMOB DISABLE
        MobileAds.Initialize((InitializationStatus initstatus) =>
        {
            adsInitialized = true;
            FillEmptyAdSlots();
        });
    }

    void FixedUpdate() {

        // ADMOB DISABLE
        // If no internet
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            if (rewardedAd != null) {
                rewardedAd.Destroy();
                rewardedAd = null;
            }
            
            internetReachable = false;
            ToggleDisplay();
            return;
        } 

        // If there is internet
        internetReachable = true;
        ToggleDisplay();
        
        timer++;

        if (timer < 250) {
            return;
        }
        timer = 0;


        FillEmptyAdSlots();
    }

    // Choose the right ad unit before doing anything with ads
    private void SetAdUnitId()
    {
        bool isDebugBuild = Debug.isDebugBuild;

        // Android Real App ID
        // ca-app-pub-5607588731152504~5074236463
        // iOS App ID
        // ca-app-pub-5607588731152504~7307043368

        // Android Test Ad Unit
        // ca-app-pub-3940256099942544/5224354917
        // Android Real Ad Unit
        // ca-app-pub-5607588731152504/1308199501
        // iOS Test Ad Unit
        // ca-app-pub-3940256099942544/1712485313
        // iOS Real Ad Unit
        // ca-app-pub-5607588731152504/4737462608

        if (Application.platform == RuntimePlatform.Android)
        {
            if (isDebugBuild)
            {
                _adUnitId = "ca-app-pub-3940256099942544/5224354917"; // Android Test Ad Unit
            }
            else
            {
                _adUnitId = "ca-app-pub-5607588731152504/1308199501"; // Android Real Ad Unit
            }
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (isDebugBuild)
            {
                _adUnitId = "ca-app-pub-3940256099942544/1712485313"; // iOS Test Ad Unit
            }
            else
            {
                _adUnitId = "ca-app-pub-5607588731152504/4737462608"; // iOS Real Ad Unit
            }
        }
        else {
            _adUnitId = "unknown"; // Default for other platforms
        }
    }

    // Loads the rewarded ad.
    public void LoadRewardedAd(string type)
    {
        bool currentCloudLoadState = cloudLoading;
        // ADMOB DISABLE
        //IncrementLoadedItems();

        // ADMOB DISABLE
        // Clean up the old ad before loading a new one.
        if (rewardedAd != null && type == "Speed") {
            rewardedAd.Destroy();
            rewardedAd = null;
        } else if (convertAd != null && type == "Convert") {
            convertAd.Destroy();
            convertAd = null;
        }

        // send the request to load the ad.
        if (adsInitialized && _adUnitId != "unused") {
             // create our request used to load the ad.
            var adRequest = new AdRequest();

            RewardedAd.Load(_adUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                    "with error : " + error);

                    if (currentCloudLoadState == cloudLoading) { 
                        cloudLoading = true;  
                    }
                    
                    return;
                }

                //Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                if (type == "Speed") {
                    rewardedAd = ad;
                } else if (type == "Convert") {
                    convertAd = ad;
                }
                
                if (currentCloudLoadState == cloudLoading) {   
                    cloudLoading = true;
                }
            });
        } 
        // if MobileAds SDK not initialized
        else {
            if (currentCloudLoadState == cloudLoading) {   
                cloudLoading = true;
            }
        }
        
    }

    // Show ad to user
    public void ShowRewardedAd()
    {
        // If user watched an ad in the last 30 seconds or first time playing
        if (lastAdShown >= DateTime.Now.AddSeconds(-90)) {
            RewardBoost();
            dataPersistenceManager.SaveGame();
            return;
        }

        // ADMOB DISABLE
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            lastAdShown = DateTime.Now;
            RewardBoost();
            dataPersistenceManager.SaveGame();

            // Listen to user events during ad
            RegisterEventHandlers(rewardedAd);
            return;
        }

        // If unable to show ad, use custom screen
        StartCoroutine(UseCustomAdScreen(() => RewardBoost()));

        lastAdShown = DateTime.Now;
        dataPersistenceManager.SaveGame();
    }

    public void ShowConvertRewardedAd() {
        try {
            analyticsDelegator.AdWatchAttempt("Convert");
        } catch {
        }

        // ADMOB DISABLE
        if (convertAd != null && convertAd.CanShowAd())
        {   
            convertAd.Show((Reward reward) =>
            {
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            // Reward user
            ConvertRewardSuccess();
            
            // Listen to user events during ad
            RegisterEventHandlers(convertAd);
            return;
        } else {
            // CustomAdScreen if no ad ready
            StartCoroutine(UseCustomAdScreen(() => ConvertRewardSuccess()));
        }
        
    }

    public void ConvertToGems() {
        // 1 gem per 30 credits
        playerState.AddGems((int) playerState.GetUserCredits() / 30);
        Debug.Log((int) playerState.GetUserCredits() / 30);
        playerState.SubtractCredits((long) playerState.GetUserCredits());

        oreMagnetRoundManager.UpdateConversion();
    }

    private void ConvertRewardSuccess() {
        // 1 gem per 15 credits
        playerState.AddGems((int) playerState.GetUserCredits() / 15);
        Debug.Log((int) playerState.GetUserCredits() / 15);
        playerState.SubtractCredits((long) playerState.GetUserCredits());

        oreMagnetRoundManager.UpdateConversion();
    }

    private IEnumerator UseCustomAdScreen(Action callbackFunc) {

        Slider progressSlider = customAdScreen.transform.GetChild(3).GetComponent<Slider>();

        customAdScreen.SetActive(true);

        int timer = 0;

        while (timer < 15) {
            progressSlider.value = timer / 15f; // Update the slider value
            timer++; // Increment the timer
            yield return new WaitForSeconds(1f); // Wait for 1 second
        }

        customAdScreen.SetActive(false);

        callbackFunc?.Invoke();

        yield break;
    }

    // Listen to user events during ad
    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            /*Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));*/
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

    // Flip between showing ad buttons and Ad Opt Out text, or internet error depending on internet reachability
    private void ToggleDisplay() {
        if (internetReachable && !displayStatus) {
            signupNoWifi.SetActive(false);
            signUpButton.SetActive(true);
            accountNoWifi.SetActive(false);
            changeNameButton.SetActive(true);
            deleteAccountButton.SetActive(true);

            adButton.SetActive(true);

            convertRewardNoWifi.SetActive(false);
            doubleConvertRewardButton.SetActive(true);

            _ = cloudDelegator.AttemptLogIn();
            
            displayStatus = true;
            return;
        }

        if (!displayStatus || internetReachable) {
            return;
        }
        
        signupNoWifi.SetActive(true);
        signUpButton.SetActive(false);
        accountNoWifi.SetActive(true);
        changeNameButton.SetActive(false);
        deleteAccountButton.SetActive(false);
        
        adButton.SetActive(false);

        convertRewardNoWifi.SetActive(true);
        doubleConvertRewardButton.SetActive(false);

        displayStatus = false;
    }

    private void RewardBoost(int? totalTime = 90) {
        PlayerMovement playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(12);

        StartCoroutine(StartRewardCountdown((int) totalTime));

        LogAnalytics("Speed");
    }

    private void LogAnalytics(string analyticToLog) {
        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.AdWatchAttempt(analyticToLog);
    }

    private IEnumerator StartRewardCountdown(int totalTime) {

        adButton.SetActive(false);
        adButton.transform.parent.GetChild(1).gameObject.SetActive(true);

        int minutes;
        int seconds;
        string timerText;

        // Initialize the timer to 3:00 (3 minutes in seconds)
        while (totalTime > 0) {
            // Calculate minutes and seconds
            minutes = totalTime / 60;
            seconds = totalTime % 60;
            timerText = $"{minutes}:{seconds:D2}";

            // Update the timer text (assuming it's a TMP Text component)
            oreMagnetAdTimerText.text = timerText;
            oreMagnetAdTimer = totalTime - 1;
            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        oreMagnetAdTimerText.text = "0:00";
        oreMagnetAdTimer = 0;

        adButton.SetActive(true);
        adButton.transform.parent.GetChild(1).gameObject.SetActive(false);

        PlayerMovement playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(6);
        yield break;
    }

    public void LoadData(GameData data) {

        this.oreMagnetAdTimer = data.oreMagnetAdTimer;

        if (oreMagnetAdTimer > 0) {
            // Reward
            RewardBoost(oreMagnetAdTimer);
        }
    }

    public void SaveData(ref GameData data) {        
        data.oreMagnetAdTimer = this.oreMagnetAdTimer;
    }

    private void FillEmptyAdSlots() {
        if (rewardedAd == null || !rewardedAd.CanShowAd()) {
            LoadRewardedAd("Speed");
        }
        if (convertAd == null || !convertAd.CanShowAd()) {
            LoadRewardedAd("Convert");
        }
    }

}
