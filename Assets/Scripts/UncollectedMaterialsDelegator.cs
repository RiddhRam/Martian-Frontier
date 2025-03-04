using UnityEngine;

public class UncollectedMaterialsDelegator : MonoBehaviour
{
    public OreDelegation oreDelegation;
    public SerializableDictionary<string, MaterialManagerData> uncollectedMaterials = new();
    private MaterialManager materialManager;

    public void RemoveMaterial(string materialID)
    {
        try {
            uncollectedMaterials.Remove(materialID);
        } catch {
        }
    }

    public void UpdateMaterial(MaterialManager materialManager, GameObject materialToAdd) {
        // If player has ores in a hauler, resets mine, switches to a driller, then switches to a different smaller hauler and tries to pick up all of previous haulers ores, this will fail
        try {
            MaterialManagerData materialManagerData = materialManager.GetMaterialManagerData();
            MaterialManagerData oldValue = uncollectedMaterials[materialManagerData.id];
            oldValue.count = materialManager.count;

            // material type stays the same, count gets updated
            uncollectedMaterials[materialManager.id] = oldValue;
        } catch {
            try {
                AddMaterial(materialToAdd, materialToAdd.transform.position, materialManager.materialIndex, materialManager.count, materialManager.drillProfitMultiplier);
            } catch {
            }//108, 129, 106 + 40
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
    }

    public System.Numerics.BigInteger GetMineValue() {
        System.Numerics.BigInteger mineValue = 0;

        foreach (var kvp in uncollectedMaterials) {
            mineValue += kvp.Value.count * oreDelegation.GetMaterialPrices()[kvp.Value.materialIndex];
        }

        return mineValue;
    }
}
