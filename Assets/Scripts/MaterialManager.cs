using UnityEngine;
using TMPro;
using System.Collections;

[System.Serializable]
public class MaterialManager : MonoBehaviour
{
    public int count;
    public string materialName;
    public int materialIndex;
    public string id;
    private MaterialManagerData materialManagerData;
    private SpriteRenderer spriteRenderer;
    private GameObject mapCamera;
    float baseTimeWait = 0.5f;
    float extraTimeWait = 1f;

    void Awake() {
        GenerateGuid();
        materialManagerData = new();
        spriteRenderer = transform.GetChild(1).GetComponent<SpriteRenderer>();
        mapCamera = GameObject.Find("UI").GetComponent<UIDelegation>().mapCamera;
        if (GameObject.Find("Player Vehicle").GetComponent<AIMovement>().isActiveAndEnabled) {
            baseTimeWait *= 18;
            extraTimeWait *= 18;
        }
    }

    void OnEnable() {
        StartCoroutine(ToggleSpriteVisibility());
    }

    private IEnumerator ToggleSpriteVisibility() {
        float timer = 0f;

        while (true) {
            yield return new WaitForSeconds(baseTimeWait);

            if (!mapCamera.activeSelf) {
                continue;
            }

            timer += baseTimeWait;

            if (spriteRenderer.isVisible && timer >= (baseTimeWait + extraTimeWait)) {
                spriteRenderer.enabled = false; // Hide the sprite
                timer = 0f; // Reset timer
            } else if (!spriteRenderer.isVisible && timer >= baseTimeWait) {
                spriteRenderer.enabled = true; // Show the sprite
                timer = 0f; // Reset timer
                yield return new WaitForSeconds(extraTimeWait);
            }
        }
    }

    private void GenerateGuid() {
        id = System.Guid.NewGuid().ToString();
    }

    public void UpdateData() {
        materialManagerData.count = count;
        materialManagerData.id = id;
        materialManagerData.materialIndex = materialIndex;
        materialManagerData.position = transform.localPosition;
        materialManagerData.materialName = materialName;
    }

    // Use this instead of start in case of lag, this way count will be gauranteed to be updated
    public void SetCount(int newCount)
    {
        count = newCount;
        // Get the TextMeshPro component on the child object
        TextMeshPro countText = GetComponentInChildren<TextMeshPro>();
        countText.text = count.ToString();
        UpdateData();
    }

    public MaterialManagerData GetMaterialManagerData() {
        return materialManagerData;
    }

}
