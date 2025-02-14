using UnityEngine;

[System.Serializable]
public class MaterialManagerData
{
    public int count;
    public string materialName;
    public int materialIndex;
    public string id;
    public Vector3 position;
    public float drillProfitMultiplier;

    public MaterialManagerData()
    {
        drillProfitMultiplier = 0f; // Ensuring default value
    }
}
