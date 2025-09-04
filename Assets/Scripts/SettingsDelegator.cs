using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsDelegator : MonoBehaviour
{
    public GameObject UIDelegation;
    public GameObject musicToggle;
    public GameObject soundFXToggle;
    public GameObject languageDropdown;
    public GameObject graphicsQualityDropdown;
    public GameObject generalButton;
    public GameObject generalPanel;
    public GameObject accountButton;
    public GameObject accountPanel;

    private bool musicEnabled;
    private bool soundFXEnabled;
    [SerializeField] private UpgradesDelegator upgradesDelegator;

    // FOR BOOLEANS (toggles), 0 = false, 1 = true
    void Start()
    {
        languageDropdown.GetComponent<LanguageDelegator>().settingsDelegator = gameObject;
        UpdateBools();

        // Get the Toggle components
        Toggle musicToggleComponent = musicToggle.GetComponent<Toggle>();
        Toggle soundFXToggleComponent = soundFXToggle.GetComponent<Toggle>();

        // Set initial toggle states
        musicToggleComponent.isOn = musicEnabled;
        soundFXToggleComponent.isOn = soundFXEnabled;

        // Add listeners to save preferences when toggles are changed
        musicToggleComponent.onValueChanged.AddListener((value) =>
        {
            SetPlayerPrefBool("Music", musicToggleComponent);
        });
        soundFXToggleComponent.onValueChanged.AddListener((value) =>
        {
            SetPlayerPrefBool("SoundFX", soundFXToggleComponent);
        });

        string loadedLanguage = LoadLanguage();
        SetLanguage(loadedLanguage);
        UpdateOptions();

        graphicsQualityDropdown.GetComponent<GraphicsSettingsDelegator>().OnEnable();
    }

    public void UpdateBools() {

        musicEnabled = GetPlayerPrefBool("Music");
        UpdateToggleColors(musicToggle.GetComponent<Toggle>(), musicEnabled);
        AudioDelegator.Instance.UpdateMusicVolume(musicEnabled);

        soundFXEnabled = GetPlayerPrefBool("SoundFX");
        UpdateToggleColors(soundFXToggle.GetComponent<Toggle>(), soundFXEnabled);
        AudioDelegator.Instance.soundFXEnabled = soundFXEnabled;
    }

    private void UpdateToggleColors(Toggle toggle, bool value) {
        Color newColor = value ? new Color(57f / 255f, 255f / 255f, 20f / 255f) : new Color(255f / 255f, 78f / 255f, 78f / 255f);
        toggle.transform.GetChild(0).GetComponent<Image>().color = newColor;
    }

    private bool GetPlayerPrefBool(string key) {
        // Get the value, and default to 1 if not set.
        // If it is equal to 1, then true, if 0 then false
        return PlayerPrefs.GetInt(key, 1) == 1;
    }

    public void SetPlayerPrefBool(string key, Toggle toggle)
    {
        bool value = !GetPlayerPrefBool(key);
        // Save the boolean value as an integer (1 for true, 0 for false)
        int enabledInt = value ? 1 : 0;

        toggle.isOn = value;

        PlayerPrefs.SetInt(key, enabledInt);
        PlayerPrefs.Save();

        UpdateBools();
    }

    public string LoadLanguage()
    {
        string savedLanguage = PlayerPrefs.GetString("Language", GetLanguageShortCode(Application.systemLanguage.ToString())); // Default to English
        
        return savedLanguage;
    }

    public void SetLanguage(string language)
    {

        // Get available locales
        var availableLocales = LocalizationSettings.AvailableLocales.Locales;

        // Find the Locale that matches the language code
        Locale selectedLocale = null;

        // Find the first match
        foreach (var availableLocale in availableLocales)
        {
            string languageShortCode = availableLocale.Identifier.Code;
            if (language != languageShortCode ) {
                continue;
            }
            
            selectedLocale = availableLocale;
            break;
        }

        // If no match found, set it to the application's system language
        if (selectedLocale == null)
        {
            string systemLanguageCode = Application.systemLanguage.ToString(); // Get system language
            
            foreach (var availableLocale in availableLocales)
            {
                if (availableLocale.Identifier.Code == systemLanguageCode)
                {
                    selectedLocale = availableLocale;
                    break; // Exit loop once a match is found
                }
            }
        }

        // Set the language
        LocalizationSettings.SelectedLocale = selectedLocale;
        if (upgradesDelegator) {
            upgradesDelegator.UpdateAllPowerPanels();
        }

        AnalyticsDelegator.Instance.SelectLanguage(language);
        PlayerPrefs.SetString("Language", language); // Save the selected language
    }

    private void UpdateOptions() {
        TMP_Dropdown dropdown = languageDropdown.GetComponent<TMP_Dropdown>();
        
        // Get available locales
        var locales = LocalizationSettings.AvailableLocales.Locales;

        // Create a list of string options
        var options = new List<string>();
        foreach (var locale in locales)
        {
            string languageShortCode = locale.Identifier.Code;
            string languageName = GetLanguageFullName(languageShortCode);
            options.Add(languageName);
        }

        // Clear any existing options and add new ones
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    public string GetLanguageFullName(string languageShortCode)
    {
        switch (languageShortCode)
        {
            case "zh":
                return "中文 (简体)"; // Chinese Simplified
            case "en":
                return "ENGLISH"; // English
            case "fil":
                return "FILIPINO";
            case "fr":
                return "FRANÇAIS"; // French
            case "hi":
                return "हिन्दी"; // Hindi
            case "id":
                return "INDONESIAN"; // Indonesian
            case "ja":
                return "日本語"; // Japanese
            case "ko":
                return "한국어"; // Korean
            case "pt":
                return "PORTUGUÊS"; // Portuguese
            case "ru":
                return "РУССКИЙ"; // Russian
            case "es":
                return "ESPAÑOL"; // Spanish
            default:
                return "ENGLISH"; // Default case for unsupported codes
        }
    }

    public string GetLanguageShortCode(string languageFullName)
    {
        switch (languageFullName)
        {
            case "Chinese (Simplified)":
                return "zh"; // Chinese Simplified
            case "中文 (简体)":
                return "zh"; // Chinese Simplified
            case "English":
                return "en"; // English
            case "ENGLISH":
                return "en"; // English
            case "Filipino":
                return "fil"; // Filipino
            case "FILIPINO":
                return "fil"; // Filipino
            case "French":
                return "fr"; // French
            case "FRANÇAIS":
                return "fr"; // French
            case "Hindi":
                return "hi"; // Hindi
            case "हिन्दी":
                return "hi"; // Hindi
            case "Indonesian":
                return "id"; // Indonesian
            case "INDONESIAN":
                return "id"; // Indonesian
            case "Japanese":
                return "ja"; // Japanese
            case "日本語":
                return "ja"; // Japanese
            case "Korean":
                return "ko"; // Korean
            case "한국어":
                return "ko"; // Korean
            case "Portuguese":
                return "pt"; // Portuguese
            case "PORTUGUÊS":
                return "pt"; // Portuguese
            case "Russian":
                return "ru"; // Russian
            case "РУССКИЙ":
                return "ru"; // Russian
            case "Spanish":
                return "es"; // Spanish
            case "ESPAÑOL":
                return "es"; // Spanish
            default:
                return "en"; // Default case for unsupported full names
        }
    }

    public void TogglePanel(string type) {
        if (type == "General") {
            accountPanel.SetActive(false);
            accountButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            accountButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            generalPanel.SetActive(true);
            generalButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            generalButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        } else {
            generalPanel.SetActive(false);
            generalButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            generalButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            accountPanel.SetActive(true);
            accountButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            accountButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        }
    }

}