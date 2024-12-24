using UnityEngine;
using System;
using System.IO;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";
    private bool useEncryption = false;
    private readonly string encryptonCodeWord = "RydStud10s!";

    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption) {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName; 
        this.useEncryption = useEncryption;
    }

    public GameData Load() {
        string fullpath = Path.Combine(dataDirPath, dataFileName);
        GameData loadedData = null;

        if (!File.Exists(fullpath)) {
            return loadedData;
        }

        try {
            // Load the serialized data from the file
            string dataToLoad = "";
            using (FileStream stream = new FileStream(fullpath, FileMode.Open)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    dataToLoad = reader.ReadToEnd();
                }
            }

            // Decrypt data if needed
            if (useEncryption) {
                dataToLoad = EncryptDecrypt(dataToLoad);
            }

            // Deserialize the data from the json back into the C# object
            loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
        } 
        catch (Exception e) {
            Debug.LogError("Error when trying to load data from file: " + fullpath + "\n" + e);
        }
        
        return loadedData;
    }

    public void Save(GameData data) {
        string fullpath = Path.Combine(dataDirPath, dataFileName);

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            // Serialize the C# game data object into JSON
            string dataToStore = JsonUtility.ToJson(data, true);

            // Encrypt data if needed
            if (useEncryption) {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            // Write the serialized data to the file
            using (FileStream stream = new FileStream(fullpath, FileMode.Create)) 
            {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.Write(dataToStore);
                }
            }
        } 
        catch (Exception e) {
            Debug.LogError("Error when trying to save data to file: " + fullpath + "\n" + e);
        }
    }

    // XOR encryption
    private string EncryptDecrypt(string data) {
        string modifiedData = "";

        for (int i = 0; i < data.Length; i++) {
            modifiedData += (char) (data[i] ^ encryptonCodeWord[i % encryptonCodeWord.Length]);
        }

        return modifiedData;
    }
}
