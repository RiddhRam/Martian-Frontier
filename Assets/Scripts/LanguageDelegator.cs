using TMPro;
using UnityEngine;

public class LanguageDelegator : MonoBehaviour
{
    public GameObject settingsDelegator;

    public void OnEnable() {
        UpdateText();
    }

    public void ChangeLanguage(int index) {
        string text = GetComponent<TMP_Dropdown>().options[index].text;
        text = settingsDelegator.GetComponent<SettingsDelegator>().GetLanguageShortCode(text);
        settingsDelegator.GetComponent<SettingsDelegator>().SetLanguage(text);
    }

    public void UpdateText() {
        SettingsDelegator settingsDelegatorScript = settingsDelegator.GetComponent<SettingsDelegator>();
        string text = settingsDelegatorScript.LoadLanguage();
        var locales = GetComponent<TMP_Dropdown>().options;

        int index = 0;
        foreach (var locale in locales)
        {
            
            if (text != settingsDelegatorScript.GetLanguageShortCode(locale.text)) {
                index++;
                continue;
            }

            GetComponent<TMP_Dropdown>().value = index;
            break;
        }
    }
}
