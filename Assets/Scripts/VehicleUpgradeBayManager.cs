using System.Collections.Generic;
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

    [Header("Customizations Display")]
    public Outline[] bodyOutlines;
    public Outline[] drillOutlines;

    private Outline selectedOutline;
    private bool drillIsSelected;
    private Outline equippedBodyOutline;
    private Outline equippedDrillOutline;

    public GameObject equipButton;
    public GameObject buyButton;
    public TextMeshProUGUI gemPriceText;

    long gemPrice;

    [Header("Other Scripts")]
    public UIDelegation uIDelegation;
    public PlayerState playerState;
    public JoystickMovement joystickMovement;
    public GarageDelegator garageDelegator;

    private SerializableDictionary<string, int> vehicleUpgradeLevels;
    private SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    private List<string> customizationsOwned;

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
        GenerateCustomizationsDisplays();

        uIDelegation.RevealElement(upgradeBayPanel);
        // Stop player from moving;
        joystickMovement.joystickVec = Vector2.zero;
    }

    // driller = the gameobject of the driller, not its panel
    // matchActiveDrill = whether or not to update the drill the player is currently using too
    private void MatchGarageDisplayToDrill(int drillerIndex) {

        Transform driller = garageDelegator.drillers[drillerIndex].transform;

        (bodySprite, _) = GetBodySprite(drillerIndex, driller.name);
        DrillerController currentDrillerController = driller.GetChild(1).GetComponent<DrillerController>();
        
        garageBodyImages[drillerIndex].sprite = bodySprite;
        
        drillAnimator = currentDrillerController.GetComponent<Animator>();

        if (drillAnimator) {
            (garageDrillImages[drillerIndex].GetComponent<Animator>().runtimeAnimatorController, _, _) = GetDrillAnimator(driller.name);
        } 
        else {
            (garageDrillImages[drillerIndex].sprite, _) = GetDrillSprite(currentDrillerController.drillTypeIndex, driller.name);
        }
    }

    // Find the selected body sprite the user chose
    private (Sprite bodySprite, int spriteIndex) GetBodySprite(int drillerIndex, string drillName) {

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return (allBodies[drillerIndex][0], 0);
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != allBodies[drillerIndex].Length; i++) {
            if (bodySpriteName == allBodies[drillerIndex][i].name) {
                return (allBodies[drillerIndex][i], i);
            }
        }

        // Fallback
        return (allBodies[drillerIndex][0], 0);
    }

    // Find the selected drill animator the user chose
    private (RuntimeAnimatorController uiRuntimeAnimatorController, RuntimeAnimatorController runtimeAnimatorController, int animatorIndex) GetDrillAnimator(string drillName) {
        // Currently only bore drills are animated so it's simple

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return (boreUIDrills[0], boreDrills[0], 0);
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != boreUIDrills.Length; i++) {
            if (bodySpriteName == boreUIDrills[i].name) {
                return (boreUIDrills[i], boreDrills[i], i);
            }
        }

        // Fallback
        return (boreUIDrills[0], boreDrills[0], 0);
    }

    private (Sprite drillSprite, int spriteIndex) GetDrillSprite(int drillTypeIndex, string drillName) {

        // If they didn't choose any, return the first one, which is supposed to be Vertex
        if (!vehicleCustomizations.ContainsKey(drillName)) {
            return (allNormalDrills[drillTypeIndex][0], 0);
        }

        string bodySpriteName = vehicleCustomizations[drillName].body;

        for (int i = 0; i != allNormalDrills[drillTypeIndex].Length; i++) {
            if (bodySpriteName == allNormalDrills[drillTypeIndex][i].name) {
                return (allNormalDrills[drillTypeIndex][i], i);
            }
        }

        // Fallback
        return (allNormalDrills[drillTypeIndex][0], 0);
    }

    // Remove the display from the upgrade bay
    private void DestroyPreviousVehicleDisplay() {
        if (drillToCopy) {
            Destroy(drillToCopy.gameObject);
        }

        for (int i = 0; i != bodyOutlines.Length; i++) {
            bodyOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        }

        for (int i = 0; i != drillOutlines.Length; i++) {
            drillOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
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

    private void GenerateCustomizationsDisplays() {
        bool usesAnimatedDrill = DrillUsesAnimation();

        for (int i = 0; i != bodyOutlines.Length; i++) {
            // Reset everything
            bodyOutlines[i].effectColor = new(1, 1, 1);

            // There should be at least one customization button for each body sprite, so set it to the according one
            bodyOutlines[i].transform.GetChild(0).GetComponent<Image>().sprite = allBodies[drillerController.drillerIndex][i];

            int index = i;
            // Add button click listener
            bodyOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => SelectBody(index));
        }

        // Same as above
        for (int i = 0; i != drillOutlines.Length; i++) {
            drillOutlines[i].effectColor = new(1, 1, 1);

            // There should be at least one customization button for each drill animator or sprite
            drillOutlines[i].transform.GetChild(0).GetComponent<Animator>().runtimeAnimatorController = null;
            if (usesAnimatedDrill) {
                drillOutlines[i].transform.GetChild(0).GetComponent<Animator>().runtimeAnimatorController = boreUIDrills[i];
            } else {
                drillOutlines[i].transform.GetChild(0).GetComponent<Image>().sprite = allNormalDrills[drillerController.drillTypeIndex][i];
            }

            int index = i;
            // Add button click listener
            drillOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => SelectDrill(index));
        }

        HighlightEquippedOptions(usesAnimatedDrill);
    }

    private void SelectBody(int index) {
        // Make the last one white
        if (selectedOutline) {
            selectedOutline.effectColor = new(1, 1, 1);
        }

        // Select the new one and make it green
        selectedOutline = bodyOutlines[index];
        selectedOutline.effectColor = new(57/255f, 1, 20/255f);

        // Reset drill sprite
        drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController = null;
        if (DrillUsesAnimation()) {
            (drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController, _, _) = GetDrillAnimator(transform.parent.name);
        } else {
            (drillToCopy.GetChild(2).GetComponent<Image>().sprite, _) = GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name);
        }

        // Show preview of driller with this body
        drillToCopy.GetChild(1).GetComponent<Image>().sprite = allBodies[drillerController.drillerIndex][index];

        // If the selected one is the currently active one, disable equip and purchase button
        if (GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name).Item1 == allBodies[drillerController.drillerIndex][index]) {
            equipButton.SetActive(false);
            buyButton.SetActive(false);
        } 
        // Player owns this but not equipped
        else if (PlayerOwnsCustomization((allBodies[drillerController.drillerIndex][index].name + drillerController.transform.parent.name).ToLower())) {
            equipButton.SetActive(true);
            buyButton.SetActive(false);
        } 
        // Doesn't own and not equpped
        else {
            equipButton.SetActive(false);
            buyButton.SetActive(true);
        }

        drillIsSelected = false;
        
        // Rehighlight equipped options to be blue, in case it was made green or white
        HighlightEquippedOptions(DrillUsesAnimation());
    } 

    private void SelectDrill(int index) {
        // Make the last one white
        if (selectedOutline) {
            selectedOutline.effectColor = new(1, 1, 1);
        }

        // Select the new one and make it green
        selectedOutline = drillOutlines[index];
        selectedOutline.effectColor = new(57/255f, 1, 20/255f);

        // Reset body sprite
        (drillToCopy.GetChild(1).GetComponent<Image>().sprite, _) = GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name);
        
        // Show preview of driller with this drill
        if (DrillUsesAnimation()) {
            drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController = boreUIDrills[index];

            // If the selected one is the currently active one, disable equip and purchase button
            if (GetDrillAnimator(drillerController.transform.parent.name).uiRuntimeAnimatorController == boreUIDrills[index]) {
                equipButton.SetActive(false);
                buyButton.SetActive(false);
            } 
            // Player owns this but not equipped
            else if (PlayerOwnsCustomization((boreUIDrills[index].name + drillerController.transform.parent.name).ToLower())) {
                equipButton.SetActive(true);
                buyButton.SetActive(false);
            } 
            // Doesn't own and not equpped
            else {
                equipButton.SetActive(false);
                buyButton.SetActive(true);
            }
        } else {
            drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController = null;
            drillToCopy.GetChild(2).GetComponent<Image>().sprite = allNormalDrills[drillerController.drillTypeIndex][index];

            if (GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name).Item1 == allNormalDrills[drillerController.drillTypeIndex][index]) {
                equipButton.SetActive(false);
                buyButton.SetActive(false);
            } 
            // Player owns this but not equipped
            else if (PlayerOwnsCustomization((allNormalDrills[drillerController.drillTypeIndex][index].name + drillerController.transform.parent.name).ToLower())) {
                equipButton.SetActive(true);
                buyButton.SetActive(false);
            } 
            // Doesn't own and not equpped
            else {
                equipButton.SetActive(false);
                buyButton.SetActive(true);
            }
        }
        
        drillIsSelected = true;

        // Rehighlight equipped options to be blue, in case it was made green or white
        HighlightEquippedOptions(DrillUsesAnimation());
    }

    private void HighlightEquippedOptions(bool usesAnimatedDrill) {
        // Find the index of the players current option, and then change the outline of that options box to blue
        int selectedBody;
        (_, selectedBody) = GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name);
        equippedBodyOutline = bodyOutlines[selectedBody];
        equippedBodyOutline.effectColor = new(35/255f, 35/255f, 1);

        int selectedDrill;
        if (usesAnimatedDrill) {
            (_, _, selectedDrill) = GetDrillAnimator(drillerController.transform.parent.name);
        } else {
            (_, selectedDrill) = GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name);
        }
        equippedDrillOutline = drillOutlines[selectedDrill];
        equippedDrillOutline.effectColor = new(35/255f, 35/255f, 1);
    }

    private bool PlayerOwnsCustomization(string name) {
        // Vertex is free
        if (name.ToLower().Contains("vertex")) {
            return true;
        }

        if (customizationsOwned.Contains(name)) {
            Debug.Log("Found!");
            return true;
        }

        return false;
    }

    public void EquipCustomization() {
        
        Transform spriteTransform = selectedOutline.transform.GetChild(0);

        if (drillIsSelected) {
            
            if (DrillUsesAnimation()) {
                // Remove last customization
                drillerController.GetComponent<Animator>().runtimeAnimatorController = null;

                // Add new customization
                UpdateCustomizationDictionary(spriteTransform.GetComponent<Animator>().runtimeAnimatorController.name.Replace(" UI", ""), true);
            } else {
                UpdateCustomizationDictionary(spriteTransform.GetComponent<Image>().sprite.name, true);
            }
        } else {
            UpdateCustomizationDictionary(spriteTransform.GetComponent<Image>().sprite.name, false);
        }

        equipButton.SetActive(false);

        MatchGarageDisplayToDrill(drillerController.drillerIndex);
    }

    private void UpdateCustomizationDictionary(string customization, bool isDrill) {
        // If not in dictionary, add it and make vertex the default
        if (!vehicleCustomizations.ContainsKey(drillerController.transform.parent.name)) {
            vehicleCustomizations[drillerController.transform.parent.name] = new VehicleCustomization("vertex", "vertex");
        }

        if (isDrill) {
            vehicleCustomizations[drillerController.transform.parent.name].drill = customization;
            return;
        }

        vehicleCustomizations[drillerController.transform.parent.name].body = customization;
    }

    public void PurchaseCustomization() {
        if (!playerState.VerifyEnoughGems(gemPrice)) {
            uIDelegation.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.SubtractGems(gemPrice);

        string customizationName;

        Transform spriteTransform = selectedOutline.transform.GetChild(0);
        if (drillIsSelected) {
            if (DrillUsesAnimation()) {
                customizationName = spriteTransform.GetComponent<Animator>().runtimeAnimatorController.name;
            } else {
                customizationName = spriteTransform.GetComponent<Image>().sprite.name;
            }
        } else {
            customizationName = spriteTransform.GetComponent<Image>().sprite.name;
        }

        customizationName += drillerController.transform.parent.name;
        customizationName = customizationName.ToLower();
        
        customizationsOwned.Add(customizationName);

        buyButton.SetActive(false);

        EquipCustomization();
    }

    public bool DrillUsesAnimation() {
        if (drillerController.GetComponent<Animator>()) {
            return true;
        }

        return false;
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
        this.customizationsOwned = data.customizationsOwned;

        for (int i = 0; i != garageDelegator.drillers.Length; i++) {
            MatchGarageDisplayToDrill(i);
        }
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
        data.vehicleCustomizations = this.vehicleCustomizations;
        data.customizationsOwned = this.customizationsOwned;
    }

    public void ToggleButtonColor(bool isCustomizations) {
        if (isCustomizations) {
            customizationsButtonImage.color = new(1, 0, 0, 1);
            customizationsButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);

            upgradesButtonImage.color = new(1, 1, 1, 90/255f);
            upgradesButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);
        } 
        else {
            customizationsButtonImage.color = new(1, 1, 1, 90/255f);
            customizationsButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);

            upgradesButtonImage.color = new(1, 0, 0, 1);
            upgradesButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);
        }
    }
}