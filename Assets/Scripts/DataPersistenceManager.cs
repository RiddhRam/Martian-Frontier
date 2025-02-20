using System.Collections.Generic;
using UnityEngine;
using System;
using System.Numerics;

public class DataPersistenceManager : MonoBehaviour
{

    [Header("File Storage Config")]
    public string fileName;
    public CloudDelegator cloudDelegator;
    private bool useEncryption = true;

    private GameData gameData = new();
    private List<IDataPersistence> dataPersistenceObjects;
    private FileDataHandler dataHandler;
    public AdConsent adConsent;
    private float timer = 0f;
    private float interval = 90f; // Save time interval

    public static DataPersistenceManager instance {get; private set; }

    private void Awake() {

        if (instance != null) {
            Debug.LogError("Found more than one data persistence manager");
        }
        instance = this;

        // Don't encrypt when using the editor, go debugging purposes
        if (Application.isEditor) {
            useEncryption = false;
        }

    }

    private void Start() {
        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();

        // Load saved data from file from a file handler
        CompareGameData(dataHandler.Load());
        #if UNITY_IPHONE || UNITY_IOS
        if (adConsent) {
            adConsent.UpdatePlayerStatus(this.gameData.finishedTutorial);
            return;
        }
        #endif
        LoadGame();
    }

    void Update() {
        timer += Time.deltaTime; // Increment the timer by the time passed since the last frame

        if (timer >= interval) // Check if the timer has reached the interval
        {
            SaveGame();
        }
    }

    public void NewGame() {
        this.gameData = new GameData();
    }

    public void LoadGame() {
        // If no file, create a new game
        if (this.gameData == null) {
            Debug.Log("No game data to load, creating new game");
            NewGame();
        }
        
        // initialize values to scripts that need it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            try {
                dataPersistenceObj.LoadData(gameData);
            } catch (Exception error) {
                Debug.Log(error);
            }

            try {
                StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems(gameObject));
            } catch {
            }
        }
    }

    public void SaveGame() {
        timer = 0;
        if (dataPersistenceObjects == null) {
            return;
        }

        // Get data from scripts to save
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            if (dataPersistenceObj.ToString() == "null") {
                continue;
            }
            
            try {
                dataPersistenceObj.SaveData(ref gameData);
            } catch (Exception ex) {
                Debug.Log(ex);
            }
        }
        
        // Save the data as a file
        _ = dataHandler.Save(gameData);

        try {
            if (cloudDelegator) {
                cloudDelegator.SaveGameDataToCloud();
            }
        } catch (Exception ex) {
            Debug.Log("Error when saving to cloud: " + ex);
        }
    }

    private void OnApplicationQuit() {
        SaveGame();
    }

    private void OnApplicationPause() {
        SaveGame();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects() {
        List<IDataPersistence> dataPersistenceObjects = new();

        // Find all root objects in the scene
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // Loop through all root objects and find inactive objects in the hierarchy
        foreach (GameObject rootObject in rootObjects) {
            FindDataPersistenceInHierarchy(rootObject, dataPersistenceObjects);
        }

        return dataPersistenceObjects;
    }

    private void FindDataPersistenceInHierarchy(GameObject obj, List<IDataPersistence> dataPersistenceObjects) {
        // Check for IDataPersistence component in this object
        IDataPersistence dataPersistence = obj.GetComponent<IDataPersistence>();
        if (dataPersistence != null) {
            dataPersistenceObjects.Add(dataPersistence);
        }

        // Recurse through all children, including inactive ones
        foreach (Transform child in obj.transform) {
            FindDataPersistenceInHierarchy(child.gameObject, dataPersistenceObjects);
        }
    }

    public void ResetEntireGame() {
        this.gameData = new GameData
        {
            finishedTutorial = true
        };

        // initialize values to scripts that need it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            try {
                dataPersistenceObj.LoadData(this.gameData);
            } catch (Exception error) {
                Debug.Log(error);
            }
        }
    }   

    // For web saving only
    public string CreateJson() {
        return dataHandler.CreateJson(gameData, false);
    }

    public GameData ParseJson(string webData) {
        return dataHandler.ParseJson(webData, false);
    }

    // Used here as well
    public bool CompareGameData(GameData gameData) {
        // true = use new save (cloud save or something else)
        // false = use current save
        
        // If cloud save has higher rebirth use the cloud save, else use local save
        if (gameData.rebirthProfitMultiplier > this.gameData.rebirthProfitMultiplier) {
            this.gameData = gameData;
            return true;
        }
        // If less than, return, other wise, they are equal so we look for a tie breaker
        if (gameData.rebirthProfitMultiplier < this.gameData.rebirthProfitMultiplier) {
            return false;
        }

        // Keep one with most cash, if rebirth is equal
        if (BigInteger.Parse(gameData.userCash) > BigInteger.Parse(this.gameData.userCash)) {
            this.gameData = gameData;
            return true;
        }
        if (BigInteger.Parse(gameData.userCash) < BigInteger.Parse(this.gameData.userCash)) {
            return false;
        }

        // Keep one with most XP if cash and rebirth is equal
        if (BigInteger.Parse(gameData.userXP) > BigInteger.Parse(this.gameData.userXP)) {
            this.gameData = gameData;
            return true;
        }
        return false;
    }

    public GameData GetGameData() {
        return this.gameData;
    }

}