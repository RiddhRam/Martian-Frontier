using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";
    private bool useEncryption = false;
    private readonly string encryptionKey = "RydStud10s!TvNcD";
    private readonly string iv = "0123456789abcdef";

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
                dataToLoad = DecryptData(dataToLoad);
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
                dataToStore = EncryptData(dataToStore);
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

    // AES encryption
    private string EncryptData(string data)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
            
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Use MemoryStream to hold the encrypted data
            using (MemoryStream msEncrypt = new MemoryStream())
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
                // Write the data into the stream to be encrypted
                swEncrypt.Write(data);
                swEncrypt.Close();  // Close to finish encryption process

                // Return the encrypted data as a Base64 string
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    private string DecryptData(string data)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
            
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(data)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}
