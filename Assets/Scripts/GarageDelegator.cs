using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections;

public class GarageDelegator : MonoBehaviour
{
    public GameObject drillersButton;
    public GameObject drillersPanel;
    public Transform drillersContent;
    public GameObject drillerDisplayPanel;
    private TextMeshProUGUI[] heatLimitTexts;
    private TextMeshProUGUI[] drillerWidthTexts;
    private Slider[] drillerSpeedSliders;

    public GameObject haulersButton;
    public GameObject haulersPanel;
    public Transform haulersContent;
    public GameObject haulerDisplayPanel;
    private TextMeshProUGUI[] cargoTexts;
    private TextMeshProUGUI[] haulerWidthTexts;
    private Slider[] haulerSpeedSliders;

    public GameObject[] drillers;
    public Sprite[] drillersImages;

    public GameObject[] haulers;
    public Sprite[] haulersImages;

    public string activePanel = "Drillers";

    public bool openedGarage = false;

    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public AnalyticsDelegator analyticsDelegator;
    public UIDelegation uIDelegation;

    public SerializableDictionary<string, int> vehicleUpgradeLevels;
    private readonly int[] upgradeGemPrices = new int[] {20, 40, 80, 150, 240, 360, 490, 660, 840, 1000, 1300, 1500, 1800, 2100, 2400, 2800, 3200, 3600, 4000, 4400, 4900, 5400, 5900, 6400, 7000, 7600, 8200, 8800, 9500, 10200, 10900, 11600, 12400, 13100, 13900, 14700, 15600, 16500, 17400, 18300, 19200, 20200, 21200, 22200, 23200, 24300, 25300, 26400, 27600, 28700, 29900, 31100, 32300, 33600, 34800, 36100, 37400, 38800, 40100, 41500, 42900, 44400, 45800, 47300, 48800, 50300, 51900, 53500, 55100, 56700, 58300, 60000, 61700, 63400, 65200, 66900, 68700, 70500, 72400, 74200, 76100, 78000, 79900, 81900, 83900, 85900, 87900, 89900, 92000, 94100, 96200, 98400, 101000, 103000, 105000, 107000, 109000, 112000, 114000, 116000, 119000, 121000, 123000, 126000, 128000, 131000, 133000, 136000, 138000, 141000, 144000, 146000, 149000, 151000, 154000, 157000, 160000, 162000, 165000, 168000, 171000, 174000, 176000, 179000, 182000, 185000, 188000, 191000, 194000, 197000, 200000, 203000, 206000, 210000, 213000, 216000, 219000, 222000, 226000, 229000, 232000, 235000, 239000, 242000, 246000, 249000, 252000, 256000, 259000, 263000, 266000, 270000, 274000, 277000, 281000, 284000, 288000, 292000, 296000, 299000, 303000, 307000, 311000, 314000, 318000, 322000, 326000, 330000, 334000, 338000, 342000, 346000, 350000, 354000, 358000, 362000, 367000, 371000, 375000, 379000, 383000, 388000, 392000, 396000, 401000, 405000, 409000, 414000, 418000, 423000, 427000, 432000, 436000, 441000, 445000, 450000, 454000, 459000, 464000, 468000};
    private Image stubbyImage;
    public bool blockPanelSwitching;

    void Start() {
        // Haulers
        cargoTexts = new TextMeshProUGUI[haulersContent.childCount];
        haulerWidthTexts = new TextMeshProUGUI[haulersContent.childCount];
        haulerSpeedSliders = new Slider[haulersContent.childCount];

        Transform panel;

        for (int i = 0; i != haulersContent.childCount; i++) {

            panel = haulersContent.GetChild(i);

            cargoTexts[i] = panel.GetChild(5).GetChild(1).GetComponent<TextMeshProUGUI>();
            haulerWidthTexts[i] = panel.GetChild(6).GetChild(1).GetComponent<TextMeshProUGUI>();
            haulerSpeedSliders[i] = panel.GetChild(7).GetChild(1).GetComponent<Slider>();
            UpdateVehicleGarageAppearance(i, "Hauler");

            // If already purchased
            if (playerState.CheckVehicleOwnerShip(panel.name)) {
                PurchasedVehicle(panel);
            } 
            // Otherwise show the price
            else {
                long price = GetHaulerController(i).GetPrice();
                panel.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + playerState.FormatPrice(price);
            }
        }

        // Drillers
        heatLimitTexts = new TextMeshProUGUI[drillersContent.childCount];
        drillerWidthTexts = new TextMeshProUGUI[drillersContent.childCount];
        drillerSpeedSliders = new Slider[drillersContent.childCount];

        for (int i = 0; i != drillersContent.childCount; i++) {

            panel = drillersContent.GetChild(i);

            heatLimitTexts[i] = panel.GetChild(5).GetChild(1).GetComponent<TextMeshProUGUI>();
            drillerWidthTexts[i] = panel.GetChild(6).GetChild(1).GetComponent<TextMeshProUGUI>();
            drillerSpeedSliders[i] = panel.GetChild(7).GetChild(1).GetComponent<Slider>();
            UpdateVehicleGarageAppearance(i, "Driller");

            // If already purchased
            if (playerState.CheckVehicleOwnerShip(panel.name)) {
                PurchasedVehicle(panel);
            } 
            // Otherwise show the price
            else {
                long price = GetDrillerController(i).GetPrice();
                panel.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + playerState.FormatPrice(price);
            }
        }
    }

    private void UpdateVehicleGarageAppearance(int index, string vehicleType) {
        if (vehicleType == "Hauler") {
            HaulerController haulerController = GetHaulerController(index);

            cargoTexts[index].text = haulerController.GetMaxMaterials().ToString();
            haulerWidthTexts[index].text = haulerController.width.ToString();
            haulerSpeedSliders[index].value = haulerController.GetPlayerSpeed();
            return;
        }

        DrillerController drillerController = GetDrillerController(index);

        heatLimitTexts[index].text = drillerController.endurance.ToString();
        drillerWidthTexts[index].text = drillerController.width.ToString();
        drillerSpeedSliders[index].value = drillerController.GetPlayerSpeed();
    }

    private HaulerController GetHaulerController(int index) {
        return haulers[index].GetComponent<HaulerController>();
    }

    private DrillerController GetDrillerController(int index) {
        return drillers[index].transform.GetChild(1).GetComponent<DrillerController>();
    }

    public IEnumerator FlashDeployButton() {

        Color originalColor = stubbyImage.color;
        Color darkColor = originalColor * 0.7f;

        float duration = 0.5f; // time to go from original to dark and back
        float t = 0f;
        bool goingDarker = true;

        while (true)
        {
            t += Time.deltaTime / duration;

            if (goingDarker)
                stubbyImage.color = Color.Lerp(originalColor, darkColor, t);
            else
                stubbyImage.color = Color.Lerp(darkColor, originalColor, t);

            if (t >= 1f)
            {
                t = 0f;
                goingDarker = !goingDarker;
            }

            yield return null;
        }
    }

    public void ActivatePanel(string panel) {
        if (blockPanelSwitching) {
            uIDelegation.ShowError("FINISH THE TUTORIAL FIRST");
            return;
        }

        DeactivatePanel();

        // If drillers
        if (panel == "Drillers") {
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

    public void DeactivatePanel() {
        if (blockPanelSwitching) {
            return;
        }

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

    public void BuyVehicle (Transform panelPurchasingFrom) {

        GameObject vehicle = FindVehicle(panelPurchasingFrom.name);

        if (!playerState.VerifyEnoughCash(vehicle)) {
            // If not enough money display quick error, but later change this to prompt to pay money for in game cash
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
            PurchasedVehicle(panelPurchasingFrom);
            return;
        }

        // If purchase failed, reset the button scale
        StartCoroutine(panelPurchasingFrom.transform.GetChild(2).GetComponent<UIButton>().ResetScale());
    }

    private GameObject FindVehicle(string vehicleName) {
        // Iterate through all vehicles and find which vehicle it is
        // First check if user used a hauler
        (string secondaryName, bool checkSecondaryName) = playerVehicleDelegation.GetMergedVehicleName(vehicleName);

        for (int i = 0; i != haulers.Length; i++) {
            if (!vehicleName.Contains(haulers[i].name)) {
                if (!(checkSecondaryName && vehicleName.Contains(secondaryName))) {
                    continue;
                }
            }
            
            return haulers[i];
        }

        // If wasn't a hauler then it's a driller
        for (int i = 0; i != drillers.Length; i++) {
            if (!vehicleName.Contains(drillers[i].name)) {
                if (!(checkSecondaryName && vehicleName.Contains(secondaryName))) {
                    continue;
                }
            }

            return drillers[i];
        }

        return null;
    }

    public void DeployVehicle (Transform vehicle) {
        uIDelegation.HideElement(gameObject);
        uIDelegation.RevealAll();
        playerVehicleDelegation.GetComponent<PlayerVehicleDelegation>().SwitchVehicle(FindVehicle(vehicle.name));
    }

    public void PurchasedVehicle(Transform panelPurchasedFrom) {
        // Won't need the buy button anymore
        panelPurchasedFrom.GetChild(2).gameObject.SetActive(false);

        // Need the deploy button now
        panelPurchasedFrom.GetChild(3).gameObject.SetActive(true);
    }

    public GameObject[] GetDrillers() {
        return drillers;
    }

    public GameObject[] GetHaulers() {
        return haulers;
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