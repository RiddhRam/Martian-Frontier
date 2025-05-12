using System.Collections.Generic;
using UnityEngine;
using System;
using System.Numerics;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

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

    [SerializeField]
    private float timer = 0f;
    private readonly float interval = 90f; // Save time interval

    private bool notSinglePlayerScene = false;

    public static DataPersistenceManager instance {get; private set; }

    // If this is false, then don't save the game
    // Helps improve game data integrity
    //
    // Player might open game by accident then quickly close, which triggers an early save
    // The early save will use the default game data values rather than from their game data file
    private bool gameLoaded = false;

    private void Awake() {

        if (instance != null) {
            Debug.LogError("Found more than one data persistence manager");
        }
        instance = this;

        // Don't encrypt when using the editor, for debugging purposes
        if (Application.isEditor) {
            //useEncryption = false;
        }

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
        }

    }

    private void Start() {
        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();

        // Load saved data from file from a file handler
        CompareGameData(dataHandler.Load());

        if (adConsent) {
            adConsent.UpdatePlayerStatus(this.gameData.finishedTutorial);
            return;
        }
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

    public async void ResetBetaPlayer() {
        // Reset game
        NewGame();

        // Recognize as beta player
        this.gameData.bp = 2;

        DirectlyWriteSave();

        // Make sure player isn't signing in
        while (cloudDelegator.doingSigninProcess)
            await Task.Yield();

        // Make sure cloud save is overwritten too
        await cloudDelegator.SaveGameDataToCloud();

        // Still make sure they aren't signing in just in case
        while (cloudDelegator.doingSigninProcess)
            await Task.Yield();

        cloudDelegator.TempSignOut();

        // Restart game
        SceneManager.LoadScene("Loading Screen");
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

        gameLoaded = true;
    }

    public void SaveGame(bool async = true) {
        timer = 0;

        if (dataPersistenceObjects == null || !gameLoaded) {
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
        
        if (async) {
            // Save the data as a file
            _ = dataHandler.SaveAsync(gameData);
        } else {
            dataHandler.Save(gameData);
        }

        // Make sure game data is valid
        if (!dataHandler.gameDataValid) {
            return;
        }

        try {
            if (cloudDelegator) {
                _ = cloudDelegator.SaveGameDataToCloud();
            }
        } catch (Exception ex) {
            Debug.Log("Error when saving to cloud: " + ex);
        }
    }

    public void DirectlyWriteSave() {
        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit() {
        SaveGame(false);
    }

    private void OnApplicationPause() {
        SaveGame(false);
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

        DirectlyWriteSave();
        
        SceneManager.LoadScene("Loading Screen");
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
        
        // If player is from the beta and has not collected their reward, don't load data from the cloud

        // Keep the game save with the older stuff. STUBBY was from the beta, so the player will receive a reward
        // The current save has to go first for this one, otherwise it gets stuck in a loop
        // If Beta key == 200, then the player already was recognized so just reward them with the current save
        // If bp == 2, then the player was previously recognized as a beta player
        if (this.gameData.vehiclesOwned.Contains("STUBBY") || this.gameData.bp == 2 || PlayerPrefs.GetInt("Beta") == 200) {
            this.gameData.bp = 2;
            return false;
        }
        if (gameData.vehiclesOwned.Contains("STUBBY") || gameData.bp == 2) {
            this.gameData = gameData;
            return true;
        }

        // Keep one with most XP 
        if (BigInteger.Parse(gameData.userXP) > BigInteger.Parse(this.gameData.userXP)) {
            this.gameData = gameData;
            return true;
        }
        if (BigInteger.Parse(gameData.userXP) < BigInteger.Parse(this.gameData.userXP)) {
            return false;
        }

        // Keep one with most cash if xp is equal
        if (BigInteger.Parse(gameData.userCash) > BigInteger.Parse(this.gameData.userCash)) {
            this.gameData = gameData;
            return true;
        }
        if (BigInteger.Parse(gameData.userCash) < BigInteger.Parse(this.gameData.userCash)) {
            return false;
        }

        // Keep one with most gems if others all equal
        if (BigInteger.Parse(gameData.userGems) > BigInteger.Parse(this.gameData.userGems)) {
            this.gameData = gameData;
            return true;
        }

        return false;
    }

    public GameData GetGameData() {
        return this.gameData;
    }

    public ref GameData GetGameDataRef() {
        return ref this.gameData;
    }

}