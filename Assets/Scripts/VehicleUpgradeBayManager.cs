using System.Collections;
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
    [SerializeField] Sprite[] boreBodies;
    [SerializeField] Sprite[] tempestBodies;
    [SerializeField] Sprite[] specterBodies;

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
    public GameObject[] drillUIPositions;

    public Transform displayPanel;
    private Transform drillToCopy;

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

    long customizationGemPrice;

    [Header("Upgrades Display")]
    public Slider heatUpgradeSlider;
    public Slider coolUpgradeSlider;

    public TextMeshProUGUI heatUpgradePriceText;
    public TextMeshProUGUI coolUpgradePriceText;

    [Header("Other Scripts")]
    public UIDelegation uIDelegation;
    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public bool loaded = false;

    [Header("For Tutorial")]
    public bool flashButton;
    public Image closeButtonImage;

    private SerializableDictionary<string, VehicleUpgrade> vehicleUpgradeLevels;
    private SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    private List<string> customizationsOwned;

    const int heatBonusPerLevel = 10; // 10 endurance per level

    const int coolTimesPerSecond = 50; // 50 fps
    const float coolPerFrame = 0.12f; // 0.12f * 50 = 6 second per level

    // 50-level curve: 5 000 → 1 500 000, total ≈ 10 118 400

    private static readonly int[] upgradeHeatPrices = new int[]
    {
        5_000, 5_200, 5_300, 5_600, 5_800,
        6_100, 6_400, 6_800, 7_300, 7_800,
        8_400, 9_200, 10_000, 11_000, 12_100,
        13_400, 15_000, 16_900, 19_100, 21_700,
        24_700, 28_200, 32_200, 32_800, 38_000,
        44_000, 50_800, 58_500, 67_400, 77_700,
        89_600, 103_700, 120_000, 138_900, 160_700,
        185_600, 215_500, 250_200, 290_500, 337_200,
        345_300, 400_900, 465_400, 540_300, 627_300,
        728_300, 845_500, 981_600, 1_139_500, 1_500_000
    };

    // 50-level curve: 1 000 → 500 000, total ≈ 5 000 600
    private static readonly int[] upgradeCoolPrices = new int[]
    {
        1000, 1200, 1300, 1500, 1800, 2100, 2400, 2700, 3200, 3700,
        4200, 4900, 5600, 6500, 7500, 8700, 10000, 11600, 13400, 15500,
        17900, 20600, 23800, 27500, 31800, 36700, 42400, 48900, 56500, 65300,
        75400, 83300, 91900, 101500, 112000, 123700, 136500, 150700, 166400, 183700,
        202800, 223900, 247200, 272900, 301300, 332700, 367300, 405400, 447600, 494200
    };

    private Coroutine heatValueTextCoroutine;
    private Coroutine coolValueTextCoroutine;

    void Awake()
    {
        // Set heat button click listener and hold to purchase
        Transform heatButton = heatUpgradePriceText.transform.parent.parent;
        heatButton.GetComponent<Button>().onClick.AddListener(() => PurchaseUpgrade("Heat"));

        HoldButton heatHoldButton = heatButton.gameObject.AddComponent<HoldButton>();
        heatHoldButton.SetAction(() => PurchaseUpgrade("Heat"));

        // Set cool button click listener and hold to purchase
        Transform coolButton = coolUpgradePriceText.transform.parent.parent;
        coolButton.GetComponent<Button>().onClick.AddListener(() => PurchaseUpgrade("Cooldown"));

        HoldButton coolHoldButton = coolButton.gameObject.AddComponent<HoldButton>();
        coolHoldButton.SetAction(() => PurchaseUpgrade("Cooldown"));
    }

    void OnTriggerEnter2D(Collider2D collision) {
        // Only the player vehicle can open the UI panel on their local game
        // Also only the drill can activate this pad, not the body
        if (!collision.transform.parent.parent.name.Contains("Player Vehicle") || !collision.GetComponent<DrillerController>()) {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;

        uIDelegation.HideAll();

        // Customizations
        DestroyPreviousVehicleDisplay();
        CreateNewVehicleDisplay();
        GenerateCustomizationsDisplays();

        // Upgrades
        UpdateUpgradeDetails();

        uIDelegation.RevealElement(upgradeBayPanel);

        // Stop player from moving;
        JoystickMovement.Instance.joystickVec = Vector2.zero;
    }

    public void MatchPlayerDrillToDrill() {

        // Match drill
        if (DrillUsesAnimation()) {
            (_, drillerController.GetComponent<Animator>().runtimeAnimatorController, _) = GetDrillAnimator(drillerController.transform.parent.name);
        } 
        else {
            (drillerController.GetComponent<SpriteRenderer>().sprite, _) = GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name);
        }

        // Match body
        (drillerController.transform.parent.GetChild(0).GetComponent<SpriteRenderer>().sprite, _) = GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name);

        // Used so we can get the base values
        DrillerController originalDrillerController = playerVehicleDelegation.drillers[drillerController.drillerIndex].transform.GetChild(1).GetComponent<DrillerController>();
        
        drillerController.endurance = GetHeatLimit(originalDrillerController);
        drillerController.SetCoolRate(GetCoolRate(originalDrillerController));
    }

    private int GetDrillUpgradeLevel(string drillName, string upgradeType) {
        if (!vehicleUpgradeLevels.ContainsKey(drillName)) {
            return 0;
        }

        if (upgradeType == "Cooldown") {
            return vehicleUpgradeLevels[drillName].coolLevel;
        }

        return vehicleUpgradeLevels[drillName].heatLevel;
    }

    private int GetHeatLimit(DrillerController originalDrillerController) {
        // Starts at 90
        // 10 endurance per level
        return 90 + (heatBonusPerLevel * GetDrillUpgradeLevel(originalDrillerController.transform.parent.name, "Heat"));
    }

    private float GetCoolRate(DrillerController originalDrillerController) {
        // 6 per level (0.12f per update, and 50fps)
        return originalDrillerController.GetCoolRate() + (coolPerFrame * GetDrillUpgradeLevel(originalDrillerController.transform.parent.name, "Cooldown"));
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

        string bodySpriteName = vehicleCustomizations[drillName].drill;

        for (int i = 0; i != boreDrills.Length; i++) {
            if (bodySpriteName == boreDrills[i].name) {
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

        string bodySpriteName = vehicleCustomizations[drillName].drill;

        for (int i = 0; i != allNormalDrills[drillTypeIndex].Length; i++) {
            if (bodySpriteName == allNormalDrills[drillTypeIndex][i].name) {
                return (allNormalDrills[drillTypeIndex][i], i);
            }
        }

        // Fallback
        return (allNormalDrills[drillTypeIndex][0], 0);
    }
    
    // Remove the display from the upgrade bay
    private void DestroyPreviousVehicleDisplay()
    {
        if (drillToCopy)
        {
            Destroy(drillToCopy.gameObject);
        }

        for (int i = 0; i != bodyOutlines.Length; i++)
        {
            bodyOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        }

        for (int i = 0; i != drillOutlines.Length; i++)
        {
            drillOutlines[i].transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        }
    }

    // Copy the premade display and show that in upgrade bay
    public void CreateNewVehicleDisplay()
    {
        // Instantiate premade positions for the sprite
        drillToCopy = Instantiate(drillUIPositions[drillerController.drillerIndex]).transform;

        // Move to upgrade bay panel
        drillToCopy.SetParent(displayPanel);
        drillToCopy.localScale = new(2f, 2f, 2f);

        // Reposition
        RectTransform rt = drillToCopy.GetComponent<RectTransform>();
        rt.offsetMin = new(0, rt.offsetMin.y);
        rt.offsetMax = new(0, rt.offsetMax.y);

        Vector2 pos = rt.anchoredPosition;
        pos.y = -1400f;
        rt.anchoredPosition = pos;

        // Set body
        (drillToCopy.GetChild(1).GetComponent<Image>().sprite, _) = GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name);

        string drillName = drillerController.transform.parent.name;
        // Set drill
        if (DrillUsesAnimation())
        {
            (drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController, _, _) = GetDrillAnimator(drillName);
        }
        else
        {
            (drillToCopy.GetChild(2).GetComponent<Image>().sprite, _) = GetDrillSprite(drillerController.drillTypeIndex, drillName);
        }

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
            (drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController, _, _) = GetDrillAnimator(drillerController.transform.parent.name);
        } else {
            (drillToCopy.GetChild(2).GetComponent<Image>().sprite, _) = GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name);
        }

        // Show preview of driller with this body
        drillToCopy.GetChild(1).GetComponent<Image>().sprite = allBodies[drillerController.drillerIndex][index];

        // If the selected one is the currently active one, disable equip and purchase button
        if (GetBodySprite(drillerController.drillerIndex, drillerController.transform.parent.name).bodySprite == allBodies[drillerController.drillerIndex][index]) {
            equipButton.SetActive(false);
            buyButton.SetActive(false);
        } 
        // Player owns this but not equipped
        else if (PlayerOwnsCustomization((allBodies[drillerController.drillerIndex][index].name + drillerController.transform.parent.name).ToLower())) {
            equipButton.SetActive(true);
            buyButton.SetActive(false);
        } 
        // Doesn't own and not equipped
        else {
            if (allBodies[drillerController.drillerIndex][index].name.Contains("Surge")) {
                UpdateCustomizationGemPrice(30_000);
            } else if (allBodies[drillerController.drillerIndex][index].name.Contains("Cryo")) {
                UpdateCustomizationGemPrice(75_000);
            }

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
        
        long surgePrice = 60_000;
        long cryoPrice = 150_000;
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
                if (boreUIDrills[index].name.Contains("Surge")) {
                    UpdateCustomizationGemPrice(surgePrice);
                } else if (boreUIDrills[index].name.Contains("Cryo")) {
                    UpdateCustomizationGemPrice(cryoPrice);
                }

                equipButton.SetActive(false);
                buyButton.SetActive(true);
            }
        } else {
            drillToCopy.GetChild(2).GetComponent<Animator>().runtimeAnimatorController = null;
            drillToCopy.GetChild(2).GetComponent<Image>().sprite = allNormalDrills[drillerController.drillTypeIndex][index];

            if (GetDrillSprite(drillerController.drillTypeIndex, drillerController.transform.parent.name).drillSprite == allNormalDrills[drillerController.drillTypeIndex][index]) {
                equipButton.SetActive(false);
                buyButton.SetActive(false);
            } 
            // Player owns this but not equipped
            else if (PlayerOwnsCustomization((allNormalDrills[drillerController.drillTypeIndex][index].name + drillerController.transform.parent.name).ToLower())) {
                equipButton.SetActive(true);
                buyButton.SetActive(false);
            } 
            // Doesn't own and not equipped
            else {
                if (allNormalDrills[drillerController.drillTypeIndex][index].name.Contains("Surge")) {
                    UpdateCustomizationGemPrice(surgePrice);
                } else if (allNormalDrills[drillerController.drillTypeIndex][index].name.Contains("Cryo")) {
                    UpdateCustomizationGemPrice(cryoPrice);
                }

                equipButton.SetActive(false);
                buyButton.SetActive(true);
            }
        }
        
        drillIsSelected = true;

        // Rehighlight equipped options to be blue, in case it was made green or white
        HighlightEquippedOptions(DrillUsesAnimation());
    }

    private void UpdateCustomizationGemPrice(long newPrice) {
        customizationGemPrice = newPrice;
        gemPriceText.text = playerState.FormatPrice(customizationGemPrice);
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

    private bool PlayerOwnsCustomization(string drillName) {
        // Vertex is free
        if (drillName.ToLower().Contains("vertex")) {
            return true;
        }

        if (customizationsOwned.Contains(drillName)) {
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
                // For animations we could find the SpriteRenderer animator controller instead of the Image one
                // But the only difference in the name is " UI", so just use this and replace it
                UpdateCustomizationDictionary(spriteTransform.GetComponent<Animator>().runtimeAnimatorController.name.Replace(" UI", ""), true);
            } else {
                UpdateCustomizationDictionary(spriteTransform.GetComponent<Image>().sprite.name, true);
            }
        } else {
            UpdateCustomizationDictionary(spriteTransform.GetComponent<Image>().sprite.name, false);
        }

        equipButton.SetActive(false);

        GenerateCustomizationsDisplays();
        MatchPlayerDrillToDrill();
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

    private void UpdateUpgradesDictionary(int upgrade, string type) {
        // Make a new one if non existent and initialize to 0
        if (!vehicleUpgradeLevels.ContainsKey(drillerController.transform.parent.name)) {
            vehicleUpgradeLevels[drillerController.transform.parent.name] = new VehicleUpgrade(0, 0);
        }

        // Change by amount
        if (type == "Cooldown") {
            vehicleUpgradeLevels[drillerController.transform.parent.name].coolLevel += upgrade;
            return;
        }

        vehicleUpgradeLevels[drillerController.transform.parent.name].heatLevel += upgrade;
    }

    public void PurchaseCustomization() {
        if (!playerState.VerifyEnoughGems(customizationGemPrice)) {
            uIDelegation.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.SubtractGems(customizationGemPrice);

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

    // Returns false if player can't afford upgrade, true otherwise
    public bool PurchaseUpgrade(string type)
    {
        // Find upgrade price
        long upgradePrice;

        if (type == "Cooldown")
        {
            upgradePrice = upgradeCoolPrices[GetDrillUpgradeLevel(drillerController.transform.parent.name, "Cooldown")];
        }
        else
        {
            upgradePrice = upgradeHeatPrices[GetDrillUpgradeLevel(drillerController.transform.parent.name, "Heat")];
        }

        // Transact amount
        if (!playerState.VerifyEnoughCash(upgradePrice))
        {
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return false;
        }

        playerState.SubtractCash(upgradePrice);

        // Increment by one
        UpdateUpgradesDictionary(1, type);

        // Update displays and drill
        MatchPlayerDrillToDrill();
        UpdateUpgradeDetails(type);

        return true;
    }

    public void UpdateUpgradeDetails(string flashPower = null) {
        // Update heat limit
        heatUpgradeSlider.value = drillerController.endurance;

        TextMeshProUGUI heatValueText =  heatUpgradeSlider.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        heatValueText.text = drillerController.endurance.ToString();

        if (flashPower == "Heat") {
            // Flash text
            if (heatValueTextCoroutine != null) {
                StopCoroutine(heatValueTextCoroutine);
            }
            heatValueTextCoroutine = StartCoroutine(FlashUpgradeValueText(heatValueText));
        }

        int heatLevel = GetDrillUpgradeLevel(drillerController.transform.parent.name, "Heat");
        
        // If max level
        if (heatLevel >= upgradeHeatPrices.Length) {
            heatUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            heatUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            heatUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            heatUpgradePriceText.text = "MAX";
        } else {
            heatUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = true;
            heatUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(0, 195/255f, 0);

            heatUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(true);
            heatUpgradePriceText.text = playerState.FormatPrice(upgradeHeatPrices[heatLevel]);
        }
        
        // Update cool rate
        coolUpgradeSlider.value = drillerController.GetCoolRate() * coolTimesPerSecond;

        TextMeshProUGUI coolValueText =  coolUpgradeSlider.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        coolValueText.text = (drillerController.GetCoolRate() * coolTimesPerSecond).ToString() + "/s";

        if (flashPower == "Cooldown") {
            // Flash text
            if (coolValueTextCoroutine != null) {
                StopCoroutine(coolValueTextCoroutine);
            }
            coolValueTextCoroutine = StartCoroutine(FlashUpgradeValueText(coolValueText));
        }
        
        int coolLevel = GetDrillUpgradeLevel(drillerController.transform.parent.name, "Cooldown");
        // If max level
        if (coolLevel >= upgradeCoolPrices.Length) {
            coolUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            coolUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            coolUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            coolUpgradePriceText.text = "MAX";
        } else {
            coolUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = true;
            coolUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(0, 195/255f, 0);

            coolUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(true);
            coolUpgradePriceText.text = playerState.FormatPrice(upgradeCoolPrices[coolLevel]);
        }

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
            boreBodies,
            tempestBodies,
            specterBodies,
        };

        allNormalDrills = new Sprite[][] {
            baseDrills,
            wideDrills,
        };

        this.vehicleUpgradeLevels = data.vehicleUpgradeLevels;
        this.vehicleCustomizations = data.vehicleCustomizations;
        this.customizationsOwned = data.customizationsOwned;

        // In production this loads after PlayerVehicleDelegation for some reason, whichever loads second should call the function
        if (playerVehicleDelegation.loaded) {
            MatchPlayerDrillToDrill();
        }

        loaded = true;
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

    // For tutorial
    public void FlashUpgradeButton() {
        flashButton = true;
        Image heatUpgradeButtonImage = heatUpgradePriceText.transform.parent.parent.GetComponent<Image>();

        Color originalColor = heatUpgradeButtonImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(heatUpgradeButtonImage, originalColor, darkColor));
    }

    public void FlashCloseButton() {
        flashButton = true;

        Color originalColor = closeButtonImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(closeButtonImage, originalColor, darkColor));
    }

    private IEnumerator FlashUpgradeValueText(TextMeshProUGUI valueText) {
        Color start = new(9/255f, 176/255f, 9/255f);
        Color target = Color.black;

        valueText.color = start;

        // Wait 1 seconds
        yield return new WaitForSecondsRealtime(0.5f);

        // Transition from white to black
        float time = 0f;
        while (time < 0.5f)
        {
            time += Time.unscaledDeltaTime;
            valueText.color = Color.Lerp(start, target, time / 0.5f);
            yield return null;
        }

        // Ensure it’s fully invisible
        valueText.color = target;
    }

    private IEnumerator FlashButton(Image buttonImage, Color originalColor, Color darkColor) {
        float duration = 0.5f; // time to go from original to dark and back
        float t = 0f;
        bool goingDarker = true;

        while (flashButton)
        {
            t += Time.deltaTime / duration;

            if (goingDarker)
                buttonImage.color = Color.Lerp(originalColor, darkColor, t);
            else
                buttonImage.color = Color.Lerp(darkColor, originalColor, t);

            if (t >= 1f)
            {
                t = 0f;
                goingDarker = !goingDarker;
            }

            yield return null;
        }

        buttonImage.color = originalColor;
    }

    public bool BoughtOneUpgrade() {
        foreach (var key in vehicleUpgradeLevels.Keys) {
            if (vehicleUpgradeLevels[key].heatLevel > 0 || vehicleUpgradeLevels[key].coolLevel > 0) {
                return true;
            }
        }

        return false;
    }
}