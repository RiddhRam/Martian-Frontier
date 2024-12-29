using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SettingsDelegator : MonoBehaviour
{
    public GameObject UIDelegation;
    public GameObject musicToggle;
    public GameObject soundFXToggle;
    public GameObject languageDropdown;
    public GameObject graphicsQualityDropdown;
    public GameObject restartTutorialButton;
    public GameObject tutorialPreFab;

    private GameObject audioDelegator;
    private bool musicEnabled;
    private bool soundFXEnabled;
    private AnalyticsDelegator analyticsDelegator;

    // FOR BOOLEANS (toggles), 0 = false, 1 = true
    void Start()
    {
        audioDelegator = GameObject.Find("Audio Delegator");
        languageDropdown.GetComponent<LanguageDelegator>().settingsDelegator = gameObject;
        analyticsDelegator = AnalyticsDelegator.Instance;
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
        AudioDelegator audioDelegatorScript = audioDelegator.GetComponent<AudioDelegator>();

        musicEnabled = GetPlayerPrefBool("Music");
        UpdateToggleColors(musicToggle.GetComponent<Toggle>(), musicEnabled);
        audioDelegatorScript.UpdateMusicVolume(musicEnabled);

        soundFXEnabled = GetPlayerPrefBool("SoundFX");
        UpdateToggleColors(soundFXToggle.GetComponent<Toggle>(), soundFXEnabled);
        audioDelegatorScript.soundFXEnabled = soundFXEnabled;
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
        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.SelectLanguage(language);
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
            case "fr":
                return "FRANÇAIS"; // French
            case "hi":
                return "हिन्दी"; // Hindi
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
            case "French":
                return "fr"; // French
            case "FRANÇAIS":
                return "fr"; // French
            case "Hindi":
                return "hi"; // Hindi
            case "हिन्दी":
                return "hi"; // Hindi
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

    public void RestartTutorial() {
        GameObject tutorialGO = Instantiate(tutorialPreFab);
        tutorialGO.transform.SetParent(UIDelegation.transform, false);
        tutorialGO.GetComponent<TutorialManager>().GameLoaded();
    }
}