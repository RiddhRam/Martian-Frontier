using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleUpgradeBayManager : MonoBehaviour, IDataPersistence
{
    [Header("Drill Bodies")]
    [SerializeField] Sprite[] grinderBodies;
    [SerializeField] Sprite[] twinBodies;
    [SerializeField] Sprite[] viperBodies;
    [SerializeField] Sprite[] specterBodies;
    [SerializeField] Sprite[] tempestBodies;
    [SerializeField] Sprite[] boreBodies;

    private Sprite[][] allBodies;

    [Header("Drill Drillers")]
    [SerializeField] Sprite[] baseDrills;
    [SerializeField] Sprite[] wideDrills;
    [SerializeField] RuntimeAnimatorController[] boreDrills;
    [SerializeField] RuntimeAnimatorController[] boreUIDrills;

    private Sprite[][] allNormalDrills;

    [Header("For Displaying")]
    public DrillerController drillerController;
    public GameObject upgradeBayPanel;
    // Where to put the sprites
    public Image[] garageBodyImages;
    public Image[] garageDrillImages;

    public Transform displayPanel;
    private Transform drillToCopy;

    // The actual sprite to show
    private Sprite bodySprite;
    private Animator drillAnimator;

    [Header("Buttons")]
    public Image customizationsButtonImage;
    public Image upgradesButtonImage;

    [Header("Other Scripts")]
    public UIDelegation uIDelegation;
    public PlayerState playerState;
    public JoystickMovement joystickMovement;
    public GarageDelegator garageDelegator;

    private SerializableDictionary<string, int> vehicleUpgradeLevels;
    private SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;

    /*public void OnUpgradeButtonClick (string vehicleName, Transform upgradeButton, TextMeshProUGUI level, TextMeshProUGUI profit) {
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
    }*/

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Only the player vehicle can open the UI panel on their local game
        // Also only the drill can activate this pad, not the body
        if (!collision.transform.parent.parent.name.Contains("Player Vehicle") || !collision.GetComponent<DrillerController>()) {
            return;
        }

        uIDelegation.HideAll();

        // Get rid of last vehicle display and create new one that matches the current vehicle
        DestroyPreviousVehicleDisplay();
        CreateNewVehicleDisplay();

        uIDelegation.RevealElement(upgradeBayPanel);
        // Stop player from moving;
        joystickMovement.joystickVec = Vector2.zero;
    }

    // driller = the gameobject of the driller, not its panel
    // matchActiveDrill = whether or not to update the drill the player is currently using too
    private void MatchGarageDisplayToDrill(int drillerIndex, Transform driller) {
        bodySprite = GetBodySprite(drillerIndex, driller.name);
        DrillerController currentDrillerController = driller.GetChild(1).GetComponent<DrillerController>();
        
        garageBodyImages[drillerIndex].sprite = bodySprite;
        
        drillAnimator = currentDrillerController.GetComponent<Animator>();

        if (drillAnimator) {
            garageDrillImages[drillerIndex].GetComponent<Animator>().runtimeAnimatorController = GetUIDrillAnimator(driller.name);
        } 
        else {
            garageDrillImages[drillerIndex].sprite = GetDrillSprite(currentDrillerController.drillTypeIndex, driller.name);
        }
    }

    // Find the selected body sprite the user chose
    private Sprite GetBodySprite(int drillerIndex, string drillName) {

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return allBodies[drillerIndex][0];
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != allBodies[drillerIndex].Length; i++) {
            if (bodySpriteName == allBodies[drillerIndex][i].name) {
                return allBodies[drillerIndex][i];
            }
        }

        // Fallback
        return allBodies[drillerIndex][0];
    }

    // Find the selected drill animator the user chose
    private RuntimeAnimatorController GetUIDrillAnimator(string drillName) {

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return boreUIDrills[0];
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != boreUIDrills.Length; i++) {
            if (bodySpriteName == boreUIDrills[i].name) {
                return boreUIDrills[i];
            }
        }

        // Fallback
        return boreUIDrills[0];
    }

    private Sprite GetDrillSprite(int drillTypeIndex, string drillName) {

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return allNormalDrills[drillTypeIndex][0];
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != allNormalDrills[drillTypeIndex].Length; i++) {
            if (bodySpriteName == allNormalDrills[drillTypeIndex][i].name) {
                return allNormalDrills[drillTypeIndex][i];
            }
        }

        // Fallback
        return allNormalDrills[drillTypeIndex][0];
    }

    // Remove the display from the upgrade bay
    private void DestroyPreviousVehicleDisplay() {
        if (drillToCopy) {
            Destroy(drillToCopy.gameObject);
        }
    }

    // Copy the garage display and show that in upgrade bay
    private void CreateNewVehicleDisplay() {
        // Copy from garage panel
        drillToCopy = Instantiate(garageDrillImages[drillerController.drillerIndex].transform.parent.gameObject).transform;

        // Move to upgrade bay panel
        drillToCopy.SetParent(displayPanel);
        drillToCopy.localScale = new(3, 3, 3);
        
        // Reposition
        RectTransform rt = drillToCopy.GetComponent<RectTransform>();
        rt.offsetMin = new(0, rt.offsetMin.y);
        rt.offsetMax = new(0, rt.offsetMax.y);

        Vector2 pos = rt.anchoredPosition;
        pos.y = -850f;
        rt.anchoredPosition = pos;
    }

    public void LoadData(GameData data)
    {
        allBodies = new Sprite[][]
        {
            grinderBodies,
            twinBodies,
            viperBodies,
            specterBodies,
            tempestBodies,
            boreBodies
        };

        allNormalDrills = new Sprite[][] {
            baseDrills,
            wideDrills,
        };

        this.vehicleUpgradeLevels = data.vehicleUpgradeLevels;
        this.vehicleCustomizations = data.vehicleCustomizations;

        for (int i = 0; i != garageDelegator.drillers.Length; i++) {
            MatchGarageDisplayToDrill(i, garageDelegator.drillers[i].transform);
        }
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
        data.vehicleCustomizations = this.vehicleCustomizations;
    }

    public void ToggleButtonColor(bool isCustomizations) {
        if (isCustomizations) {
            customizationsButtonImage.color = new(1, 0, 0, 1);
            customizationsButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);

            upgradesButtonImage.color = new(1, 1, 1, 90/255f);
            upgradesButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);
        } else {
            customizationsButtonImage.color = new(1, 1, 1, 90/255f);
            customizationsButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);

            upgradesButtonImage.color = new(1, 0, 0, 1);
            upgradesButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);
        }
    }
}