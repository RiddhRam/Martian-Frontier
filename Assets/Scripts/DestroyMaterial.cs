using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DestroyMaterial : MonoBehaviour
{
    public GameObject sliderValueText;
    public GameObject playerVehicle;
    public GameObject sliderCounter;
    public GameObject cargoCapacitySlider;
    public int amountToDestroy;
    private int indexToDestroy;
    private GameObject previouslySelectedMaterial;

    public void DestroyMaterialFunc() {
        // Get the first child of player vehicle which should be a hauler so it must have hauler controller script
        // Access the public int[] materialCount and change its material count at the index by the amount specified
        playerVehicle.transform.GetChild(0).GetComponent<HaulerController>().materialCount[indexToDestroy] -= amountToDestroy;
        previouslySelectedMaterial.GetComponent<MaterialManagerUI>().SetCount(playerVehicle.transform.GetChild(0).GetComponent<HaulerController>().materialCount[indexToDestroy]);

        if (previouslySelectedMaterial.GetComponent<MaterialManagerUI>().count <= 0) {
            Destroy(previouslySelectedMaterial.transform.parent.gameObject);
        }

        sliderCounter.GetComponent<Slider>().value = 0;
        sliderValueText.GetComponent<TextMeshProUGUI>().text = "0";
        cargoCapacitySlider.GetComponent<CargoCapacitySlider>().UpdateCapacity();
    }

    // Highlight new material and change slider values and stuff
    public void SelectMaterial(GameObject materialSelected) {
        indexToDestroy = materialSelected.GetComponent<MaterialManagerUI>().materialIndex;
        materialSelected.GetComponent<Image>().color = new Color(57f / 255f, 255f / 255f, 20f / 255f);

        if (previouslySelectedMaterial != null) {
            previouslySelectedMaterial.GetComponent<Image>().color = new Color(181f / 255f, 181f / 255f, 181f / 255f);
        }

        previouslySelectedMaterial = materialSelected;
        sliderCounter.GetComponent<Slider>().interactable = true;
        sliderCounter.GetComponent<Slider>().value = 0;
        sliderCounter.GetComponent<Slider>().maxValue = materialSelected.GetComponent<MaterialManagerUI>().count;
        gameObject.GetComponent<Button>().interactable = true;
    }

    // Have to do it this way since Sliders are broken or something
    public void ChangeAmountToDestroy() {
        int newAmount = (int) sliderCounter.GetComponent<Slider>().value;

        // In case a glitch happens where it exceeds the right value
        if (newAmount > playerVehicle.transform.GetChild(0).GetComponent<HaulerController>().materialCount[indexToDestroy]) {
            newAmount = playerVehicle.transform.GetChild(0).GetComponent<HaulerController>().materialCount[indexToDestroy];
        }

        amountToDestroy = newAmount;
        sliderValueText.GetComponent<TextMeshProUGUI>().text = amountToDestroy.ToString();
    }
}
