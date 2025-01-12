using UnityEngine;
using TMPro;

[System.Serializable]
public class MaterialManager : MonoBehaviour
{
    public int count;
    public string materialName;
    public int materialIndex;
    public string id;
    private MaterialManagerData materialManagerData;
    private SpriteRenderer spriteRenderer;
    private float timer = 0f;
    private GameObject mapCamera;

    void Awake() {
        GenerateGuid();
        materialManagerData = new();
        spriteRenderer = transform.GetChild(1).GetComponent<SpriteRenderer>();
        mapCamera = GameObject.Find("UI").GetComponent<UIDelegation>().mapCamera;
    }

    private void Update() {
        if (!mapCamera.activeSelf) {
            return;
        }

        timer += Time.deltaTime;

        if (spriteRenderer.isVisible && timer >= 1.5f)
        {
            spriteRenderer.enabled = false; // Hide the sprite
            timer = 0f; // Reset timer
        }
        else if (!spriteRenderer.isVisible && timer >= 0.5f)
        {
            spriteRenderer.enabled = true; // Show the sprite
            timer = 0f; // Reset timer
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
