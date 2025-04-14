using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using UnityEngine;

public class AnalyticsDelegator : MonoBehaviour
{
    public static AnalyticsDelegator Instance;
    private bool isInitialized = false;
    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this);
        } else {
            Instance = this;
        }
    }

    async void Start() {
        // Disable analytics in editor and development
        if (Debug.isDebugBuild) {
            return;
        }

        // Wait for initialization in Cloud Delegator
        await Task.Delay(500);

        AnalyticsService.Instance.StartDataCollection();
        isInitialized = true;
    }

    public void TestEvent(string message) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Test_Event") {
            {"Test_Message", message}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void InitializeMine(int previousHighestRow) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Initialize_Mine") {
            {"Previous_Highest_Row", previousHighestRow}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void AdWatchAttempt(string reward) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Ad_Watch_Attempt") {
            {"Reward", reward}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void OpenUIPanel(string name) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_UI_Panel") {
            {"Panel", name}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void ShowError(string error) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Show_Error") {
            {"Error", error}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void SelectVehicle(string vehicleName, string vehicleType, int tier) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Select_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Vehicle_Type", vehicleType},
            {"Vehicle_Tier", tier}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void PurchaseVehicle(string vehicleName, string vehicleType, int tier) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Purchase_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Vehicle_Type", vehicleType},
            {"Vehicle_Tier", tier}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void RefineryUpgrade(string upgradeName, int upgradeLevel) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Refinery_Upgrade") {
            {"Upgrade_Name", upgradeName},
            {"Upgrade_Level", upgradeLevel}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void DropOffOres(string vehicleName, int oreCount, float cashCount) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Drop_Off_Ores") {
            {"Vehicle_Name", vehicleName},
            {"Ore_Count", oreCount},
            {"Cash_Count", cashCount}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void Rebirth(int level) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Rebirth") {
            {"Rebirth_Level", level},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void OpenTutorialUIPanel(string name) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_Tutorial_UI_Panel") {
            {"Panel", name}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void SelectLanguage(string language) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Select_Language") {
            {"Language", language}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void StartTutorial() {
        // Do this asynchronously that way it has time to initialize
        StartCoroutine(StartTutorialAsync());
    }

    private IEnumerator StartTutorialAsync() {
        yield return new WaitUntil(() => isInitialized);

        CustomEvent myEvent = new CustomEvent("Start_Tutorial");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void FinishTutorial() {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Finish_Tutorial");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void ContinuedAfterTutorial() {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Continued_After_Tutorial");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void EnjoyingGame() {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Enjoying_Game");
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void NotEnjoyingGame(string reason) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Not_Enjoying_Game") {
            {"HateReason", reason}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void StartSuperChallenge(int selectedChallengeIndex) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Start_Super_Challenge") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void CompleteSuperChallenge(int selectedChallengeIndex, int timeLeft) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Complete_Super_Challenge") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
            {"Time_Left", timeLeft},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void CollectChallengeReward(int selectedChallengeIndex) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Collect_Challenge_Reward") {
            {"Selected_Challenge_Index", selectedChallengeIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void PurchaseCashWithGems(float amount) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Purchase_Cash_With_Gems") {
            {"Amount", amount},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void UpgradeVehicle(string vehicleName, int upgradeLevel) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Upgrade_Vehicle") {
            {"Vehicle_Name", vehicleName},
            {"Upgrade_Level", upgradeLevel}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void OpenCrate(bool openAll, int amount) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Open_Crate") {
            {"Open_All", openAll},
            {"Amount", amount}
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void IAPPurchase(string type) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("IAP_Purchase") {
            {"Type", type},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void EquipPower(string powerName) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Equip_Power") {
            {"Power", powerName},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void UsePower(string powerName) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Use_Power") {
            {"Power", powerName},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void SwitchSession(string sessionType) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Switch_Session") {
            {"Type", sessionType},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void TutorialStep(int tutorialIndex) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Tutorial_Step") {
            {"Index", tutorialIndex},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }
}
