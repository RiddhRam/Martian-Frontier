using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";
    private bool useEncryption = false;
    private readonly string encryptionKey = "HIDDEN FROM PUBLIC REPOSITORY";
    public bool gameDataValid = false;

    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption) {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName; 
        this.useEncryption = useEncryption;
    }

    public GameData Load() {
        string fullpath = Path.Combine(dataDirPath, dataFileName);
        GameData loadedData = new();

        // If no game save, then return new()
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

            // Deserialize the data from the json back into the C# object
            loadedData = ParseJson(dataToLoad, useEncryption);
        }  
        catch (Exception ex) {
            Debug.LogError("Loading error: " + ex.Message);
        }
        
        return loadedData;
    }

    public GameData ParseJson(string dataToLoad, bool useEncryption) {
        // Temporarily save data here, then we will return it later
        GameData tempData = new();
        
        // Deserialize JSON
        GameDataString dataJson = JsonConvert.DeserializeObject<GameDataString>(dataToLoad) ?? new GameDataString();

        // Use reflection to get all fields of the GameDataString class
        FieldInfo[] fields = typeof(GameDataString).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            // Get the field in the GameData class by name
            FieldInfo correspondingField = typeof(GameData).GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);;
            // Get the target type of the corresponding field
            Type fieldType = correspondingField.FieldType;            

            // If the field type is nullable, get the underlying type
            if (Nullable.GetUnderlyingType(fieldType) != null) {
                fieldType = Nullable.GetUnderlyingType(fieldType);
            }
            if (correspondingField == null || field.GetValue(dataJson) == null)
            {
                continue;
            }

            var value = field.GetValue(dataJson).ToString();;
            
            // These types are't encrypted, tilemap data or other large fields
            if (fieldType == typeof(SerializableDictionary<Vector2Int, int>[,])) {
                // Regular expression to find the contents within {}
                /*string pattern = @"\{.*?\}";

                // Match the pattern
                MatchCollection matches = Regex.Matches(value, pattern);
                int totalColumns = tempData.destroyedTilemapsTileValues.GetLength(0);
                int totalRows = tempData.destroyedTilemapsTileValues.GetLength(1);
                SerializableDictionary<Vector2Int, int>[,] newArray = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];

                // Print each match
                int rowIndex = 0;
                int columnIndex = 0;
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
                    newArray[columnIndex, rowIndex] = dict;

                    rowIndex++;
                    if (rowIndex == totalRows) {
                        rowIndex = 0;
                        columnIndex++;
                    }
                }

                correspondingField.SetValue(tempData, newArray);*/
            } 
            // value is a string, and we need to convert it to the right type
            else {
                try {
                    string strValue = value;
                    
                    if (useEncryption) {
                        strValue = EncryptDecrypt(strValue, false);
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

                        // Set the converted value to the field in tempData
                        correspondingField.SetValue(tempData, newVector);
                    } 
                    else if (fieldType == typeof(float)) {
                        float newFloat = float.Parse(strValue);
                        // Set the converted value to the field in tempData
                        correspondingField.SetValue(tempData, newFloat);
                    } 
                    else if (fieldType == typeof(List<string>)) {
                        // URL decode all quotation marks
                        strValue = strValue.Replace("%22", "\"");
                        List<string> deserializedValue = JsonConvert.DeserializeObject<List<string>>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(List<int>)) {
                        List<int> deserializedValue = JsonConvert.DeserializeObject<List<int>>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(HashSet<string>)) {
                        // URL decode all quotation marks
                        strValue = strValue.Replace("%22", "\"");
                        HashSet<string> deserializedValue = JsonConvert.DeserializeObject<HashSet<string>>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(HashSet<int>))
                    {
                        HashSet<int> deserializedValue = JsonConvert.DeserializeObject<HashSet<int>>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(SerializableDictionary<string, VehicleUpgrade>))
                    {
                        // Same as below intDictData
                        // Trim the outer [ ] and also turn the url encoding back to quotation marks
                        strValue = strValue.Substring(1, strValue.Length - 2).Replace("%22", "\"");
                        strValue = "{" + strValue + "}";

                        SerializableDictionary<string, VehicleUpgrade> vehicleData = JsonUtility.FromJson<SerializableDictionary<string, VehicleUpgrade>>(strValue);
                        correspondingField.SetValue(tempData, vehicleData);
                    }
                    else if (fieldType == typeof(SerializableDictionary<string, VehicleCustomization>))
                    {

                        // Same as below intDictData
                        // Trim the outer [ ] and also turn the url encoding back to quotation marks
                        strValue = strValue.Substring(1, strValue.Length - 2).Replace("%22", "\"");
                        strValue = "{" + strValue + "}";

                        SerializableDictionary<string, VehicleCustomization> vehicleData = JsonUtility.FromJson<SerializableDictionary<string, VehicleCustomization>>(strValue);
                        correspondingField.SetValue(tempData, vehicleData);
                    }
                    else if (fieldType == typeof(SerializableDictionary<int, int>))
                    {
                        // Same as below intDictData
                        // Trim the outer [ ] and also turn the url encoding back to quotation marks
                        strValue = strValue.Substring(1, strValue.Length - 2).Replace("%22", "\"");
                        strValue = "{" + strValue + "}";

                        SerializableDictionary<int, int> intDictData = JsonUtility.FromJson<SerializableDictionary<int, int>>(strValue);
                        correspondingField.SetValue(tempData, intDictData);
                    }
                    else if (fieldType == typeof(SerializableDictionary<string, int>))
                    {
                        // Same as above
                        // Trim the outer [ ] and also turn the url encoding back to quotation marks
                        strValue = strValue.Substring(1, strValue.Length - 2).Replace("%22", "\"");
                        strValue = "{" + strValue + "}";

                        SerializableDictionary<string, int> intDictData = JsonUtility.FromJson<SerializableDictionary<string, int>>(strValue);
                        correspondingField.SetValue(tempData, intDictData);
                    }
                    else if (fieldType == typeof(long))
                    {
                        long newInt = long.Parse(strValue);
                        correspondingField.SetValue(tempData, newInt);
                    }
                    else if (fieldType == typeof(int))
                    {
                        int newInt = int.Parse(strValue);
                        correspondingField.SetValue(tempData, newInt);
                    }
                    else if (fieldType == typeof(bool))
                    {
                        bool newBool = bool.Parse(strValue);
                        correspondingField.SetValue(tempData, newBool);
                    }
                    else if (fieldType == typeof(int[]))
                    {
                        int[] deserializedValue = JsonConvert.DeserializeObject<int[]>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(float[]))
                    {
                        float[] deserializedValue = JsonConvert.DeserializeObject<float[]>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else if (fieldType == typeof(bool[]))
                    {
                        bool[] deserializedValue = JsonConvert.DeserializeObject<bool[]>(strValue);
                        correspondingField.SetValue(tempData, deserializedValue);
                    }
                    else
                    {
                        // Convert value to the corresponding field type
                        var convertedValue = Convert.ChangeType(strValue, fieldType);

                        // Set the converted value to the field in tempData
                        correspondingField.SetValue(tempData, convertedValue);
                    }
                }
                catch (Exception ex) {
                    Debug.LogError(field.Name + ": " + ex.Message);
                    // If field is corrupted, then the user most likely finished the tutorial already, since game is most likely
                    // to be corrupted when the map is intense, and its usually only intense after you pass the tutorial
                    if (fieldType == typeof(bool)) {
                        correspondingField.SetValue(tempData, true);
                    } else if (field == typeof(SerializableDictionary<string, VehicleUpgrade>)) {
                        correspondingField.SetValue(tempData, new());
                    } else if (field == typeof(SerializableDictionary<string, VehicleCustomization>)) {
                        correspondingField.SetValue(tempData, new());
                    } else if (field == typeof(SerializableDictionary<string, int>)) {
                        correspondingField.SetValue(tempData, new());
                    }
                }
            }
        }

        return tempData;
    }

    public async Task SaveAsync(GameData data) {

        string fullPath = Path.Combine(dataDirPath, dataFileName);
        string tempPath = fullPath + ".tmp";

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = CreateJson(data, useEncryption);

            // Make sure game data is valid
            if (!VerifyGameDataIntegrity(dataToStore)) {
                Debug.LogError("No game integrity");
                return;
            }

            // Write to a temporary file first
            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 2097152, useAsync: true))
            using (StreamWriter writer = new StreamWriter(stream)) {
                await writer.WriteAsync(dataToStore);
            }

            // Replace the original file with the temporary file
            // If the original file exists, replace it. Otherwise, move the temp file.
            if (File.Exists(fullPath)) {
                File.Replace(tempPath, fullPath, null);
            } else {
                File.Move(tempPath, fullPath);
            }
        } 
        catch (Exception ex) {
            Debug.Log($"Error when trying to save data to file: {ex.Message}");
        }
    }

    public void Save(GameData data) {

        string fullPath = Path.Combine(dataDirPath, dataFileName);
        string tempPath = fullPath + ".tmp";

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Save time
            data.offlineTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string dataToStore = CreateJson(data, useEncryption);

            // Make sure game data is valid
            if (!VerifyGameDataIntegrity(dataToStore)) {
                Debug.LogError("No game integrity");
                return;
            }

            // Write to a temporary file first
            using (FileStream stream = new FileStream(tempPath, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(stream)) {
                writer.Write(dataToStore);
            }

            // Replace the original file with the temporary file
            // If the original file exists, replace it. Otherwise, move the temp file.
            if (File.Exists(fullPath)) {
                File.Replace(tempPath, fullPath, null);
            } else {
                File.Move(tempPath, fullPath);
            }
        } 
        catch (Exception ex) {
            Debug.Log($"Error when trying to save data to file: {ex.Message}");
        }
    }

    public string CreateJson(GameData data, bool useEncryption)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\n");

        // Use reflection to loop through all fields in the GameData class
        FieldInfo[] fields = typeof(GameData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            object fieldValue = field.GetValue(data);

            // This can become very large, it stores the data of all destroyed and revealed blocks 
            // encryption is not needed and takes too long
            if (fieldValue is SerializableDictionary<Vector2Int, int>[,] dictionaryArray)
            {
                
                // Save the dictionary array column by column.
                // Save the first column by starting at the first row and going down
                // Then go to the next column to the right and repeat
                jsonBuilder.Append($"  \"{field.Name}\": \"[");

                foreach (var dictionary in dictionaryArray)
                {
                    string result = JsonConvert.SerializeObject(dictionary);
                    // Clear all quotes around the coordinates
                    result = result.Replace("\"", "");

                    jsonBuilder.Append(result);
                }

                jsonBuilder.Append("]\",\n");
            }
            else if (fieldValue is SerializableDictionary<string, VehicleCustomization> customizationDictionary) {

                // Same as intDictionaryArray below
                string json = JsonUtility.ToJson(customizationDictionary);
                json = json.Trim('{', '}');
                json = json.Replace("\"", "%22");
                json = "[" + json + "]";

                if (useEncryption) {
                    json = EncryptDecrypt(json, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{json}\",\n");
            }
            else if (fieldValue is SerializableDictionary<string, VehicleUpgrade> upgradeDictionary) {

                // Same as intDictionaryArray below
                string json = JsonUtility.ToJson(upgradeDictionary);
                json = json.Trim('{', '}');
                json = json.Replace("\"", "%22");
                json = "[" + json + "]";

                if (useEncryption) {
                    json = EncryptDecrypt(json, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{json}\",\n");                
            }
            else if (fieldValue is SerializableDictionary<int, int> intToIntDictionaryArray) {
                // Same as intDictionaryArray below
                string json = JsonUtility.ToJson(intToIntDictionaryArray);
                json = json.Trim('{', '}');
                json = json.Replace("\"", "%22");
                json = "[" + json + "]";

                if (useEncryption) {
                    json = EncryptDecrypt(json, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{json}\",\n");
            }
            // For upgrade arrays
            else if (fieldValue is SerializableDictionary<string, int> intDictionaryArray)
            {
                string json = JsonUtility.ToJson(intDictionaryArray);
                json = json.Trim('{', '}');
                json = json.Replace("\"", "%22");
                json = "[" + json + "]";

                if (useEncryption)
                {
                    json = EncryptDecrypt(json, true);
                }

                // If using encryption we need to add quotation marks, otherwise no need
                jsonBuilder.Append($"  \"{field.Name}\": \"{json}\",\n");
            }
            else if (fieldValue is List<string>)
            {
                List<string> value = (List<string>)fieldValue;

                string result = JsonConvert.SerializeObject(value);

                // URL encode all quotation marks to make it safer for when we load the game
                result = result.Replace("\"", "%22");

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is List<int>)
            {
                List<int> value = (List<int>)fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is List<long>)
            {
                List<long> value = (List<long>)fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is HashSet<string>)
            {
                HashSet<string> value = (HashSet<string>)fieldValue;

                string result = JsonConvert.SerializeObject(value);

                // URL encode all quotation marks to make it safer for when we load the game
                result = result.Replace("\"", "%22");

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is HashSet<int>)
            {
                HashSet<int> value = (HashSet<int>)fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is int[])
            {
                int[] value = (int[])fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is float[])
            {
                float[] value = (float[])fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else if (fieldValue is bool[])
            {
                bool[] value = (bool[])fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption)
                {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else
            {
                string valueToUse = fieldValue.ToString();

                // Use encryption only if outside of editor
                if (useEncryption)
                {
                    valueToUse = EncryptDecrypt(valueToUse, true);
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

    // XOR algorithm
    private string EncryptDecrypt(string data, bool encrypting) {
        byte[] returnBytes;
        byte[] result;

        // If decrypting, convert from Base64 string to byte array
        if (!encrypting)
        {
            returnBytes = Convert.FromBase64String(data);
        }
        else
        {
            returnBytes = Encoding.UTF8.GetBytes(data);
        }

        result = new byte[returnBytes.Length];

        for (int i = 0; i < returnBytes.Length; i++)
        {
            result[i] = (byte)(returnBytes[i] ^ encryptionKey[i % encryptionKey.Length]);
        }

        // If encrypting, return the Base64 string, otherwise return the decrypted string
        if (encrypting)
        {
            return Convert.ToBase64String(result);
        }
        else
        {
            return Encoding.UTF8.GetString(result);
        }
    }

    public bool VerifyGameDataIntegrity(string dataToStore) {
        try {
            ParseJson(dataToStore, useEncryption);
            gameDataValid = true;
        } catch (Exception ex) {
            Debug.LogError("Couldnt verify game integrity: " + ex.Message);
            gameDataValid = false;
            return false;
        }

        return true;
    }

}