using UnityEngine;

public class BackgroundAmbientMusic : MonoBehaviour
{
    public AudioClip[] backgroundSongs;
    public float[] backgroundSongVolumes;
    private AudioSource audioSource;
    private int currentSongIndex = 0; // Index of the current song
    private float fadeDuration = 7.0f;

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
            audioSource.volume = Mathf.Lerp(backgroundSongVolumes[currentSongIndex], 0, t / fadeDuration);
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
            audioSource.volume = Mathf.Lerp(0, backgroundSongVolumes[currentSongIndex], t / fadeDuration);
            yield return null;
        }

        audioSource.volume = backgroundSongVolumes[currentSongIndex];

        StartCoroutine(FadeOutAndPlayNext());
    }
}
