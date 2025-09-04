using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Analytics;
using UnityEngine.SceneManagement;
using Unity.Services.Core;

public class AnalyticsDelegator : MonoBehaviour
{
    private static AnalyticsDelegator _instance;
    public static AnalyticsDelegator Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<AnalyticsDelegator>();
            }
            return _instance;
        }
    }
    public bool isInitialized = false;

    private float sceneStartRealtime;
    private string currentScene;

    async void Start()
    {
        // Disable analytics in editor and development
        if (Debug.isDebugBuild)
        {
            return;
        }

        currentScene = SceneManager.GetActiveScene().name;
        sceneStartRealtime = Time.realtimeSinceStartup;

        await UnityServices.InitializeAsync();

        while (CloudDelegator.Instance.auth == null)
        {
            await Task.Delay(100);
        }

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
                return;
            }
        });

        AnalyticsService.Instance.StartDataCollection();

        isInitialized = true;
    }

    public void LogSceneDuration(string nextScene)
    {
        if (!isInitialized) return;

        var ev = new CustomEvent("Scene_Time")
        {
            { "Scene", currentScene },
            { "Duration", Time.realtimeSinceStartup - sceneStartRealtime },
            { "Next_Scene", nextScene }
        };
        AnalyticsService.Instance.RecordEvent(ev);
        AnalyticsService.Instance.Flush();

        //if (CloudDelegator.Instance.auth != null) 

        FirebaseAnalytics.LogEvent("Scene_Time",
            new Parameter("Scene", currentScene),
            new Parameter("Duration", Time.realtimeSinceStartup - sceneStartRealtime));
        new Parameter("Next_Scene", nextScene);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            EndCurrentSceneTimer();   // going to background
        else
            ResumeCurrentSceneTimer(); // returning to foreground
    }

    void OnApplicationQuit()
    {
        EndCurrentSceneTimer();       // one last flush before process death
    }

    private void EndCurrentSceneTimer()
    {
        if (currentScene == null) return;

        LogSceneDuration("");

        currentScene = null;  // so we don’t double‑log
    }

    private void ResumeCurrentSceneTimer()
    {
        if (currentScene != null) return;       // already running

        currentScene = SceneManager.GetActiveScene().name;
        sceneStartRealtime = Time.realtimeSinceStartup;
    }

    public void TestEvent(string message)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Test_Event") {
            {"Test_Message", message}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Test_Event", new Parameter("Test_Message", message));
    }

    public void InitializeMine(int previousHighestRow)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Initialize_Mine") {
            {"Previous_Highest_Row", previousHighestRow}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Initialize_Mine", new Parameter("Previous_Highest_Row", previousHighestRow));
    }

    public void AdWatchAttempt(string reward, int rebirthLevel)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Ad_Watch_Attempt") {
            {"Reward", reward},
            { "Rebirth_Level", rebirthLevel},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Ad_Watch_Attempt",
            new Parameter("Reward", reward),
            new Parameter("Rebirth_Level", rebirthLevel));
    }

    public void OpenUIPanel(string panelName)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_UI_Panel") {
            {"Panel", panelName}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Open_UI_Panel", new Parameter("Panel", panelName));
    }

    public void ShowError(string error)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Show_Error") {
            {"Error", error}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Show_Error", new Parameter("Error", error));
    }

    public void SelectVehicle(string vehicleName, string vehicleType, int tier)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Select_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Vehicle_Type", vehicleType},
            {"Vehicle_Tier", tier}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Select_Vehicle",
            new Parameter("Vehicle_Name", vehicleName),
            new Parameter("Vehicle_Type", vehicleType),
            new Parameter("Vehicle_Tier", tier));
    }

    public void PurchaseVehicle(string vehicleName, string vehicleType, int tier)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Purchase_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Vehicle_Type", vehicleType},
            {"Vehicle_Tier", tier}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Purchase_Vehicle",
            new Parameter("Vehicle_Name", vehicleName),
            new Parameter("Vehicle_Type", vehicleType),
            new Parameter("Vehicle_Tier", tier));
    }

    public void RefineryUpgrade(string upgradeName, int upgradeLevel)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Refinery_Upgrade") {
            {"Upgrade_Name", upgradeName},
            {"Upgrade_Level", upgradeLevel}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Refinery_Upgrade",
            new Parameter("Upgrade_Name", upgradeName),
            new Parameter("Upgrade_Level", upgradeLevel));
    }

    public void DropOffOres(string vehicleName, int oreCount, float cashCount)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Drop_Off_Ores") {
            {"Vehicle_Name", vehicleName},
            {"Ore_Count", oreCount},
            {"Cash_Count", cashCount}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Drop_Off_Ores",
            new Parameter("Vehicle_Name", vehicleName),
            new Parameter("Ore_Count", oreCount),
            new Parameter("Cash_Count", cashCount));
    }

    public void OpenTutorialUIPanel(string panelName)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_Tutorial_UI_Panel") {
            {"Panel", panelName}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Open_Tutorial_UI_Panel", new Parameter("Panel", panelName));
    }

    public void SelectLanguage(string language)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Select_Language") {
            {"Language", language}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Select_Language", new Parameter("Language", language));
    }

    public void StartTutorial()
    {
        // Do this asynchronously that way it has time to initialize
        StartCoroutine(StartTutorialAsync());
    }

    private IEnumerator StartTutorialAsync()
    {
        yield return new WaitUntil(() => isInitialized);

        string cohort = PlayerPrefs.GetString("Cohort", "No Cohort");

        CustomEvent myEvent = new CustomEvent("Start_Tutorial") {
            {"Cohort", cohort},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Start_Tutorial",
            new Parameter("Cohort", cohort));
    }

    public void FinishTutorial()
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Finish_Tutorial");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Finish_Tutorial");
    }

    public void ContinuedAfterTutorial()
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Continued_After_Tutorial");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Continued_After_Tutorial");
    }

    public void EnjoyingGame()
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Enjoying_Game");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Enjoying_Game");
    }

    public void NotEnjoyingGame(string reason)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Not_Enjoying_Game") {
            {"HateReason", reason}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Not_Enjoying_Game", new Parameter("HateReason", reason));
    }

    public void StartSuperChallenge(int selectedChallengeIndex)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Start_Super_Challenge") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Start_Super_Challenge", new Parameter("Selected_Challenge_Index", selectedChallengeIndex));
    }

    public void CompleteSuperChallenge(int selectedChallengeIndex, int timeLeft)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Complete_Super_Challenge") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
            {"Time_Left", timeLeft},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Complete_Super_Challenge",
            new Parameter("Selected_Challenge_Index", selectedChallengeIndex),
            new Parameter("Time_Left", timeLeft));
    }

    public void CollectChallengeReward(int selectedChallengeIndex)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Collect_Challenge_Reward") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Collect_Challenge_Reward", new Parameter("Selected_Challenge_Index", selectedChallengeIndex));
    }

    public void PurchaseCashWithGems(float amount)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Purchase_Cash_With_Gems") {
            {"Amount", amount},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Purchase_Cash_With_Gems", new Parameter("Amount", amount));
    }

    public void UpgradeVehicle(string vehicleName, int upgradeLevel)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Upgrade_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Upgrade_Level", upgradeLevel}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Upgrade_Vehicle",
            new Parameter("Vehicle_Name", vehicleName),
            new Parameter("Upgrade_Level", upgradeLevel));
    }

    public void OpenCrate(bool openAll, int amount)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_Crate") {
            {"Open_All", openAll},
            {"Amount", amount}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Open_Crate",
            new Parameter("Open_All", openAll.ToString()),
            new Parameter("Amount", amount));
    }

    public void IAPPurchase(string type)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("IAP_Purchase") {
            {"Type", type},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("IAP_Purchase", new Parameter("Type", type));
    }

    public void EquipPower(string powerName)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Equip_Power") {
            {"Power", powerName},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Equip_Power", new Parameter("Power", powerName));
    }

    public void UsePower(string powerName)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Use_Power") {
            {"Power", powerName},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Use_Power", new Parameter("Power", powerName));
    }

    public void SwitchSession(string sessionType)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Switch_Session") {
            {"Type", sessionType},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Switch_Session", new Parameter("Type", sessionType));
    }

    public void TutorialStep(int tutorialIndex)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Tutorial_Step") {
            {"Index", tutorialIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Tutorial_Step", new Parameter("Index", tutorialIndex));
    }

    public void PurchaseCreditsWithGems(float amount)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Purchase_Credits_With_Gems") {
            {"Amount", amount},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Purchase_Credits_With_Gems", new Parameter("Amount", amount));
    }

    public void TechLabUpgrade(string upgradeName)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Tech_Lab_Upgrade") {
            {"Upgrade_Name", upgradeName},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Tech_Lab_Upgrade", new Parameter("Upgrade_Name", upgradeName));
    }

    public void VehicleUpgrade(string type, int rebirthLevel)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Vehicle_Upgrade") {
            {"Type", type},
            //{"Upgrade_Level", upgradeLevel},
            {"Rebirth_Level", rebirthLevel},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Vehicle_Upgrade",
            new Parameter("Type", type),
            //new Parameter("Upgrade_Level", upgradeLevel),
            new Parameter("Rebirth_Level", rebirthLevel));
    }

    public void OreUpgrade(string type, int upgradeLevel, int rebirthLevel)
    {
        if (!isInitialized)
        {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Ore_Upgrade") {
            {"Type", type},
            {"Upgrade_Level", upgradeLevel},
            {"Rebirth_Level", rebirthLevel},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Ore_Upgrade",
            new Parameter("Type", type),
            new Parameter("Upgrade_Level", upgradeLevel),
            new Parameter("Rebirth_Level", rebirthLevel));
    }
    
    public void Rebirth(int rebirthLevel) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Rebirth") {
            {"Rebirth_Level", rebirthLevel},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
        FirebaseAnalytics.LogEvent("Rebirth",
            new Parameter("Rebirth_Level", rebirthLevel));
    }
}
