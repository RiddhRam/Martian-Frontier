using UnityEngine;
using TMPro;
using System;

public class MaterialManager : MonoBehaviour
{
    // This is public for easy direct access
    public int count = 0;
    public String materialName;

    // Use this instead of start in case of lag, this way count will be gauranteed to be updated
    public void SetCount(int newCount)
    {
        count = newCount;
        // Get the TextMeshPro component on the child object
        TextMeshPro countText = GetComponentInChildren<TextMeshPro>();
        countText.text = count.ToString();
    }

}
