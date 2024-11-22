using System.Collections.Generic;
using UnityEngine;

public class UncollectedMaterialsDelegator : MonoBehaviour
{
    public SerializableDictionary<string, MaterialManagerData> uncollectedMaterials;

    void Awake() {
        uncollectedMaterials = new();
    }

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

    public void AddMaterial(GameObject materialToAdd) {

        materialToAdd.transform.SetParent(transform);

        // add the material to the dictionary
        MaterialManager materialManager = materialToAdd.GetComponent<MaterialManager>();
        uncollectedMaterials.Add(materialManager.id, materialManager.GetMaterialManagerData());
    }
}
