using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class GarageDelegator : MonoBehaviour, IDataPersistence
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
    public Sprite[] haulersImages;
    public Color[] tierColors;
    private string activePanel = "Drillers";

    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public AnalyticsDelegator analyticsDelegator;
    public UIDelegation uIDelegation;

    public SerializableDictionary<string, int> vehicleUpgradeLevels;
    private readonly int[] upgradeGemPrices = new int[] {20, 40, 80, 150, 240, 360, 490, 660, 840, 1000, 1300, 1500, 1800, 2100, 2400, 2800, 3200, 3600, 4000, 4400, 4900, 5400, 5900, 6400, 7000, 7600, 8200, 8800, 9500, 10200, 10900, 11600, 12400, 13100, 13900, 14700, 15600, 16500, 17400, 18300, 19200, 20200, 21200, 22200, 23200, 24300, 25300, 26400, 27600, 28700, 29900, 31100, 32300, 33600, 34800, 36100, 37400, 38800, 40100, 41500, 42900, 44400, 45800, 47300, 48800, 50300, 51900, 53500, 55100, 56700, 58300, 60000, 61700, 63400, 65200, 66900, 68700, 70500, 72400, 74200, 76100, 78000, 79900, 81900, 83900, 85900, 87900, 89900, 92000, 94100, 96200, 98400, 101000, 103000, 105000, 107000, 109000, 112000, 114000, 116000, 119000, 121000, 123000, 126000, 128000, 131000, 133000, 136000, 138000, 141000, 144000, 146000, 149000, 151000, 154000, 157000, 160000, 162000, 165000, 168000, 171000, 174000, 176000, 179000, 182000, 185000, 188000, 191000, 194000, 197000, 200000, 203000, 206000, 210000, 213000, 216000, 219000, 222000, 226000, 229000, 232000, 235000, 239000, 242000, 246000, 249000, 252000, 256000, 259000, 263000, 266000, 270000, 274000, 277000, 281000, 284000, 288000, 292000, 296000, 299000, 303000, 307000, 311000, 314000, 318000, 322000, 326000, 330000, 334000, 338000, 342000, 346000, 350000, 354000, 358000, 362000, 367000, 371000, 375000, 379000, 383000, 388000, 392000, 396000, 401000, 405000, 409000, 414000, 418000, 423000, 427000, 432000, 436000, 441000, 445000, 450000, 454000, 459000, 464000, 468000};

    public void DeactivatePanel() {
        // If drillers
        if (activePanel == "Drillers") {
            drillersPanel.SetActive(false);
            drillersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            drillersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            // Destroy and regenerate panels if needed later, in case user changes language, or rebirths
            int drillChildCount = drillersContent.transform.childCount;
            for (int i = 0; i != drillChildCount; i++) {
                Destroy(drillersContent.transform.GetChild(i).gameObject);
            }
            return;
        }

        // If haulers
        haulersPanel.SetActive(false);
        haulersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        haulersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

        int haulerChildCount = haulersContent.transform.childCount;
        for (int i = 0; i != haulerChildCount; i++) {
            Destroy(haulersContent.transform.GetChild(i).gameObject);
        }
    }

    public void GeneratePanel(string panelToActivate) {
        
        // If drillers
        if (panelToActivate == "Drillers") {

            GameObject[] tierPanels = new GameObject[tierColors.Length];
            // Create a tier panel for each tier
            for (int i = 0; i != tierPanels.Length; i++) {
                GameObject newTierPanel = Instantiate(drillerTierPanel);
                tierPanels[i] = newTierPanel;
                Transform panelTransform = tierPanels[i].transform;
                panelTransform.SetParent(drillersContent.transform);
                // Have to make sure scale is right
                panelTransform.localScale = new(1, 1, 1);
                // Get the right translation
                string tierString = GetLocalizedValue("TIER {0}", i+1);
                // Set the name
                panelTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = tierString;
            }

            // Track number of items in each tier, to dynamically resize content height based on rows
            int[] tierItems = new int[tierPanels.Length];

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

                int level = GetVehicleLevel(drillers[i].name);

                // Set the panel to the right colour
                panelTransform.GetChild(0).GetComponent<Outline>().effectColor = tierColors[tier - 1];

                // Set the sprite, drill width, speed and name
                panelTransform.GetChild(1).GetComponent<Image>().sprite = drillersImages[i];
                panelTransform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);

                if (level == upgradeGemPrices.Length) {
                    panelTransform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                    panelTransform.GetChild(4).GetChild(1).gameObject.SetActive(true);
                    panelTransform.GetChild(4).GetComponent<Button>().interactable = false;;
                } else {
                    panelTransform.GetChild(4).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(upgradeGemPrices[level]);
                }

                panelTransform.GetChild(5).GetComponent<TextMeshProUGUI>().text = drillers[i].name;
                TextMeshProUGUI levelText = panelTransform.GetChild(6).GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI profitText = panelTransform.GetChild(7).GetComponent<TextMeshProUGUI>();
                levelText.text = GetLocalizedValue("LEVEL {0}", level);
                profitText.text = GetLocalizedValue("PROFIT: +{0}%", GetVehicleProfitMultiplier(drillers[i].name) * 100);
                panelTransform.GetChild(8).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
                panelTransform.GetChild(9).GetChild(1).GetComponent<Slider>().value = drillSpeed;

                // I made some changes, these comments might be wrong
                // Multiply the width and height of the panel image relative to the proportion of 
                // (base body width and height * new vehicle body width and height) * new vehicle game object scale
                // both values for new vehicle can be obtained from it's game object in the public arrays above
                // base body width and height: 2.89 (its also 289px)
                // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
                // multiplier = (3.80/2.89) * 1.3

                float scaleFactor = drillersImages[i].bounds.size.x / 2.89f * drillers[i].transform.localScale.x;

                panelTransform.GetChild(1).transform.localScale = new(scaleFactor, 1.16f * scaleFactor, 1);
                
                tierItems[tier-1]++;

                // Get the Buy Button component
                Button buyButton = panelTransform.GetChild(2).GetComponent<Button>();
                // Have to save it as a variable with a local scope, or else it keeps going up and out of bounds
                int index = i;

                // If vehicle is owned
                if (playerState.CheckVehicleOwnerShip(drillers[i].name)) {
                    PurchasedVehicle(newVehiclePanel, drillers[i]);
                } else {
                    // If not owned
                    // Add an OnClick listener to the button and pass in the prefab of the vehicle
                    buyButton.onClick.AddListener(() => OnBuyButtonClick(newVehiclePanel, drillers[index]));
                }

                // Get the Buy Button component
                Button upgradeButton = panelTransform.GetChild(4).GetComponent<Button>();
                // Add an OnClick listener to the button and pass in the prefab of the vehicle
                upgradeButton.onClick.AddListener(() => OnUpgradeButtonClick(drillers[index].name, panelTransform.GetChild(4), levelText, profitText));
            }

            float bigContentHeight = 0;
            // Resize each tier panel
            for (int i = 0; i != tierPanels.Length; i++) {
                Transform scrollViewContent = tierPanels[i].transform.GetChild(1);
                // Calculate the number of rows
                GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
                int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
                int rows = Mathf.CeilToInt((float) tierItems[i] / columns);

                // Resize the scroll view content height to fit the rows (top padding of tier panels + cell height * rows + vertical spacing between cell rows * (rows - 1))
                RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 50 + 2500 * rows + 40 * (rows - 1));
                tierPanels[i].GetComponent<RectTransform>().sizeDelta = new (0, contentRect.sizeDelta.y);
                bigContentHeight += contentRect.sizeDelta.y;
            }

            RectTransform bigContentRect = drillersContent.GetComponent<RectTransform>();
            // Resize the scroll view content height to fit the rows using the height of all panels and then factor in the spacing * (tiers - 1) which is (150 * 2) currently
            bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, 100 + bigContentHeight + 150 * (tierPanels.Length - 1));
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
            int level = GetVehicleLevel(haulers[i].name);

            GameObject newVehiclePanel = Instantiate(haulerDisplayPanel);
            Transform panelTransform = newVehiclePanel.transform;
            panelTransform.SetParent(haulersContent.transform);

            panelTransform.localScale = new(1, 1, 1);

            // Set the sprite, hauler width, speed and name
            panelTransform.GetChild(1).GetComponent<Image>().sprite = haulersImages[i];
            panelTransform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);

            if (level == upgradeGemPrices.Length) {
                panelTransform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                panelTransform.GetChild(4).GetChild(1).gameObject.SetActive(true);
                panelTransform.GetChild(4).GetComponent<Button>().interactable = false;;
            } else {
                panelTransform.GetChild(4).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(upgradeGemPrices[level]);
            }

            panelTransform.GetChild(5).GetComponent<TextMeshProUGUI>().text = haulers[i].name;
            TextMeshProUGUI levelText = panelTransform.GetChild(6).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI profitText = panelTransform.GetChild(7).GetComponent<TextMeshProUGUI>();
            levelText.text = GetLocalizedValue("LEVEL {0}", level);
            profitText.text = GetLocalizedValue("PROFIT: +{0}%", GetVehicleProfitMultiplier(haulers[i].name) * 100);
            panelTransform.GetChild(8).GetChild(1).GetComponent<TextMeshProUGUI>().text = cargo.ToString();
            panelTransform.GetChild(9).GetChild(1).GetComponent<TextMeshProUGUI>().text = width.ToString();
            panelTransform.GetChild(10).GetChild(1).GetComponent<Slider>().value = haulerSpeed;

            // I made some changes, these comments might be wrong
            // Multiply the width and height of the panel image relative to the proportion of 
            // (base body width and height * new vehicle body width and height) * new vehicle game object scale
            // both values for new vehicle can be obtained from it's game object in the public arrays above
            // base body width and height: 2.89 (its also 289px)
            // Example (Bore I): bore body dimensions: (3.80) 380px, Scale = 1.3
            // multiplier = (3.80/2.89) * 1.3

            float scaleFactor = haulersImages[i].bounds.size.x / 2.89f * haulers[i].transform.localScale.x;

            panelTransform.GetChild(1).transform.localScale = new(scaleFactor, 1.16f * scaleFactor, 1);

            // Get the Buy Button component
            Button buyButton = panelTransform.GetChild(2).GetComponent<Button>();
            // Have to save it as a variable with a local scope, or else it keeps going up and out of bounds
            int index = i;
            
            // If vehicle is owned
            if (playerState.CheckVehicleOwnerShip(haulers[i].name)) {
                PurchasedVehicle(newVehiclePanel, haulers[i]);
            } else {
                // If not owned
                // Add an OnClick listener to the button and pass in the prefab of the vehicle
                buyButton.onClick.AddListener(() => OnBuyButtonClick(newVehiclePanel, haulers[index]));
            }

            // Get the Buy Button component
            Button upgradeButton = panelTransform.GetChild(4).GetComponent<Button>();
            // Add an OnClick listener to the button and pass in the prefab of the vehicle
            upgradeButton.onClick.AddListener(() => OnUpgradeButtonClick(haulers[index].name, panelTransform.GetChild(4), levelText, profitText));
        }

        Canvas.ForceUpdateCanvases();
        // Resize the content panel
        // Calculate the number of rows
        GridLayoutGroup haulerGridLayoutGroup = haulersContent.GetComponent<GridLayoutGroup>();
        int haulerColumns = Mathf.Max(1, Mathf.FloorToInt(haulersContent.GetComponent<RectTransform>().rect.width / haulerGridLayoutGroup.cellSize.x));
        int haulerRows = Mathf.CeilToInt((float) haulers.Length / haulerColumns);

        // Resize the scroll view content height to fit the rows (top padding + cell height * rows + vertical spacing between cell rows * (rows - 1))
        RectTransform haulersContentRect = haulersContent.GetComponent<RectTransform>();
        haulersContentRect.sizeDelta = new Vector2(haulersContentRect.sizeDelta.x, 50 + 2450 * haulerRows + 40 * (haulerRows - 1));
        haulersContent.GetComponent<RectTransform>().sizeDelta = new (0, haulersContentRect.sizeDelta.y);
    }

    public void OnUpgradeButtonClick (string vehicleName, Transform upgradeButton, TextMeshProUGUI level, TextMeshProUGUI profit) {
        int gemPrice = upgradeGemPrices[GetVehicleLevel(vehicleName)];

        if (!playerState.VerifyEnoughGems(gemPrice)) {
            // If not enough money display quick error, but later change this to prompt to pay money for for in game cash
            uIDelegation.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.SubtractGems(gemPrice);

        int newLevel = GetVehicleLevel(vehicleName);
        newLevel++;

        vehicleUpgradeLevels[vehicleName] = newLevel;

        level.text = GetLocalizedValue("LEVEL {0}", newLevel);
        profit.text = GetLocalizedValue("PROFIT: +{0}%", GetVehicleProfitMultiplier(vehicleName) * 100);

        Button button = upgradeButton.GetComponent<Button>();
        if (newLevel >= 200) {
            upgradeButton.GetChild(0).gameObject.SetActive(false);
            upgradeButton.GetChild(1).gameObject.SetActive(true);
            button.interactable = false;
        } else {
            upgradeButton.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(upgradeGemPrices[newLevel]);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnUpgradeButtonClick(vehicleName, upgradeButton, level, profit));
        }

        Transform vehicle = playerVehicleDelegation.transform.GetChild(0);
        if (vehicle.name == vehicleName) {
            float profitMultiplier = GetVehicleProfitMultiplier(vehicle.name);

            HaulerController haulerController = vehicle.GetComponent<HaulerController>();

            if (haulerController) {
                haulerController.SetProfitMultiplier(profitMultiplier);
            } else {
                vehicle.GetChild(1).GetComponent<DrillerController>().SetProfitMultiplier(profitMultiplier);
            }
        }

        analyticsDelegator.UpgradeVehicle(vehicleName, newLevel);
    }

    public void ActivatePanel(string panel) {
        // If a panel was specified use that, otherwise use the activePanel
        string panelToActivate = panel.Length != 0 ? panel : activePanel;

        GeneratePanel(panelToActivate);
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

    public void OnBuyButtonClick (GameObject panelPurchasingFrom, GameObject vehicle) {

        if (!playerState.VerifyEnoughCash(vehicle)) {
            // If not enough money display quick error, but later change this to prompt to pay money for for in game cash
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return;
        }
        if (vehicle.GetComponent<HaulerController>()) {
            playerState.SubtractCash(vehicle.GetComponent<HaulerController>().GetPrice(), vehicle);
        } else {
            playerState.SubtractCash(vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPrice(), vehicle);
        }

        // If purchase was successful
        if (playerState.CheckVehicleOwnerShip(vehicle.name)) {
            PurchasedVehicle(panelPurchasingFrom, vehicle);
            return;
        }

        // If purchase failed, reset the button scale
        StartCoroutine(panelPurchasingFrom.transform.GetChild(2).GetComponent<UIButton>().ResetScale());
    }

    // The FormatPrice in PlayerState is slightly different
    public string FormatPrice(long price) {
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

    public void OnDeployButtonClick (GameObject vehicle) {
        uIDelegation.HideElement(gameObject);
        DeactivatePanel();
        uIDelegation.RevealAll();
        playerVehicleDelegation.GetComponent<PlayerVehicleDelegation>().SwitchVehicle(vehicle);
    }

    public void PurchasedVehicle(GameObject panelPurchasedFrom, GameObject vehiclePrefab) {
        // Won't need the buy button at all
        panelPurchasedFrom.transform.GetChild(2).gameObject.SetActive(false);

        GameObject deployButtonGO = panelPurchasedFrom.transform.GetChild(3).gameObject;
        deployButtonGO.SetActive(true);
        // Add an OnClick listener to the button and pass in the prefab of the vehicle
        // Pass in the button too so we can reset it's scale 
        deployButtonGO.GetComponent<Button>().onClick.AddListener(() => OnDeployButtonClick(vehiclePrefab));
    }

    public GameObject[] GetDrillers() {
        return drillers;
    }

    public GameObject[] GetHaulers() {
        return haulers;
    }

    public void PlayerRebirth() {
        activePanel = "Haulers";
        DeactivatePanel();
        activePanel = "Drillers";
        DeactivatePanel();

        GeneratePanel("Drillers");
        GeneratePanel("Haulers");
    }

    public string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public void LoadData(GameData data)
    {
        this.vehicleUpgradeLevels = data.vehicleUpgradeLevels;

        if (playerVehicleDelegation.vehicleType == "Driller") {
            DrillerController drillerController = playerVehicleDelegation.transform.GetChild(0).GetChild(1).GetComponent<DrillerController>();
            drillerController.SetProfitMultiplier(GetVehicleProfitMultiplier(drillerController.name));
            return;
        }

        HaulerController haulerController = playerVehicleDelegation.transform.GetChild(0).GetComponent<HaulerController>();
        haulerController.SetProfitMultiplier(GetVehicleProfitMultiplier(haulerController.name));
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
    }

    public float GetVehicleProfitMultiplier(string vehicleName) {
        if (vehicleUpgradeLevels.ContainsKey(vehicleName)) {
            int currentLevel = vehicleUpgradeLevels[vehicleName];
            float multiplier = 0;

            for (int i = 1; i <= currentLevel; i++) {
                if (i % 10 == 0) {
                    multiplier += 0.1f;
                } else {
                    multiplier += 0.01f;
                }
            }

            return (float) System.Math.Round(multiplier, 2);
        }

        return 0;
    }

    public int GetVehicleLevel(string vehicleName) {
        if (vehicleUpgradeLevels.ContainsKey(vehicleName)) {
            return vehicleUpgradeLevels[vehicleName];
        }

        return 0;
    }

}