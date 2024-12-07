using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CargoCapacitySlider : MonoBehaviour
{
    private int maxMaterials;
    private int totalMaterials;
    public GameObject playerVehicle;
    public GameObject cargoCount;
    public void UpdateCapacity() {
        GameObject child = playerVehicle.transform.GetChild(0).gameObject;
        totalMaterials = child.GetComponent<HaulerController>().GetTotalMaterialCount();
        maxMaterials = child.GetComponent<HaulerController>().GetMaxMaterials();

        gameObject.GetComponent<Slider>().maxValue = maxMaterials;
        gameObject.GetComponent<Slider>().value = totalMaterials;
        cargoCount.GetComponent<TextMeshProUGUI>().text = totalMaterials.ToString();
    }
}
