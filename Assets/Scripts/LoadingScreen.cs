using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    public Slider progressBar;

    public int loadedItems = 0;
    // See comment below to see why total items is this value, change in inspector
    public int totalItems;
    public int cloudSaveItems;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
        SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        LoadData() (10 total)
        AdDelegator, DailyChallengeDelegator, GarageDelegator, LeaderboardDelegator, MineRenderer, PlayerState, PlayerVehicleDelegation, RefineryController, SupplyCrateDelegator, UpgradePanelsDelegator
        
        Extras:
        CloudDelegator: Awake() (1 total) (initial load) || LoadGameDataFromCloud() (1 total) (async) || OnSignedIn() (1 total) when changing scenes but still logged in
        MineRender: AsyncLoadData() (1 total) runs asynchronously, may interfere with cloud loading screen
        
        TutorialManager: LoadData() (1 total), only in singleplayer
        NPCManager: LoadData() (1 total), only in co-op local

        Total as of Mar 22 2025: 13 || 12
        14 || 13 for coop local
        Last check: Mar 22 2025
    */

    private float rotationSpeed = 200f; // Speed of buffer rotation in degrees per second

    // Update is called once per frame
    void Update()
    {
        progressBar.value = loadedItems;
        progressBar.maxValue = totalItems;
        bufferCircle.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);

        if (loadedItems < totalItems) {
            return;
        }

        gameObject.SetActive(false);
    }

    public IEnumerator IncrementLoadedItems(GameObject name)
    {
        loadedItems++;
        //Debug.Log(loadedItems + ": " + name.name);
        yield break;
    }
}
