using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    public GameObject progressBar;

    private int loadedItems = 0;
    // See comment below to see why total items is this value
    private int totalItems = 9;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
       SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        AdDelegator: Initialize Mobile ads SDK, LoadRewardedAd() x 3, LoadData()  (5 total)
        MineRenderer: LoadData() (1 total)
        PlayerState: LoadData() (1 total)
        PlayerVehicleDelegation: LoadData() (1 total)
        RefineryController: LoadData() (1 total)
        Total as of Jan 11 2025: 9
        Last check: Jan 11 2025
    */

    private float rotationSpeed = 200f; // Speed of buffer rotation in degrees per second

    void Start()
    {
        progressBar.GetComponent<Slider>().maxValue = totalItems;
        progressBar.GetComponent<Slider>().value = loadedItems;
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().LoadGame();
    }

    // Update is called once per frame
    void Update()
    {
        progressBar.GetComponent<Slider>().value = loadedItems;
        bufferCircle.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);

        if (loadedItems < totalItems) {
            return;
        }

        Destroy(gameObject);
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
