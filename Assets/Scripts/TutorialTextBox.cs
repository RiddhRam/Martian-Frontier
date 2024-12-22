using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class TutorialTextBox : MonoBehaviour
{
    public string key;
    private string message;
    private string locale;
    private TextMeshProUGUI messageGO;
    // Start is called before the first frame update
    void OnEnable()
    {
        message = GetLocalizedValue(key);
        locale = LocalizationSettings.SelectedLocale.ToString();
        messageGO = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        StartCoroutine(TypeMessage());
    }

    private string GetLocalizedValue(string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);
        
        return entry.LocalizedValue;
    }
    

    private System.Collections.IEnumerator TypeMessage()
    {
        messageGO.text = ""; // Clear the initial text
        foreach (char letter in message.ToCharArray())
        {
            messageGO.text += letter + "|"; // Add the next character
            yield return new WaitForSeconds(0.025f); // Wait before adding the next character
            messageGO.text = messageGO.text.Substring(0, messageGO.text.Length-1);
        }

        // Listen in case user switches language
        yield return new WaitUntil(() => locale != LocalizationSettings.SelectedLocale.ToString());

        // If switched language, change the message and recursively call the function
        message = GetLocalizedValue(key);
        locale = LocalizationSettings.SelectedLocale.ToString();
        StartCoroutine(TypeMessage());
    }
}
