using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Mediation.UnityAds.Api;

public class AdDelegator : MonoBehaviour, IDataPersistence
{
    private static AdDelegator _instance;
    public static AdDelegator Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<AdDelegator>();
            }
            return _instance;
        }
    }

    private string _adUnitId = "unused";
    public GameObject adButton;
    public GameObject rewardDisplay;
    public TextMeshProUGUI visionText;
    public TextMeshProUGUI profitText;
    public TextMeshProUGUI rewardAdTimerText;
    public string[] rewardTypes;
    public GameObject movementJoystick;
    public GameObject tutorial;
    public GameObject customAdScreen;
    public GameObject signupNoWifi;
    public GameObject signUpButton;
    public GameObject accountNoWifi;
    public GameObject changeNameButton;
    public GameObject deleteAccountButton;
    public GameObject leaderboardNoWifi;
    public GameObject doubleCrateRewardButton;
    public GameObject crateRewardNoWifi;
    public GameObject teamNoWifi;
    public GameObject lobbyAdDisplay;
    public TextMeshPro lobbyAdRewardAmountText;
    public TextMeshPro lobbyAdRewardTimerText;

    public GameObject leaderboardCashPanel;
    private bool cashPanelWasOpen = true;

    private RewardedAd rewardedAd;
    private RewardedAd crateAd;

    private int timer = 0;
    private bool internetReachable = false;
    // This needs to be seperate because user can swap vehicle while boost active
    public float originalSpeed;
    public bool speedBoostActive;

    private int rewardAdTimer = 0;

    private double lobbyRewardAmount;
    private int lobbyRewardTimer;

    public PlayerState playerState;
    public RefineryController refineryController;
    public SupplyCrateDelegator supplyCrateDelegator;
    public UpgradesDelegator upgradesDelegator;

    private bool adsInitialized = false;
    private string adPermissionGiven;
    private bool cloudLoading = false;
    private bool displayStatus = true;
    private bool firstTimePlaying = false;
    private bool disableAds = false;
    private bool adShowing = false;
    private System.Random rng = new();

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

        if (!disableAds) {
            FillEmptyAdSlots();
        }
    
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
        if (disableAds)
            return;

        bool currentCloudLoadState = cloudLoading;
        // ADMOB DISABLE
        //IncrementLoadedItems();

        // ADMOB DISABLE
        // Clean up the old ad before loading a new one.
        if (rewardedAd != null && type == "Boost") {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
        // Crate and lobby share the same ad
        else if (crateAd != null && (type == "Crate" || type == "Lobby"))
        {
            crateAd.Destroy();
            crateAd = null;
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
                if (type == "Boost") {
                    rewardedAd = ad;
                } else if (type == "Crate" || type == "Lobby") {
                    crateAd = ad;
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
        if (disableAds)
            return;

        // ADMOB DISABLE
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            adShowing = true;
            rewardedAd.Show((Reward reward) =>
            {
                adShowing = false;
                RewardBoost();
                DataPersistenceManager.Instance.SaveGame();
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            // Listen to user events during ad
            RegisterEventHandlers(rewardedAd);
            return;
        }

        // ADMOB SERVING LIMIT
        RewardBoost();
        DataPersistenceManager.Instance.SaveGame();
        return;

        // If unable to show ad, use custom screen
        StartCoroutine(UseCustomAdScreen(() => RewardBoost()));
    }

    public void ShowCrateRewardedAd() {
        if (disableAds) {
            return;
        }

        try {
            LogAnalytics("Crate");
        } catch {
        }

        // ADMOB DISABLE
        if (crateAd != null && crateAd.CanShowAd())
        {
            adShowing = true;
            crateAd.Show((Reward reward) =>
            {
                adShowing = false;
                // Reward user
                CrateRewardSuccess();
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            // Listen to user events during ad
            RegisterEventHandlers(crateAd);
            return;
        }

        // ADMOB SERVING LIMIT
        CrateRewardSuccess();
        return;
        
        // CustomAdScreen if no ad ready
        StartCoroutine(UseCustomAdScreen(() => CrateRewardSuccess()));
    }

    public void ShowLobbyRewardedAd() {
        if (disableAds || adShowing) {
            return;
        }

        try {
            LogAnalytics("Lobby");
        } catch {
        }

        // ADMOB DISABLE
        if (crateAd != null && crateAd.CanShowAd())
        {   
            adShowing = true;
            crateAd.Show((Reward reward) =>
            {
                adShowing = false;
                // Reward user
                LobbyRewardSuccess();
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            // Listen to user events during ad
            RegisterEventHandlers(crateAd);
            return;
        }

        // CustomAdScreen if no ad ready
        StartCoroutine(UseCustomAdScreen(() => LobbyRewardSuccess()));
    }

    private void CrateRewardSuccess() {
        StartCoroutine(CrateRewardCoroutine());
    }

    // Need to do this in a coroutine so game doesnt crash on android
    private IEnumerator CrateRewardCoroutine() {
        // So game doesnt crash on android
        yield return new WaitForEndOfFrame();

        supplyCrateDelegator.DoubleRewardsActivated();
        DataPersistenceManager.Instance.SaveGame();
    }

    private void LobbyRewardSuccess() {
        StartCoroutine(LobbyRewardCoroutine());
    }

    private IEnumerator LobbyRewardCoroutine() {
        // So game doesnt crash on android
        yield return new WaitForEndOfFrame();

        playerState.AddCash(lobbyRewardAmount);
        lobbyAdDisplay.SetActive(false);
    }

    public IEnumerator TryShowLobbyReward(double rewardAmount) {

        // show unless ads disabled or already showing or no internet or rewardAmount is less than 1000
        if (disableAds || lobbyRewardTimer > 0 || !internetReachable || rewardAmount < 1000) {
            yield break;
        }

        // ADMOB SERVING LIMIT
        yield break;

        lobbyRewardAmount = rewardAmount;
        lobbyAdRewardAmountText.text = playerState.FormatPrice(new System.Numerics.BigInteger(lobbyRewardAmount));

        lobbyRewardTimer = 30;
        lobbyAdDisplay.SetActive(true);

        while (lobbyRewardTimer > 0) {
            lobbyAdRewardTimerText.text = lobbyRewardTimer + "s";

            lobbyRewardTimer--;

            yield return new WaitForSecondsRealtime(1);
        }

        lobbyAdDisplay.SetActive(false);
    }

    private IEnumerator UseCustomAdScreen(Action callbackFunc) {
        if (disableAds) {
            yield break;
        }

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
            adShowing = false;
            RegisterReloadHandler(ad);
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            adShowing = false;
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
            Debug.Log("Rewarded Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            FillEmptyAdSlots();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded Ad failed to open full screen content " + "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            FillEmptyAdSlots();
        };
    }

    // Flip between showing ad buttons and Ad Opt Out text, or internet error depending on internet reachability
    private void ToggleDisplay() {
        // Enable
        if (internetReachable && !displayStatus) {
            signupNoWifi.SetActive(false);
            signUpButton.SetActive(true);
            accountNoWifi.SetActive(false);
            changeNameButton.SetActive(true);
            deleteAccountButton.SetActive(true);
            leaderboardCashPanel.SetActive(cashPanelWasOpen);
            leaderboardNoWifi.SetActive(false);

            if (!disableAds) {

                adButton.SetActive(true);

                if (!supplyCrateDelegator.adWatchedAlready) {
                    doubleCrateRewardButton.SetActive(true);
                    crateRewardNoWifi.SetActive(false);
                }

                teamNoWifi.SetActive(false);

            }

            CloudDelegator.Instance.AttemptLogIn();
            
            displayStatus = true;
            return;
        }

        if (!displayStatus || internetReachable) {
            return;
        }
        // Disable
        
        signupNoWifi.SetActive(true);
        signUpButton.SetActive(false);
        accountNoWifi.SetActive(true);
        changeNameButton.SetActive(false);
        deleteAccountButton.SetActive(false);
        cashPanelWasOpen = leaderboardCashPanel.activeSelf;
        leaderboardCashPanel.SetActive(false);
        leaderboardNoWifi.SetActive(true);
        
        if (!disableAds) {
            crateRewardNoWifi.SetActive(true);
            doubleCrateRewardButton.SetActive(false);
            adButton.SetActive(false);

            teamNoWifi.SetActive(true);
        }

        lobbyRewardTimer = 0;

        displayStatus = false;
    }

    private void RewardBoost(int? totalTime = 300) {

        //refineryController.SetProfitMultiplier(upgradesDelegator.refineryProfitMultiplier * upgradesDelegator.refineryProfitMultiplierBoost);
        //profitText.text = upgradesDelegator.refineryProfitMultiplierBoost.ToString() + "X";

        refineryController.SetProfitMultiplier(2);
        profitText.text = "2X";

        StartCoroutine(StartRewardCountdown((int) totalTime));

        LogAnalytics("Profit");
    }

    private void LogAnalytics(string analyticToLog) {
        AnalyticsDelegator.Instance.AdWatchAttempt(analyticToLog, MineRenderer.Instance.mineCount);
    }

    private IEnumerator StartRewardCountdown(int totalTime) {

        // So game doesnt crash on android
        yield return new WaitForEndOfFrame();

        adButton.SetActive(false);
        rewardDisplay.SetActive(true);

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
            rewardAdTimerText.text = timerText;
            rewardAdTimer = totalTime - 1;
            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Reduce the timer
            totalTime--;
        }

        rewardAdTimerText.text = "0:00";
        rewardAdTimer = 0;

        rewardDisplay.SetActive(false);
        adButton.SetActive(true);

        refineryController.SetProfitMultiplier(1);
        yield break;
    }

    public void LoadData(GameData data)
    {

        if (!data.finishedTutorial)
        {
            firstTimePlaying = true;
        }
        
        if (DataPersistenceManager.Instance.GetGameData().mineCount <= 1) {
            // No ads in tutorial
            disableAds = true;
            return;
        }
    }

    public void SaveData(ref GameData data) {
        if (disableAds) {
            return;
        }
    }

    private void FillEmptyAdSlots() {
        // Dont load new ad if an ad is showing (mainly used for iOS)
        if (adShowing) {
            return;
        }

        if (disableAds) {
            return;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd()) {
            LoadRewardedAd("Boost");
        }
        if (crateAd == null || !crateAd.CanShowAd()) {
            LoadRewardedAd("Crate");
        }
    }

}
