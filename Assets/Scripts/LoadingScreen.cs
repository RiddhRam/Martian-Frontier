using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    public Slider progressBar;

    public int loadedItems = 0;
    // See comment below to see why total items is this value
    public int totalItems = 10;
    public int cloudSaveItems = 9;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
        SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        LoadData() (7 total)
        AdDelegator, DailyChallengeDelegator, MineRenderer, PlayerState, PlayerVehicleDelegation, RefineryController, TutorialManager
        
        Extras:
        AdDelegator: LoadRewardedAd() (1 total) (initial load) runs asynchronously, may interfere with cloud loading screen
        CloudDelegator: Awake() (1 total) (initial load) || LoadGameDataFromCloud() (1 total) (async)
        MineRender: AsyncLoadData() (1 total) runs asynchronously, may interfere with cloud loading screen


        Total as of Feb 6 2025: 10 || 9
        Last check: Feb 6 2025
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

        try {
            GameObject.Find("Tutorial").GetComponent<TutorialManager>().loadingScreen = null;
        } catch {

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
