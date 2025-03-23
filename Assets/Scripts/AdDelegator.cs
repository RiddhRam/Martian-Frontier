using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Mediation.UnityAds.Api;
using UnityEngine.SceneManagement;

public class AdDelegator : MonoBehaviour, IDataPersistence
{
    private string _adUnitId = "unused";
    public GameObject adButton;
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

    public GameObject leaderboardTabButtons;
    public GameObject leaderboardCashPanel;
    public GameObject leaderboardVehiclesPanel;
    private bool cashPanelWasOpen = true;

    private RewardedAd rewardedAd;
    private RewardedAd crateAd;
    private int timer = 0;
    private bool internetReachable = false;
    // This needs to be seperate because user can swap vehicle while boost active
    public float originalSpeed;
    public bool speedBoostActive;

    private int rewardAdTimer = 0;

    public DataPersistenceManager dataPersistenceManager;
    public AnalyticsDelegator analyticsDelegator;
    public CloudDelegator cloudDelegator;
    public PlayerState playerState;
    public RefineryController refineryController;
    public SupplyCrateDelegator supplyCrateDelegator;
    public UpgradePanelsDelegator upgradePanelsDelegator;

    private bool adsInitialized = false;
    private string adPermissionGiven;
    // After 30 seconds of user watching an ad, request a new one.
    // Once user watches an ad, ad boosts are free for the next 30 seconds
    DateTime lastAdShown;
    private bool cloudLoading = false;
    private bool displayStatus = true;
    private bool firstTimePlaying = false;
    private bool disableAds = false;

    // Search this to find all lines to comment/uncomment for ads: ADMOB DISABLE
    void Awake() {
        
        #if UNITY_ANDROID
        // Reset everything
        //PlayerPrefs.SetString("APG", "");
        //ConsentInformation.Reset();
        #endif
        
        adPermissionGiven = PlayerPrefs.GetString("APG");

        if (adPermissionGiven == "Allowed") {
            UnityAds.SetConsentMetaData("gdpr.consent", true);
            UnityAds.SetConsentMetaData("privacy.consent", true);
        } else if (adPermissionGiven == "Not Allowed") {
            UnityAds.SetConsentMetaData("gdpr.consent", false);
            UnityAds.SetConsentMetaData("privacy.consent", false);
        }

        SetAdUnitId();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (adPermissionGiven == "Not Allowed") {
            return;
        }
        // Need this so rewarded ads actually reward in the real app
        MobileAds.RaiseAdEventsOnUnityMainThread = true; 
        // ADMOB DISABLE
        MobileAds.Initialize((InitializationStatus initstatus) =>
        {
            adsInitialized = true;
            FillEmptyAdSlots();
        });
    }

    // Called from loading screen
    public void GetAdConsent() {
        #if UNITY_ANDROID
        try {
            // On iOS this is done in the Ad Consent screen in AdConsent.cs

            // Only uncomment when debugging user consent settings
            /*var debugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA,
                TestDeviceHashedIds =
                new List<string>
                {
                    "93001fda-7fff-44e5-80b1-b086356f0b51"
                }
            };

            // Create a ConsentRequestParameters object.
            ConsentRequestParameters request = new ConsentRequestParameters
            {
                ConsentDebugSettings = debugSettings,
            };*/

            // Create a ConsentRequestParameters object.
            ConsentRequestParameters request = new ConsentRequestParameters();

            // TODO: Fix on android so that we can show personalized android ads
            // Check the current consent information status.
            //ConsentInformation.Update(request, OnConsentInfoUpdated);
        } catch {
            //Debug.Log("CONSENT EXCEPTION: " + ex.Message);
        }
        #endif
    }

    #if UNITY_ANDROID
    void OnConsentInfoUpdated(FormError consentError)
    {
        if (consentError != null)
        {
            // Handle the error.
            Debug.LogError(consentError);
            return;
        }

        // If the error is null, the consent information state was updated.
        // You are now ready to check if a form is available.
        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
        {
            if (formError != null)
            {
                // Consent gathering failed.
                Debug.LogError(consentError);
                return;
            }

            // Consent has been gathered.
            if (ConsentInformation.CanRequestAds())
            {
                UnityAds.SetConsentMetaData("gdpr.consent", true);
                UnityAds.SetConsentMetaData("privacy.consent", true);

                PlayerPrefs.SetString("APG", "Allowed");
            } else {
                UnityAds.SetConsentMetaData("gdpr.consent", false);
                UnityAds.SetConsentMetaData("privacy.consent", false);

                PlayerPrefs.SetString("APG", "Not Allowed");
            }
        });
    }
    #endif

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

        if (firstTimePlaying) {
            return;
        }
        
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
        } else if (crateAd != null && type == "Crate") {
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
                } else if (type == "Crate") {
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
        // If user watched an ad in the last 30 seconds or first time playing
        if (firstTimePlaying || lastAdShown >= DateTime.Now.AddSeconds(-90)) {
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

    public void ShowCrateRewardedAd() {
        if (disableAds) {
            return;
        }

        try {
            analyticsDelegator.AdWatchAttempt("Crate");
        } catch {
        }

        if (firstTimePlaying) {
            CrateRewardSuccess();
            return;
        }

        // ADMOB DISABLE
        if (crateAd != null && crateAd.CanShowAd())
        {   
            crateAd.Show((Reward reward) =>
            {
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });

            // Reward user
            CrateRewardSuccess();
            
            // Listen to user events during ad
            RegisterEventHandlers(crateAd);
            return;
        } else {
            // CustomAdScreen if no ad ready
            StartCoroutine(UseCustomAdScreen(() => CrateRewardSuccess()));
        }
        
    }

    private void CrateRewardSuccess() {
        supplyCrateDelegator.DoubleRewardsActivated();
        dataPersistenceManager.SaveGame();
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
            leaderboardCashPanel.SetActive(cashPanelWasOpen);
            leaderboardVehiclesPanel.SetActive(!cashPanelWasOpen);
            leaderboardTabButtons.SetActive(true);
            leaderboardNoWifi.SetActive(false);
            

            if (!disableAds) {

                adButton.SetActive(true);

                if (!supplyCrateDelegator.adWatchedAlready) {
                    doubleCrateRewardButton.SetActive(true);
                    crateRewardNoWifi.SetActive(false);
                }

                teamNoWifi.SetActive(false);
            }

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
        cashPanelWasOpen = leaderboardCashPanel.activeSelf;
        leaderboardCashPanel.SetActive(false);
        leaderboardVehiclesPanel.SetActive(false);
        leaderboardTabButtons.SetActive(false);
        leaderboardNoWifi.SetActive(true);
        
        if (!disableAds) {
            crateRewardNoWifi.SetActive(true);
            doubleCrateRewardButton.SetActive(false);
            adButton.SetActive(false);


            teamNoWifi.SetActive(true);
        }

        displayStatus = false;
    }

    private void RewardBoost(int? totalTime = 240) {

        PlayerMovement playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        originalSpeed = playerMovement.GetSpeed();
        playerMovement.SetSpeed(originalSpeed * 1.5f);

        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        mineRenderer.SetVisionRadius(upgradePanelsDelegator.visionRadius + upgradePanelsDelegator.visionBoost);
        visionText.text =  "+" + upgradePanelsDelegator.visionBoost.ToString();

        refineryController.SetProfitMultiplier(upgradePanelsDelegator.refineryProfitMultiplier * upgradePanelsDelegator.refineryProfitMultiplierBoost);
        profitText.text = upgradePanelsDelegator.refineryProfitMultiplierBoost.ToString() + "X";

        StartCoroutine(StartRewardCountdown((int) totalTime));

        LogAnalytics("Profit");
    }

    private void LogAnalytics(string analyticToLog) {
        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.AdWatchAttempt(analyticToLog);
    }

    private IEnumerator StartRewardCountdown(int totalTime) {

        speedBoostActive = true;

        adButton.SetActive(false);
        visionText.transform.parent.parent.gameObject.SetActive(true);

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

        adButton.SetActive(true);
        visionText.transform.parent.parent.gameObject.SetActive(false);
        
        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        mineRenderer.SetVisionRadius(upgradePanelsDelegator.visionRadius);

        refineryController.SetProfitMultiplier(upgradePanelsDelegator.refineryProfitMultiplier);

        speedBoostActive = false;
        PlayerMovement playerMovement = GameObject.Find("Player Vehicle").GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(originalSpeed);
        yield break;
    }

    public void LoadData(GameData data) {

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            disableAds = true;
            return;
        }

        this.rewardAdTimer = data.rewardAdTimer;

        if (rewardAdTimer > 0) {
            // Reward
            RewardBoost(rewardAdTimer);
        }

        if (!data.finishedTutorial) {
            firstTimePlaying = true;
        }
    }

    public void SaveData(ref GameData data) {
        if (disableAds) {
            return;
        }
        
        data.rewardAdTimer= this.rewardAdTimer;
    }

    private void FillEmptyAdSlots() {
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
