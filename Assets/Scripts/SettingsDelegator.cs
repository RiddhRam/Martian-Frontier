using UnityEngine;
using UnityEngine.UI;

public class SettingsDelegator : MonoBehaviour
{
    public GameObject UIDelegation;
    public GameObject musicToggle;
    public GameObject soundFXToggle;
    public GameObject languageDropdown;
    public GameObject restartTutorialButton;
    public GameObject tutorialPreFab;

    private GameObject audioDelegator;
    private bool musicEnabled;
    private bool soundFXEnabled;
    private SystemLanguage languageObject;
    private string language;

    // FOR BOOLEANS (toggles), 0 = false, 1 = true
    void Start()
    {
        audioDelegator = GameObject.Find("Audio Delegator");
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

        //languageObject = LoadLanguage();
        //language = languageObject.ToString();
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

    private SystemLanguage LoadLanguage()
    {
        string savedLanguage = PlayerPrefs.GetString("Language", Application.systemLanguage.ToString()); // Default to English
        
        if (System.Enum.TryParse(savedLanguage, out SystemLanguage language))
        {
            return language;
        }
        else
        {
            return Application.systemLanguage; // Fallback in case of invalid data
        }
    }

    public void RestartTutorial() {
        GameObject tutorialGO = Instantiate(tutorialPreFab);
        tutorialGO.transform.SetParent(UIDelegation.transform, false);
        tutorialGO.GetComponent<TutorialManager>().GameLoaded();
    }
}