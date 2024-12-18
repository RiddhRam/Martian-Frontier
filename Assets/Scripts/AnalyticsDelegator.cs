using Unity.Services.Analytics;
using Unity.Services.Core;
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
        await UnityServices.InitializeAsync();
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

    public void LevelUp(int level) {
        if (!isInitialized) {
            return;
        }
        CustomEvent myEvent = new CustomEvent("Level_Up") {
            {"userLevel", level},
        };
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

}
