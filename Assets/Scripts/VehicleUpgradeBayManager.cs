using UnityEngine.Animations;
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

    [Header("Drill Drillers")]
    [SerializeField] Sprite[] baseDrills;
    [SerializeField] Sprite[] wideDrills;
    [SerializeField] RuntimeAnimatorController[] boreDrills;
    [SerializeField] RuntimeAnimatorController[] boreUIDrills;

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
    private Sprite drillSprite;
    private Animator drillAnimator;

    [Header("Other Scripts")]
    public UIDelegation uIDelegation;
    public PlayerState playerState;
    public JoystickMovement joystickMovement;

    private SerializableDictionary<string, int> vehicleUpgradeLevels;

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
        MatchGarageDisplayToDrill();
        CreateNewVehicleDisplay();

        uIDelegation.RevealElement(upgradeBayPanel);
        // Stop player from moving;
        joystickMovement.joystickVec = Vector2.zero;
    }

    private void MatchGarageDisplayToDrill() {
        bodySprite = drillerController.transform.parent.GetChild(0).GetComponent<SpriteRenderer>().sprite;
        
        garageBodyImages[drillerController.drillerIndex].sprite = bodySprite;
        
        drillAnimator = drillerController.GetComponent<Animator>();

        if (drillAnimator) {
            garageDrillImages[drillerController.drillerIndex].GetComponent<Animator>().runtimeAnimatorController = FindUIAnimator(drillAnimator.runtimeAnimatorController);
        } 
        else {
            drillSprite = drillerController.GetComponent<SpriteRenderer>().sprite;
        
            garageDrillImages[drillerController.drillerIndex].sprite = drillSprite;
        }
    }

    private RuntimeAnimatorController FindUIAnimator(RuntimeAnimatorController controllerToMatch) {
        for (int i = 0; i != boreDrills.Length; i++) {
            if (boreDrills[i] == controllerToMatch) {
                return boreUIDrills[i];
            }
        }

        return null;
    }

    private void DestroyPreviousVehicleDisplay() {
        if (drillToCopy) {
            Destroy(drillToCopy.gameObject);
        }
    }

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
        pos.y = -915f;
        rt.anchoredPosition = pos;
    }

    public void LoadData(GameData data)
    {
        this.vehicleUpgradeLevels = data.vehicleUpgradeLevels;
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
    }
}