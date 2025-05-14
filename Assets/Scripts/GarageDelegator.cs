using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class GarageDelegator : MonoBehaviour
{

    public Transform drillersContent;
    public GameObject drillerDisplayPanel;
    private TextMeshProUGUI[] heatLimitTexts;
    private TextMeshProUGUI[] drillerWidthTexts;
    private Slider[] drillerSpeedSliders;

    public GameObject[] drillers;

    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public UIDelegation uIDelegation;

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
                panel.GetChild(2).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = playerState.FormatPrice(price);
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