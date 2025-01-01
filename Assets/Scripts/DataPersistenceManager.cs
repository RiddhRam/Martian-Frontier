using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataPersistenceManager : MonoBehaviour
{

    [Header("File Storage Config")]
    [SerializeField] 
    private string fileName;
    private bool useEncryption = true;

    private GameData gameData;
    private List<IDataPersistence> dataPersistenceObjects;
    private FileDataHandler dataHandler;
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
        // Load saved data from file from a file handler
        this.gameData = dataHandler.Load();

        // If no file, create a new game
        if (this.gameData == null) {
            Debug.Log("No game data to load, creating new game");
            NewGame();
        }

        // initialize values to scripts that need it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            dataPersistenceObj.LoadData(gameData);
        }
    }

    public void SaveGame() {
        timer = 0;
        if (dataPersistenceObjects == null) {
            return;
        }
        // Get data from scripts to save
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            dataPersistenceObj.SaveData(ref gameData);
        }

        // Save the data as a file
        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit() {
        SaveGame();
    }

    private void OnApplicationPause() {
        SaveGame();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects() {
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
    }
}
