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
    public GameObject playerState;
    public GameObject playerVehicleDelegation;
    public GameObject UIDelegation;
    private string activePanel = "Drillers";
    private PlayerState playerStateScript;

    void Start() {
        playerStateScript = playerState.GetComponent<PlayerState>();
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

            // Track number of items in each tier, to dynamically resize content height based on rows
            int[] tierItems = new int[3];

            for (int i = 0; i != drillers.Length; i++) {

                // Get the prefab and make its panel
                // Get its values from the prefab
                DrillerController drillerController = drillers[i].transform.GetChild(1).GetComponent<DrillerController>();
                int width = drillerController.width;
                float drillSpeed = drillerController.GetPlayerSpeed();
                int tier = drillerController.GetDrillTier();
                int price = drillerController.GetPrice();

                GameObject newVehiclePanel = Instantiate(drillerDisplayPanel);
                Transform panelTransform = newVehiclePanel.transform;
                // Add panel to the content scroll view of the right tier panel
                // This should just be a regular panel with a photo
                panelTransform.SetParent(tierPanels[tier - 1].transform.GetChild(1));

                panelTransform.localScale = new(1, 1, 1);

                // Set the sprite, drill width, speed and name in that order
                panelTransform.GetChild(0).GetComponent<Image>().sprite = drillersImages[i];
                panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
                panelTransform.GetChild(3).GetComponent<Slider>().value = drillSpeed;
                panelTransform.GetChild(4).GetComponent<TextMeshProUGUI>().text = drillers[i].name;
                panelTransform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);;

                // Multiply the width and height of the panel image relative to the proportion of 
                // (base body width and height * new vehicle body width and height) * new vehicle game object scale
                // both values for new vehicle can be obtained from it's game object in the public arrays above
                // base body width and height: 2.89 (its also 289px)
                // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
                // The images are already scaled at 3, so multiply by 3
                // multiplier = (3.80/2.89) * 1.3 * 3

                float bodyLength = drillersImages[i].bounds.size.x / 2.89f * drillers[i].transform.localScale.x * 3;
                panelTransform.GetChild(0).transform.localScale = new(bodyLength, bodyLength, 3);
                
                tierItems[tier-1]++;

                // Get the Buy Button component
                Button buyButton = newVehiclePanel.transform.GetChild(5).GetComponent<Button>();
                // Have to save it as a variable with a local scope, or else it keeps going up and out of bounds
                int index = i;
                
                // If vehicle is owned
                if (playerStateScript.CheckVehicleOwnerShip(drillers[i].name)) {
                    PurchasedVehicle(newVehiclePanel, drillers[i]);
                    continue;
                }

                // If not owned
                // Add an OnClick listener to the button and pass in the prefab of the vehicle
                buyButton.onClick.AddListener(() => OnDrillBuyButtonClick(newVehiclePanel, drillers[index]));
            }

            float bigContentHeight = 0;
            // Resize each tier panel
            for (int i = 0; i != 3; i++) {
                Transform scrollViewContent = tierPanels[i].transform.GetChild(1);
                // Calculate the number of rows
                GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
                int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
                int rows = Mathf.CeilToInt((float) tierItems[i] / columns);

                // Resize the scroll view content height to fit the rows (top padding + cell height * rows + vertical spacing * (rows - 1))
                RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 100 + 1200 * rows + 40 * (rows - 1));
                tierPanels[i].GetComponent<RectTransform>().sizeDelta = new (0, contentRect.sizeDelta.y);
                bigContentHeight += contentRect.sizeDelta.y;
            }

            RectTransform bigContentRect = drillersContent.GetComponent<RectTransform>();
            // Resize the scroll view content height to fit the rows using the height of all panels and then factor in the spacing * tiers - 1 (150 * 2)
            bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight + 150 * 2);

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

    public void OnDrillBuyButtonClick (GameObject panelPurchasingFrom, GameObject vehicle) {
        bool canBuy = playerStateScript.VerifyEnoughCash(vehicle);

        if (!canBuy) {
            // If not enough money display quick error, but later change this to prompt to pay money for for in game cash
            return;
        }
        playerStateScript.SubtractCash(vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPrice(), vehicle);

        if (playerStateScript.CheckVehicleOwnerShip(vehicle.name)) {
            PurchasedVehicle(panelPurchasingFrom, vehicle);
        }
    }

    private string FormatPrice(int price)
    {
        if (price >= 1_000_000)
        {
            return (price / 1_000_000f).ToString("0.#") + "m"; // For millions
        }
        else if (price >= 1_000)
        {
            return (price / 1_000f).ToString("0.#") + "k"; // For thousands
        }
        else
        {
            return price.ToString(); // For smaller numbers
        }
    }

    public void OnDeployButtonClick (GameObject vehicle) {
        UIDelegation.GetComponent<UIDelegation>().HideElement(gameObject);
        UIDelegation.GetComponent<UIDelegation>().RevealAll();
        playerVehicleDelegation.GetComponent<PlayerVehicleDelegation>().SwitchVehicle(vehicle);
    }

    public void PurchasedVehicle(GameObject panelPurchasedFrom, GameObject vehiclePrefab) {
        // Won't need the buy button at all
        Destroy(panelPurchasedFrom.transform.GetChild(5).GetComponent<Button>());

        GameObject deployButtonGO = panelPurchasedFrom.transform.GetChild(6).gameObject;
        deployButtonGO.SetActive(true);
        // Add an OnClick listener to the button and pass in the prefab of the vehicle
        deployButtonGO.GetComponent<Button>().onClick.AddListener(() => OnDeployButtonClick(vehiclePrefab));
    }

}