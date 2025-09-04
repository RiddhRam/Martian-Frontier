using UnityEngine;

public class BackgroundAmbientMusic : MonoBehaviour
{
    public AudioClip[] backgroundSongs;
    public float[] backgroundSongVolumes;
    private AudioSource audioSource;
    private int currentSongIndex = 0; // Index of the current song
    private float fadeDuration = 7.0f;
    private bool musicEnabled = true;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = backgroundSongs[currentSongIndex];
        audioSource.volume = backgroundSongVolumes[currentSongIndex];
        audioSource.Play();

        StartCoroutine(FadeOutAndPlayNext());
    }

    System.Collections.IEnumerator FadeOutAndPlayNext() {

        yield return new WaitForSeconds(audioSource.clip.length - fadeDuration);
        
        // Fade out the current clip
        for (float t = 0; t < fadeDuration; t += Time.deltaTime) {
            float volume = Mathf.Lerp(backgroundSongVolumes[currentSongIndex], 0, t / fadeDuration);
            // If music is not enabled then volume is 0;
            volume = musicEnabled ? volume : 0;
            audioSource.volume = volume;
            yield return null;
        }

        audioSource.volume = 0;
        audioSource.Stop();

        // Increment index, looping back to 0 if necessary
        currentSongIndex = (currentSongIndex + 1) % backgroundSongs.Length;

        // Set the next clip and fade it in
        audioSource.clip = backgroundSongs[currentSongIndex];
        audioSource.Play();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime) {
            float volume = Mathf.Lerp(0, backgroundSongVolumes[currentSongIndex], t / fadeDuration);
            // If music is not enabled then volume is 0;
            volume = musicEnabled ? volume : 0;
            audioSource.volume = volume;
            yield return null;
        }

        audioSource.volume = musicEnabled ? backgroundSongVolumes[currentSongIndex] : 0;

        StartCoroutine(FadeOutAndPlayNext());
    }

    public void UpdateMusicVolume(bool newValue) {
        musicEnabled = newValue;
        // Immediately set to 0 if needed
        audioSource.volume = musicEnabled ? backgroundSongVolumes[currentSongIndex] : 0;
    }
}
