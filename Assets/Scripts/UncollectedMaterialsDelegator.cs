using UnityEngine;

public class UncollectedMaterialsDelegator : MonoBehaviour
{
    public SerializableDictionary<string, MaterialManagerData> uncollectedMaterials = new();

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

    public void AddMaterial(GameObject materialToAdd, Sprite materialSprite, Vector3 materialPosition, int materialIndex, int materialCount) {
        // TODO: Make a prefab for each material, that way there's no need to get component to set the sprite
        materialToAdd.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = materialSprite;
        // Need to manually put it in the right spot, do this before SetCount, so it happens before UpdateData() in MaterialManager
        materialToAdd.transform.localPosition = materialPosition;
        // add the material to the dictionary
        MaterialManager materialManager = materialToAdd.GetComponent<MaterialManager>();
        materialManager.materialIndex = materialIndex;
        materialManager.SetCount(materialCount);

        materialToAdd.transform.SetParent(transform);
        uncollectedMaterials.Add(materialManager.id, materialManager.GetMaterialManagerData());
    }
}
