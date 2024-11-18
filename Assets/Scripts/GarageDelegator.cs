using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class GarageDelegator : MonoBehaviour
{
    public GameObject drillersButton;
    public GameObject drillersPanel;
    public GameObject drillersContent;
    public GameObject drillerTierPanel;
    public GameObject drillerDisplayPanel;
    public GameObject haulersButton;
    public GameObject haulersPanel;
    public GameObject haulersContent;
    public GameObject[] drillers;
    public Sprite[] drillersImages;
    public GameObject[] haulers;

    private string activePanel = "Drillers";

    void Start() {
        ActivatePanel(activePanel);
    }

    public void DeactivatePanel() {
        // If drillers
        if (activePanel == "Drillers") {
            drillersPanel.SetActive(false);
            drillersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            drillersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        // If haulers
        haulersPanel.SetActive(false);
        haulersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        haulersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
    }

    public void ActivatePanel(string panelToActivate) {
        // If drillers
        if (panelToActivate == "Drillers") {

            GameObject[] tierPanels = new GameObject[3];
            // Create a tier panel for each tier
            for (int i = 0; i != 3; i++) {
                GameObject newTierPanel = Instantiate(drillerTierPanel);
                tierPanels[i] = newTierPanel;
                Transform panelTransform = tierPanels[i].transform;
                panelTransform.SetParent(drillersContent.transform);
                panelTransform.localScale = new(1, 1, 1);
                panelTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Tier " + (i+1).ToString();
            }

            //int itemsToDisplay = 0;

            for (int i = 0; i != drillers.Length; i++) {

                // Get the prefab and make its panel
                // Get its values from the prefab
                DrillerController drillerController = drillers[i].transform.GetChild(1).GetComponent<DrillerController>();
                int width = drillerController.width;
                float drillSpeed = drillerController.GetPlayerSpeed();
                int tier = drillerController.GetDrillTier();

                GameObject newVehicleButton = Instantiate(drillerDisplayPanel);
                Transform panelTransform = newVehicleButton.transform;
                // Add panel to the content scroll view of the right tier panel
                // This should just be a regular panel with a photo
                panelTransform.SetParent(tierPanels[tier - 1].transform.GetChild(1));

                panelTransform.localScale = new(1, 1, 1);

                // Set the sprite, drill width, speed and name in that order
                panelTransform.GetChild(0).GetComponent<Image>().sprite = drillersImages[i];
                panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
                panelTransform.GetChild(3).GetComponent<Slider>().value = drillSpeed;
                panelTransform.GetChild(4).GetComponent<TextMeshProUGUI>().text = drillers[i].name;

                // Multiply the width and height of the panel image relative to the proportion of 
                // (base body width and height * new vehicle body width and height) * new vehicle game object scale
                // both values for new vehicle can be obtained from it's game object in the public arrays above
                // base body width and height: 2.89 (its also 289px)
                // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
                // The images are already scaled at 3, so multiply by 3
                // multiplier = (3.80/2.89) * 1.3 * 3

                float bodyLength = drillersImages[i].bounds.size.x / 2.89f * drillers[i].transform.localScale.x * 3;
                panelTransform.GetChild(0).transform.localScale = new(bodyLength, bodyLength, 3);
                //itemsToDisplay++;

                // Get the Button component
                //Button button = newVehicleButton.GetComponent<Button>();

                // Add an OnClick listener to the button
                //button.onClick.AddListener(() => OnVehicleButtonClick(newVehicleButton));
            }

            Activation(drillersPanel, drillersButton, "Drillers");
            return;
        }

        // If haulers
        Activation(haulersPanel, haulersButton, "Haulers");
    }

    public void Deactivation(GameObject panelToDeactivate, GameObject buttonToDeselect) {

    }

    public void Activation(GameObject panelToActivate, GameObject buttonToSelect, string panelName) {
        panelToActivate.SetActive(true);
        buttonToSelect.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        buttonToSelect.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = panelName;
    }

    public void OnVehicleButtonClick (GameObject vehicleButton) {
        Debug.Log(vehicleButton.name);
    }

}