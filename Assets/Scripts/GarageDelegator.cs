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
    public GameObject haulerDisplayPanel;
    public GameObject[] drillers;
    public Sprite[] drillersImages;
    public GameObject[] haulers;
    public GameObject playerState;
    public GameObject playerVehicleDelegation;
    public GameObject UIDelegation;
    [SerializeField]
    private Color[] tierColors;
    private string activePanel = "Drillers";
    private PlayerState playerStateScript;

    void Start() {
        playerStateScript = playerState.GetComponent<PlayerState>();
        GeneratePanel("Drillers");
        GeneratePanel("Haulers");
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

    public void GeneratePanel(string panelToActivate) {
        // If drillers
        if (panelToActivate == "Drillers") {

            GameObject[] tierPanels = new GameObject[3];
            // Create a tier panel for each tier
            for (int i = 0; i != 3; i++) {
                GameObject newTierPanel = Instantiate(drillerTierPanel);
                tierPanels[i] = newTierPanel;
                Transform panelTransform = tierPanels[i].transform;
                panelTransform.SetParent(drillersContent.transform);
                // Have to make sure scale is right
                panelTransform.localScale = new(1, 1, 1);
                // Set the name
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
                long price = drillerController.GetPrice();

                GameObject newVehiclePanel = Instantiate(drillerDisplayPanel);
                Transform panelTransform = newVehiclePanel.transform;
                // Add panel to the content scroll view of the right tier panel
                // This should just be a regular panel with a photo
                panelTransform.SetParent(tierPanels[tier - 1].transform.GetChild(1));

                panelTransform.localScale = new(1, 1, 1);

                // Set the panel to the right colour
                panelTransform.GetComponent<Image>().color = tierColors[tier - 1];
                panelTransform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().color = tierColors[tier - 1];
                panelTransform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().color = tierColors[tier - 1];

                // Set the sprite, drill width, speed and name
                panelTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = drillers[i].name;
                panelTransform.GetChild(2).GetComponent<Image>().sprite = drillersImages[i];
                panelTransform.GetChild(3).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
                panelTransform.GetChild(4).GetChild(1).GetComponent<Slider>().value = drillSpeed;
                panelTransform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);

                // I made some changes, these comments might be wrong
                // Multiply the width and height of the panel image relative to the proportion of 
                // (base body width and height * new vehicle body width and height) * new vehicle game object scale
                // both values for new vehicle can be obtained from it's game object in the public arrays above
                // base body width and height: 2.89 (its also 289px)
                // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
                // multiplier = (3.80/2.89) * 1.3

                float scaleFactor = drillersImages[i].bounds.size.x / 2.89f * drillers[i].transform.localScale.x;

                panelTransform.GetChild(2).transform.localScale = new(scaleFactor, 1.16f * scaleFactor, 1);
                
                tierItems[tier-1]++;

                // Get the Buy Button component
                Button buyButton = panelTransform.GetChild(5).GetComponent<Button>();
                // Have to save it as a variable with a local scope, or else it keeps going up and out of bounds
                int index = i;
                
                // If vehicle is owned
                if (playerStateScript.CheckVehicleOwnerShip(drillers[i].name)) {
                    PurchasedVehicle(newVehiclePanel, drillers[i], 5);
                    continue;
                }

                // If not owned
                // Add an OnClick listener to the button and pass in the prefab of the vehicle
                buyButton.onClick.AddListener(() => OnBuyButtonClick(newVehiclePanel, drillers[index], 5));
            }

            float bigContentHeight = 0;
            // Resize each tier panel
            for (int i = 0; i != 3; i++) {
                Transform scrollViewContent = tierPanels[i].transform.GetChild(1);
                // Calculate the number of rows
                GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
                int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
                int rows = Mathf.CeilToInt((float) tierItems[i] / columns);

                // Resize the scroll view content height to fit the rows (top padding of tier panels + cell height * rows + vertical spacing between cell rows * (rows - 1))
                RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 100 + 1800 * rows + 40 * (rows - 1));
                tierPanels[i].GetComponent<RectTransform>().sizeDelta = new (0, contentRect.sizeDelta.y);
                bigContentHeight += contentRect.sizeDelta.y;
            }

            RectTransform bigContentRect = drillersContent.GetComponent<RectTransform>();
            // Resize the scroll view content height to fit the rows using the height of all panels and then factor in the spacing * (tiers - 1) which is (150 * 2) currently
            bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight + 150 * (tierPanels.Length - 1));
            return;
        }

        // If haulers

        for (int i = 0; i != haulers.Length; i++) {

            // Get the prefab and make its panel
            // Get its values from the prefab
            HaulerController haulerController = haulers[i].GetComponent<HaulerController>();
            int cargo = haulerController.GetMaxMaterials();
            int width = haulerController.width;
            float haulerSpeed = haulerController.GetPlayerSpeed();
            long price = haulerController.GetPrice();

            GameObject newVehiclePanel = Instantiate(haulerDisplayPanel);
            Transform panelTransform = newVehiclePanel.transform;
            panelTransform.SetParent(haulersContent.transform);

            panelTransform.localScale = new(1, 1, 1);

            // Set the sprite, hauler width, speed and name
            panelTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = haulers[i].name;
            panelTransform.GetChild(2).GetComponent<Image>().sprite = haulers[i].GetComponent<SpriteRenderer>().sprite;
            panelTransform.GetChild(3).GetChild(1).GetComponent<TextMeshProUGUI>().text = cargo.ToString();
            panelTransform.GetChild(4).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
            panelTransform.GetChild(5).GetChild(1).GetComponent<Slider>().value = haulerSpeed;
            panelTransform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);

            // I made some changes, these comments might be wrong
            // Multiply the width and height of the panel image relative to the proportion of 
            // (base body width and height * new vehicle body width and height) * new vehicle game object scale
            // both values for new vehicle can be obtained from it's game object in the public arrays above
            // base body width and height: 2.89 (its also 289px)
            // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
            // multiplier = (3.80/2.89) * 1.3

            float scaleFactor = haulers[i].GetComponent<SpriteRenderer>().sprite.bounds.size.x / 2.89f * haulers[i].transform.localScale.x;

            panelTransform.GetChild(2).transform.localScale = new(scaleFactor, 1.16f * scaleFactor, 1);

            // Get the Buy Button component
            Button buyButton = panelTransform.GetChild(6).GetComponent<Button>();
            // Have to save it as a variable with a local scope, or else it keeps going up and out of bounds
            int index = i;
            
            // If vehicle is owned
            if (playerStateScript.CheckVehicleOwnerShip(haulers[i].name)) {
                PurchasedVehicle(newVehiclePanel, haulers[i], 6);
                continue;
            }

            // If not owned
            // Add an OnClick listener to the button and pass in the prefab of the vehicle
            buyButton.onClick.AddListener(() => OnBuyButtonClick(newVehiclePanel, haulers[index], 6));
        }

        Canvas.ForceUpdateCanvases();
        // Resize the content panel
        Transform haulersTransform = haulersContent.transform;
        // Calculate the number of rows
        GridLayoutGroup haulerGridLayoutGroup = haulersTransform.GetComponent<GridLayoutGroup>();
        int haulerColumns = Mathf.Max(1, Mathf.FloorToInt(haulersTransform.GetComponent<RectTransform>().rect.width / haulerGridLayoutGroup.cellSize.x));
        int haulerRows = Mathf.CeilToInt((float) haulers.Length / haulerColumns);

        // Resize the scroll view content height to fit the rows (top padding + cell height * rows + vertical spacing between cell rows * (rows - 1))
        RectTransform haulersContentRect = haulersTransform.GetComponent<RectTransform>();
        haulersContentRect.sizeDelta = new Vector2(haulersContentRect.sizeDelta.x, 50 + 1800 * haulerRows + 40 * (haulerRows - 1));
        haulersTransform.GetComponent<RectTransform>().sizeDelta = new (0, haulersContentRect.sizeDelta.y);
    }

    public void ActivatePanel(string panelToActivate) {
        // If drillers
        if (panelToActivate == "Drillers") {
            drillersPanel.SetActive(true);
            drillersButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            drillersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = "Drillers";
            return;
        }

        // If haulers
        haulersPanel.SetActive(true);
        haulersButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        haulersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = "Haulers";
    }

    public void OnBuyButtonClick (GameObject panelPurchasingFrom, GameObject vehicle, int buyButtonIndex) {
        bool canBuy = playerStateScript.VerifyEnoughCash(vehicle);

        if (!canBuy) {
            // If not enough money display quick error, but later change this to prompt to pay money for for in game cash
            return;
        }
        if (vehicle.GetComponent<HaulerController>()) {
            playerStateScript.SubtractCash(vehicle.GetComponent<HaulerController>().GetPrice(), vehicle);
        } else {
            playerStateScript.SubtractCash(vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPrice(), vehicle);
        }
        

        // If purchase was successful
        if (playerStateScript.CheckVehicleOwnerShip(vehicle.name)) {
            PurchasedVehicle(panelPurchasingFrom, vehicle, buyButtonIndex);
            return;
        }

        // If purchase failed, reset the button scale
        StartCoroutine(panelPurchasingFrom.transform.GetChild(buyButtonIndex).GetComponent<UIButton>().ResetScale());
    }

    // The FormatPrice in PlayerState is slightly different
    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000) {
            return (price / 1_000_000_000f).ToString("0.#") + "B"; // For billions
        }
        else if (price >= 1_000_000)
        {
            return (price / 1_000_000f).ToString("0.#") + "M"; // For millions
        }
        else if (price >= 1_000)
        {
            return (price / 1_000f).ToString("0.#") + "K"; // For thousands
        }

        return price.ToString(); // For smaller numbers
    }

    public void OnDeployButtonClick (GameObject vehicle, GameObject button) {
        UIDelegation.GetComponent<UIDelegation>().HideElement(gameObject);
        UIDelegation.GetComponent<UIDelegation>().RevealAll();
        playerVehicleDelegation.GetComponent<PlayerVehicleDelegation>().SwitchVehicle(vehicle);
    }

    public void PurchasedVehicle(GameObject panelPurchasedFrom, GameObject vehiclePrefab, int buyButtonIndex) {
        // Won't need the buy button at all
        Destroy(panelPurchasedFrom.transform.GetChild(buyButtonIndex).gameObject);

        GameObject deployButtonGO = panelPurchasedFrom.transform.GetChild(buyButtonIndex + 1).gameObject;
        deployButtonGO.SetActive(true);
        // Add an OnClick listener to the button and pass in the prefab of the vehicle
        // Pass in the button too so we can reset it's scale 
        deployButtonGO.GetComponent<Button>().onClick.AddListener(() => OnDeployButtonClick(vehiclePrefab, deployButtonGO));
    }

}