using UnityEngine;

public class UncollectedMaterialsDelegator : MonoBehaviour
{
    public SerializableDictionary<string, MaterialManagerData> uncollectedMaterials = new();
    private MaterialManager materialManager;

    public void RemoveMaterial(string materialID)
    {
        uncollectedMaterials.Remove(materialID);
    }

    public void UpdateMaterial(MaterialManager materialManager) {
        MaterialManagerData materialManagerData = materialManager.GetMaterialManagerData();
        MaterialManagerData oldValue = uncollectedMaterials[materialManagerData.id];
        oldValue.count = materialManager.count;

        // material type stays the stay, count gets updated
        uncollectedMaterials[materialManager.id] = oldValue;
    }

    public void AddMaterial(GameObject materialToAdd, Vector3 materialPosition, int materialIndex, int materialCount) {
        // Set its values
        // Need to manually put it in the right spot, do this before SetCount, so it happens before UpdateData() in MaterialManager
        materialToAdd.transform.localPosition = materialPosition;
        materialManager = materialToAdd.GetComponent<MaterialManager>();
        materialManager.materialIndex = materialIndex;
        materialManager.SetCount(materialCount);
        // add the material to the dictionary
        materialToAdd.transform.SetParent(transform);
        uncollectedMaterials.Add(materialManager.id, materialManager.GetMaterialManagerData());
    }
}
