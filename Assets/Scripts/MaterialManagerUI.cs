using UnityEngine;
using TMPro;

// Same thing but uses TextMeshProGUI
public class MaterialManagerUI : MonoBehaviour
{
    // This is public for easy direct access
    public int count = 0;
    public string materialName;
    // This might be a different type of index compared to the one in MaterialManager
    public int materialIndex;

    // Use this instead of start in case of lag, this way count will be gauranteed to be updated
    public void SetCount(int newCount)
    {
        count = newCount;
        // Get the TextMeshPro component on the child object
        TextMeshProUGUI countText = GetComponentInChildren<TextMeshProUGUI>();
        countText.text = count.ToString();
    }
}
