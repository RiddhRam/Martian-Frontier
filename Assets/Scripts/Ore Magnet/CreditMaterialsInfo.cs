using UnityEngine;
using TMPro;
using System.Collections;

public class CreditMaterialsInfo : MonoBehaviour
{
    public int count;
    [SerializeField] private GameObject mapSpriteIcons;
    [SerializeField] private SpriteRenderer miniSpriteRenderer;
    private GameObject mapCamera;
    float baseTimeWait = 0.5f;
    float extraTimeWait = 1f;

    int maxCredits = 50;

    void Awake() {
        mapCamera = GameObject.Find("UI").GetComponent<UIDelegation>().mapCamera;
    }

    void OnEnable() {
        if (!mapCamera.GetComponent<MapRecordingMode>().isActiveAndEnabled) {
            StartCoroutine(ToggleSpriteVisibility());
        } 
    }

    private IEnumerator ToggleSpriteVisibility() {
        float timer = 0f;

        while (true) {
            yield return new WaitForSeconds(baseTimeWait);

            if (!mapCamera.activeSelf) {
                continue;
            }

            timer += baseTimeWait;

            if (mapSpriteIcons.activeSelf && timer >= (baseTimeWait + extraTimeWait)) {
                mapSpriteIcons.SetActive(false); // Hide the sprite
                timer = 0f; // Reset timer
            } else if (!mapSpriteIcons.activeSelf && timer >= baseTimeWait) {
                mapSpriteIcons.SetActive(true); // Show the sprite
                timer = 0f; // Reset timer
                yield return new WaitForSeconds(extraTimeWait);
            }
        }
    }

    // Use this instead of start in case of lag, this way count will be gauranteed to be updated
    public void SetCount(int newCount)
    {
        count = newCount;
        // Get the TextMeshPro component on the child object
        TextMeshPro countText = GetComponentInChildren<TextMeshPro>();
        countText.text = count.ToString();

        float percentage = count / maxCredits;
        float redAndBlueValue = 255f - (percentage * 255);
        miniSpriteRenderer.color = new(redAndBlueValue, 255f, redAndBlueValue);
        mapSpriteIcons.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new(redAndBlueValue, 255f, redAndBlueValue);
    }
}
