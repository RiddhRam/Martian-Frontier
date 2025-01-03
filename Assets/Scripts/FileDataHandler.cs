using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";
    private bool useEncryption = false;
    private readonly string encryptionKey = "RydStud10s!TvNcD";

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
            // Load the serialized data from the file
            string dataToLoad = "";
            using (FileStream stream = new FileStream(fullpath, FileMode.Open)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    dataToLoad = reader.ReadToEnd();
                }
            }

            // Deserialize the data from the json back into the C# object
            loadedData = ParseJson(dataToLoad);
        }  
        catch {
        }
        
        return loadedData;
    }

    private GameData ParseJson(string dataToLoad) {
        // Temporarily save data here, then we will return it later
        GameData tempData = new GameData();

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
                if (fieldType == typeof(SerializableDictionary<Vector2Int, int>[,])) {
                    // Regular expression to find the contents within {}
                    string pattern = @"\{.*?\}";

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

                    correspondingField.SetValue(tempData, newArray);
                } else if (fieldType == typeof(SerializableDictionary<string, MaterialManagerData>)) {
                    // Trim the outer [ ] and also turn the url encoding back to quotation marks
                    value = value.Substring(1, value.Length - 2).Replace("%22", "\"");
                    value = "{" + value + "}";

                    SerializableDictionary<string, MaterialManagerData> materialManagerData = JsonUtility.FromJson<SerializableDictionary<string, MaterialManagerData>>(value);
                    correspondingField.SetValue(tempData, materialManagerData);
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
                        } else if (fieldType == typeof(int)) {
                            int newInt = int.Parse(strValue);
                            correspondingField.SetValue(tempData, newInt);
                        } else if (fieldType == typeof(bool)) {
                            bool newBool = bool.Parse(strValue);
                            correspondingField.SetValue(tempData, newBool);
                        } else if (fieldType == typeof(int[])) {
                            int[] deserializedValue = JsonConvert.DeserializeObject<int[]>(strValue);
                            correspondingField.SetValue(tempData, deserializedValue);
                        }
                        else {
                            // Convert value to the corresponding field type
                            var convertedValue = Convert.ChangeType(strValue, fieldType);

                            // Set the converted value to the field in tempData
                            correspondingField.SetValue(tempData, convertedValue);
                        }
                    }
                    catch {
                        // If field is corrupted, then the user most likely finished the tutorial already, since game is most likely
                        // to be corrupted when the map is intense, and its usually only intense after you pass the tutorial
                        if (fieldType == typeof(bool)) {
                            correspondingField.SetValue(tempData, true);
                        }
                    }
                }
            }
        }

        return tempData;
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
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            } else if (fieldValue is int[]) {
                int[] value = (int[]) fieldValue;

                string result = JsonConvert.SerializeObject(value);

                if (useEncryption) {
                    result = EncryptDecrypt(result, true);
                }

                jsonBuilder.Append($"  \"{field.Name}\": \"{result}\",\n");
            }
            else
            {
                string valueToUse = fieldValue.ToString();
                
                // Use encryption only if outside of editor
                if (useEncryption) {
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
}