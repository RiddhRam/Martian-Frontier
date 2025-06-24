using TMPro;
using UnityEngine;

public class GraphicsSettingsDelegator : MonoBehaviour
{
    private int motionQuality = 0;
    private int[] frameRates = {60, 30};

    public void OnEnable() {
        motionQuality = PlayerPrefs.GetInt("Motion Quality");
        ChangeFramerate(motionQuality);
        UpdateText();
    }

    public void ChangeFramerate(int newFrameRateIndex) {
        motionQuality = newFrameRateIndex;

        PlayerPrefs.SetInt("Motion Quality", motionQuality);
        Application.targetFrameRate = frameRates[motionQuality];
    }  

    public void UpdateText() {
        GetComponent<TMP_Dropdown>().value = PlayerPrefs.GetInt("Motion Quality");
    }
}
