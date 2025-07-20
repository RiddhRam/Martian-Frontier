using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    private static LoadingScreen _instance;
    public static LoadingScreen Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindObjectOfType<LoadingScreen>();
            }
            return _instance;
        }
    }

    public GameObject bufferCircle;
    public Slider progressBar;

    public GameObject loadingScreen;
    public GameObject ui;

    public int loadedItems = 0;
    // See comment below to see why total items is this value, change in inspector
    public int totalItems;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
        SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        LoadData() (15 total)
        AdDelegator, DailyChallengeDelegator, VehicleUpgradeBayManager, LeaderboardDelegator, MineRenderer, MiniGameChooser, NPCManager, OfflineRewardsManager, PlayerState, RefineryController, RefineryUpgradeBay, SupplyCrateDelegator, TutorialManager, UpgradePanelsDelegator, VehicleUpgradeBayManager
        
        Extras:
        (3 total)
        CloudDelegator: Awake() (1 total) (initial load) || LoadGameDataFromCloud() (1 total) (async) || OnSignedIn() (1 total) when changing scenes but still logged in
        MineRenderer: AsyncLoadData() (1 total) runs asynchronously
        NPCManager: PrepareGame() (1 total) runs asynchronously

        Total as of Jun 20 2025: 18
        Last check: Jun 20 2025
    */

    private float rotationSpeed = 200f; // Speed of buffer rotation in degrees per second

    // Update is called once per frame
    void Update()
    {
        if (!ui.activeSelf)
        {
            ui.SetActive(true);
        }
        
        progressBar.value = loadedItems;
        progressBar.maxValue = totalItems;
        bufferCircle.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);

        if (loadedItems < totalItems)
        {
            return;
        }

        if (loadingScreen.activeSelf)
        {
            loadingScreen.SetActive(false);
        }
        
    }

    public IEnumerator IncrementLoadedItems(GameObject name)
    {
        loadedItems++;
        //Debug.Log(loadedItems + ": " + name.name);
        yield break;
    }
}
