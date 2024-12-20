using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject bufferCircle;
    public GameObject progressBar;

    private int loadedItems = 0;
    // See comment below to see why total items is this value
    private int totalItems = 7;

    /* Scripts with IDataPersistence have at least 1 thing to be loaded
       SOME CONTAIN DUPLICATES IN CASE OF IF STATEMENTS OR ERROR CATCHING

        AdDelegator: Initialize Mobile ads SDK, LoadRewardedAd(), LoadData()
        MineRenderer: LoadData()
        PlayerState: LoadData()
        PlayerVehicleDelegation: LoadData()
        RefineryController: LoadData()
        Total as of Dec 20 2024: 7
        Last check: Dec 20 2024
    */

    private float rotationSpeed = 200f; // Speed of buffer rotation in degrees per second

    void Start()
    {
        progressBar.GetComponent<Slider>().maxValue = totalItems;
        progressBar.GetComponent<Slider>().value = loadedItems;
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
