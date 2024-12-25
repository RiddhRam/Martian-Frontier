using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Collections;
using Codice.CM.Common.Encryption;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        // If no game save, then return null
        if (!File.Exists(fullpath)) {
            return loadedData;
        }

        try {
            // Just here so we can check data types of variables
            GameData testData = new GameData();
            // Load the serialized data from the file
            string dataToLoad = "";
            using (FileStream stream = new FileStream(fullpath, FileMode.Open)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    dataToLoad = reader.ReadToEnd();
                }
            }

            // Deserialize JSON
            GameDataString dataJson = JsonUtility.FromJson<GameDataString>(dataToLoad);

            // Use reflection to get all fields of the GameDataString class
            FieldInfo[] fields = typeof(GameDataString).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Get the field in the GameData class by name
                FieldInfo correspondingField = typeof(GameData).GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);

                // Get the target type of the corresponding field
                Type fieldType = correspondingField.FieldType;

                // If the field type is nullable, get the underlying type
                if (Nullable.GetUnderlyingType(fieldType) != null) {
                    fieldType = Nullable.GetUnderlyingType(fieldType);
                }
                
                if (correspondingField != null)
                {
                    var value = field.GetValue(dataJson).ToString();

                    // These types are't encrypted, material dictionary, or tilemap data
                    if (fieldType == typeof(SerializableDictionary<Vector2Int, int>[])) {
                        // Regular expression to find the contents within {}
                        string pattern = @"\{.*?\}";

                        // Match the pattern
                        MatchCollection matches = Regex.Matches(value, pattern);
                        SerializableDictionary<Vector2Int, int>[] newArray = new SerializableDictionary<Vector2Int, int>[42];

                        // Print each match
                        int index = 0;
                        foreach (Match match in matches)
                        {
                            string matchedValue = match.Value.Replace("(", "\"(").Replace(")", ")\"").Trim('{', '}');
                            // Use a regex pattern to match key-value pairs
                            var regex = new Regex(@"\""(.*?)\"":(\d+)");
                            
                            var matchesKVP = regex.Matches(matchedValue);

                            SerializableDictionary<Vector2Int, int> dict = new();
                            // Loop through the matches and add them to the dictionary
                            foreach (Match matchKVP in matchesKVP)
                            {
                                string coord = matchKVP.Groups[1].Value;
                                
                                // Format string
                                coord = coord.Replace("(", "").Replace("\"", "");
                                coord = coord.Replace(")", "");
                                
                                string[] components = coord.Split(',');
                                // Construct a vector
                                int x = int.Parse(components[0]);
                                int y = int.Parse(components[1]);

                                Vector2Int newKey = new Vector2Int(x, y); // Extract the key
                                int newInt = int.Parse(matchKVP.Groups[2].Value); // Extract and parse the value
                                dict.Add(newKey, newInt);
                            }
                            newArray[index] = dict;
                            index++;
                        }
                        
                        correspondingField.SetValue(testData, newArray);
                    } else if (fieldType == typeof(SerializableDictionary<string, MaterialManagerData>)) {
                        // Trim the outer [ ] and also turn the url encoding back to quotation marks
                        value = value.Substring(1, value.Length - 2).Replace("%22", "\"");
                        value = "{" + value + "}";

                        SerializableDictionary<string, MaterialManagerData> materialManagerData = JsonUtility.FromJson<SerializableDictionary<string, MaterialManagerData>>(value);
                        correspondingField.SetValue(testData, materialManagerData);
                    }
                    
                    // value is a string, and we need to convert it to the right type
                    else {
                        try {
                            string strValue = value;
                            
                            if (useEncryption) {
                                strValue = DecryptData(strValue);
                            }
                            
                            if (fieldType == typeof(Vector3)) {
                                // Format string
                                strValue = strValue.Replace("(", "");
                                strValue = strValue.Replace(")", "");
                                string[] components = strValue.Split(',');

                                // Construct a vector
                                float x = float.Parse(components[0]);
                                float y = float.Parse(components[1]);
                                float z = float.Parse(components[2]);

                                Vector3 newVector = new Vector3(x, y, z);

                                // Set the converted value to the field in testData
                                correspondingField.SetValue(testData, newVector);
                            } 
                            else if (fieldType == typeof(float)) {
                                float newFloat = float.Parse(strValue);
                                // Set the converted value to the field in testData
                                correspondingField.SetValue(testData, newFloat);
                            } 
                            else if (fieldType == typeof(List<string>)) {
                                // URL decode all quotation marks
                                strValue = strValue.Replace("%22", "\"");
                                List<string> deserializedValue = JsonConvert.DeserializeObject<List<string>>(strValue);
                                correspondingField.SetValue(testData, deserializedValue);
                            } else if (fieldType == typeof(int)) {
                                int newInt = int.Parse(strValue);
                                correspondingField.SetValue(testData, newInt);
                            } else if (fieldType == typeof(bool)) {
                                bool newBool = bool.Parse(strValue);
                                correspondingField.SetValue(testData, newBool);
                            } else if (fieldType == typeof(int[])) {
                                int[] deserializedValue = JsonConvert.DeserializeObject<int[]>(strValue);
                                correspondingField.SetValue(testData, deserializedValue);
                            }
                            else {
                                // Convert value to the corresponding field type
                                var convertedValue = Convert.ChangeType(strValue, fieldType);

                                // Set the converted value to the field in testData
                                correspondingField.SetValue(testData, convertedValue);
                            }
                        }
                        catch {
                            Debug.LogError("Failed to convert and set value for field ");
                        }
                    }
                }
            }

            // Deserialize the data from the json back into the C# object
            loadedData = testData;
        } 
        catch (Exception ex){
            Debug.LogError("Error when trying to load data from file: "  + fullpath + "\n" + ex.Message);
        }
        
        return loadedData;
    }

    public void Save(GameData data) {
        string fullpath = Path.Combine(dataDirPath, dataFileName);

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            string dataToStore = CreateJson(data);

            // Write the serialized data to the file
            using (FileStream stream = new FileStream(fullpath, FileMode.Create)) 
            {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.Write(dataToStore);
                }
            }
        } 
        catch {
            Debug.LogError("Error when trying to save data to file: ");
        }
    }

    private string CreateJson(GameData data)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\n");

        // Use reflection to loop through all fields in the GameData class
        FieldInfo[] fields = typeof(GameData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            object fieldValue = field.GetValue(data);

            // These can become very large, they store the data of all destroyed and revealed blocks 
            // encryption is not needed and takes too long
            if (fieldValue is SerializableDictionary<Vector2Int, int>[] dictionaryArray)
            {

                jsonBuilder.Append($"  \"{field.Name}\": \"[");

                foreach (var dictionary in dictionaryArray)
                {
                    string result = JsonConvert.SerializeObject(dictionary);
                    // Clear all quotes around the coordinates
                    result = result.Replace("\"", "");

                    jsonBuilder.Append(result);
                }

                // Remove trailing comma
                if (jsonBuilder.Length > 1)
                    jsonBuilder.Length -= 2;
        
                jsonBuilder.Append("]\",\n");
            }
            else if (fieldValue is SerializableDictionary<string, MaterialManagerData> stringdictionaryArray) {

                string json = JsonUtility.ToJson(stringdictionaryArray);
                json = json.Trim('{', '}');
                json = json.Replace("\"", "%22");
                json = "\"[" + json + "]\"";

                jsonBuilder.Append($"  \"{field.Name}\": {json},\n");
            }
            else if (fieldValue is List<string>) {
                List<string> value = (List<string>) fieldValue;

                string result = JsonConvert.SerializeObject(value);

                // URL encode all quotation marks to make it safer for when we load the game
                result = result.Replace("\"", "%22");

                if (useEncryption) {
                    result = EncryptData(result);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            } else if (fieldValue is int[]) {
                int[] value = (int[]) fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption) {
                    result = EncryptData(result);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else
            {
                string valueToUse = fieldValue.ToString();
                
                // Use encryption only if outside of editor
                if (useEncryption) {
                    valueToUse = EncryptData(valueToUse);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{valueToUse}\",\n");
            }
        }

        // Remove trailing comma
        if (jsonBuilder.Length > 2)
            jsonBuilder.Remove(jsonBuilder.Length - 2, 1);

        jsonBuilder.Append("}");

        return jsonBuilder.ToString();
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