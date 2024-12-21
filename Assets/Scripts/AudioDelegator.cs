using UnityEngine;

public class AudioDelegator : MonoBehaviour
{
    public GameObject backgroundAmbientMusic;
    private BackgroundAmbientMusic backgroundAmbientMusicScript;
    private bool musicEnabled = true;
    public bool soundFXEnabled = true;

    // Start is called before the first frame update
    void Awake()
    {
        backgroundAmbientMusicScript = backgroundAmbientMusic.GetComponent<BackgroundAmbientMusic>();
    }

    public void PlayAudio(AudioSource audioSource, AudioClip audioClip, float volume) {
        // If soundFX disabled, volume will be 0
        volume = soundFXEnabled ? volume : 0;

        // Try to keep max volume at around -23dB
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void UpdateMusicVolume(bool newValue) {
        musicEnabled = newValue;
        backgroundAmbientMusicScript.UpdateMusicVolume(musicEnabled);
    }
}
