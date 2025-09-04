using System.Collections;
using UnityEngine;

public class AudioDelegator : MonoBehaviour
{
    private static AudioDelegator _instance;
    public static AudioDelegator Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<AudioDelegator>();
            }
            return _instance;
        }
    }

    public GameObject backgroundAmbientMusic;
    private BackgroundAmbientMusic backgroundAmbientMusicScript;
    private bool musicEnabled = true;
    public bool soundFXEnabled = true;

    public float audioTimer = 0;
    public bool audioPlaying;
    public float timeAudioVolume;

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

    public IEnumerator PlayTimedAudio(AudioSource audioSource, AudioClip audioClip, float volume, bool forcePlay) {

        audioTimer += 0.5f;

        if (audioPlaying && !forcePlay) {
            yield break;
        }

        audioPlaying = true;
        PlayAudio(audioSource, audioClip, volume);

        float time = 0;

        timeAudioVolume = volume;

        // Wait until audio plays fully or timer is up
        while (time < audioClip.length && audioTimer > 0) {
            
            yield return new WaitForSecondsRealtime(0.5f);
            time += 0.5f;
            audioTimer -= 0.5f;
        }

        // Audio played fully
        if (time >= audioClip.length) {
            audioPlaying = false;
            yield break;
        }

        const float fadeDuration = 0.25f;

        // Audio timer is up, need to fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime) {
            float currentVolume = Mathf.Lerp(volume, 0, t / fadeDuration);
            // If music is not enabled then volume is 0;
            currentVolume = soundFXEnabled ? currentVolume : 0;
            audioSource.volume = currentVolume;
            
            timeAudioVolume = currentVolume;
            yield return null;
        }

        audioSource.Stop();
        audioPlaying = false;
    }

    public void UpdateMusicVolume(bool newValue) {
        musicEnabled = newValue;
        backgroundAmbientMusicScript.UpdateMusicVolume(musicEnabled);
    }
}
