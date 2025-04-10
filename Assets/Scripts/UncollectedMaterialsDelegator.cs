using System.Linq;
using UnityEngine;

public class UncollectedMaterialsDelegator : MonoBehaviour
{
    public OreDelegation oreDelegation;
    public SerializableDictionary<string, MaterialManagerData> uncollectedMaterials = new();
    private MaterialManager materialManager;
    private System.Random random = new();
    public int materialCount;

    void Awake()
    {
        materialCount = 0;   
    }

    public void RemoveMaterial(string materialID)
    {
        try {
            this.materialCount -= uncollectedMaterials[materialID].count;
            uncollectedMaterials.Remove(materialID);
        } catch {
        }
    }

    public void UpdateMaterial(MaterialManager materialManager, GameObject materialToAdd) {
        // If player has ores in a hauler, resets mine, switches to a driller, then switches to a different smaller hauler and tries to pick up all of previous haulers ores, this will fail
        try {
            MaterialManagerData oldValue = uncollectedMaterials[materialManager.GetMaterialManagerData().id];
            
            this.materialCount -= oldValue.count - materialManager.count;
            
            oldValue.count = materialManager.count;
            // material type stays the same, count gets updated
            uncollectedMaterials[materialManager.id] = oldValue;
        } catch {
            try {
                AddMaterial(materialToAdd, materialToAdd.transform.position, materialManager.materialIndex, materialManager.count, materialManager.drillProfitMultiplier);
                this.materialCount += materialManager.count;
            } catch {
            }
        }
    
    }

    public void AddMaterial(GameObject materialToAdd, Vector3 materialPosition, int materialIndex, int materialCount, float profitMultiplier) {
        // Set its values
        // Need to manually put it in the right spot, do this before SetCount, so it happens before UpdateData() in MaterialManager
        materialToAdd.transform.localPosition = materialPosition;
        materialManager = materialToAdd.GetComponent<MaterialManager>();
        materialManager.materialIndex = materialIndex;
        materialManager.drillProfitMultiplier = profitMultiplier;
        materialManager.SetCount(materialCount);
        // add the material to the dictionary
        materialToAdd.transform.SetParent(transform);
        uncollectedMaterials[materialManager.id] = materialManager.GetMaterialManagerData();
        this.materialCount += materialCount;
    }

    public System.Numerics.BigInteger GetMineValue() {
        System.Numerics.BigInteger mineValue = 0;

        foreach (var kvp in uncollectedMaterials) {
            mineValue += kvp.Value.count * oreDelegation.GetMaterialPrices()[kvp.Value.materialIndex];
        }

        return mineValue;
    }

    public Vector3 GetRandomMaterialLocation(int tier) {
        if (uncollectedMaterials.Count == 0)
            return new(0, -6);

        foreach (var material in uncollectedMaterials.Values.OrderBy(x => random.Next())) {

            if (ConvertIndexToTier(material.materialIndex) == 1 && tier == 3) {
                continue;
            }

            return material.position;
        }
        
        return new(0, -6); // Fallback, it should never reach this
    }

    public int ConvertIndexToTier(int materialIndex) {
        if (materialIndex <= 2) {
            return 1;
        } else if (materialIndex <= 5) {
            return 2;
        }

        return 3;
    }

}