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

    public GameObject[] drillers;

    public bool openedGarage = false;

    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public AnalyticsDelegator analyticsDelegator;
    public UIDelegation uIDelegation;

    private Image stubbyImage;
    public bool blockPanelSwitching;

    void Start() {
        // Drillers
        heatLimitTexts = new TextMeshProUGUI[drillersContent.childCount];
        drillerWidthTexts = new TextMeshProUGUI[drillersContent.childCount];
        drillerSpeedSliders = new Slider[drillersContent.childCount];

        Transform panel;

        for (int i = 0; i != drillersContent.childCount; i++) {

            panel = drillersContent.GetChild(i);

            heatLimitTexts[i] = panel.GetChild(5).GetChild(1).GetComponent<TextMeshProUGUI>();
            drillerWidthTexts[i] = panel.GetChild(6).GetChild(1).GetComponent<TextMeshProUGUI>();
            drillerSpeedSliders[i] = panel.GetChild(7).GetChild(1).GetComponent<Slider>();
            UpdateVehicleGarageAppearance(i);

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

    private void UpdateVehicleGarageAppearance(int index) {
        DrillerController drillerController = GetDrillerController(index);

        // Heat limit is handled by VehicleUpgradeBayManager so only change width and speed here
        drillerWidthTexts[index].text = drillerController.width.ToString();
        drillerSpeedSliders[index].value = drillerController.GetPlayerSpeed();
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

    public void BuyVehicle (Transform panelPurchasingFrom) {

        GameObject vehicle = FindVehicle(panelPurchasingFrom.name);

        if (!playerState.VerifyEnoughCash(vehicle)) {
            // If not enough money display quick error, but later change this to prompt to pay money for in game cash
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return;
        }

        playerState.SubtractCash(vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPrice(), vehicle);

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

}