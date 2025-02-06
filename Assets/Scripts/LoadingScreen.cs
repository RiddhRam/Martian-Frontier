using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    public Slider progressBar;

    public int loadedItems = 0;
    // See comment below to see why total items is this value
    public int totalItems = 11;
    public int cloudSaveItems = 7;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
       SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        AdDelegator: LoadRewardedAd() x 3, LoadData(), Start() (5 total) || LoadData() (1 Total)
        CloudDelegator: Awake() (1 total) || LoadGameDataFromCloud() (1 total)
        DailyChallengeDelegator(): LoadData() (1 total)
        MineRenderer: LoadData() (1 total)
        PlayerState: LoadData() (1 total)
        PlayerVehicleDelegation: LoadData() (1 total)
        RefineryController: LoadData() (1 total)
        Total as of Feb 6 2025: 11 || 7
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

        gameObject.SetActive(false);
    }

    public IEnumerator IncrementLoadedItems()
    {
        //int randomDelay = Random.Range(1, 7);
        // Simulate a loading duration
        //yield return new WaitForSeconds(randomDelay);
        loadedItems++;
        yield break;
    }
}
