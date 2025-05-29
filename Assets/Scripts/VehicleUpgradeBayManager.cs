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
    [SerializeField] AudioDelegator audioDelegator;
    public bool loaded = false;

    [Header("Audio")]
    [SerializeField] AudioClip upgradeSound;
    [SerializeField] AudioSource oreSoundEffectsSource;

    [Header("For Tutorial")]
    public bool flashButton;
    public Image closeButtonImage;

    private SerializableDictionary<string, VehicleUpgrade> vehicleUpgradeLevels;
    private SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    private List<string> customizationsOwned;

    const int coolTimesPerSecond = 50; // 50 fps

    // 50-level curve: 25 000 → 3 000 000 000
    private static readonly uint[] upgradeHeatPrices = new uint[]
    {
        25_000,    32_000,    40_000,    51_000,    65_000,
        82_000,   100_000,   130_000,   170_000,   210_000,
        270_000,   350_000,   440_000,   560_000,   710_000,
        900_000, 1_100_000, 1_400_000, 1_800_000, 2_300_000,
        3_000_000, 3_800_000, 4_800_000, 6_100_000, 7_700_000,
        9_800_000,12_000_000,16_000_000,20_000_000,25_000_000,
        32_000_000,41_000_000,52_000_000,66_000_000,84_000_000,
        110_000_000,130_000_000,170_000_000,220_000_000,280_000_000,
        350_000_000,440_000_000,560_000_000,720_000_000,910_000_000,
        1_200_000_000,1_500_000_000,1_900_000_000,
        2_400_000_000,
        3_000_000_000
    };

    // 51 values
    private static readonly int[] upgradeHeatValues = new int[] {
        90,    100,   110,   130,   140,
        160,   180,   200,   220,   250,
        280,   320,   350,   400,   440,
        500,   560,   630,   700,   790,
        880,   990,   1100,  1200,  1400,
        1600,  1700,  2000,  2200,  2500,
        2800,  3100,  3500,  3900,  4300,
        4900,  5500,  6100,  6800,  7000,
        7500,  8000,  8500, 9000, 9500,
        10000, 12000, 14000, 16000, 18000, 21000
    };

    // 50-level curve: 10 000 → 1 500 000 000
    private static readonly int[] upgradeCoolPrices = new int[]
    {
        10_000,     13_000,     16_000,     21_000,     26_000,
        34_000,     43_000,     55_000,     70_000,     89_000,
        110_000,    150_000,    190_000,    240_000,    300_000,
        380_000,    490_000,    620_000,    800_000,  1_000_000,
        1_300_000,  1_700_000,  2_100_000,  2_700_000,  3_400_000,
        4_400_000,  5_600_000,  7_100_000,  9_100_000, 12_000_000,
        15_000_000, 19_000_000, 24_000_000, 31_000_000, 39_000_000,
        50_000_000, 64_000_000, 81_000_000,100_000_000,130_000_000,
        170_000_000,210_000_000,270_000_000,350_000_000,440_000_000,
        570_000_000,720_000_000,920_000_000,1_200_000_000,1_500_000_000
    };

    // 51 values
    private static readonly float[] upgradeCoolValues = new float[]
    {
        0.50f, 0.57f, 0.65f, 0.74f, 0.85f,
        0.97f, 1.1f,  1.3f,  1.4f,  1.6f,
        1.9f,  2.1f,  2.4f,  2.8f,  3.2f,
        3.6f,  4.2f,  4.8f,  5.4f,  6.2f,
        7.1f,  8.1f,  9.2f,  11f,   12f,
        14f,   16f,   18f,   21f,   24f,
        27f,   31f,   35f,   40f,   46f,
        53f,   60f,   69f,   78f,   88f,
        100f,  110f,  130f,  150f,  170f,
        190f,  220f,  250f,  290f,  320f, 350f
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
        return upgradeHeatValues[GetDrillUpgradeLevel(originalDrillerController.transform.parent.name, "Heat")];
    }

    private float GetCoolRate(DrillerController originalDrillerController) {
        // 6 per level (0.12f per update, and 50fps)
        return upgradeCoolValues[GetDrillUpgradeLevel(originalDrillerController.transform.parent.name, "Cooldown")];
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

        // Throws an out of bounds error when player uses hold to purchase, but doesn't matter because it doesn't break anything
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
            return false;
        }

        playerState.SubtractCash(upgradePrice);

        // Increment by one
        UpdateUpgradesDictionary(1, type);

        // Update displays and drill
        MatchPlayerDrillToDrill();
        UpdateUpgradeDetails(type);

        audioDelegator.PlayAudio(oreSoundEffectsSource, upgradeSound, 0.2f);

        return true;
    }

    public void UpdateUpgradeDetails(string flashPower = null) {
        int heatLevel = GetDrillUpgradeLevel(drillerController.transform.parent.name, "Heat");

        // Update heat limit
        // Slider is offset by 6
        heatUpgradeSlider.value = heatLevel + 6;

        TextMeshProUGUI heatValueText = heatUpgradeSlider.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        heatValueText.text = drillerController.endurance.ToString();

        if (flashPower == "Heat") {
            // Flash text
            if (heatValueTextCoroutine != null) {
                StopCoroutine(heatValueTextCoroutine);
            }
            heatValueTextCoroutine = StartCoroutine(FlashUpgradeValueText(heatValueText));
        }

        // If max level
        if (heatLevel >= upgradeHeatPrices.Length)
        {
            heatUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            heatUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            heatUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            heatUpgradePriceText.text = "MAX";
        }
        else
        {
            heatUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = true;
            heatUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(0, 195 / 255f, 0);
            heatUpgradePriceText.transform.parent.parent.GetComponent<ButtonAffordability>().price = upgradeHeatPrices[heatLevel];

            heatUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(true);
            heatUpgradePriceText.text = playerState.FormatPrice(upgradeHeatPrices[heatLevel]);
        }
        
        int coolLevel = GetDrillUpgradeLevel(drillerController.transform.parent.name, "Cooldown");

        // Update cool rate
        // Slider is offset by 6
        coolUpgradeSlider.value = coolLevel + 6;

        TextMeshProUGUI coolValueText = coolUpgradeSlider.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        coolValueText.text = (drillerController.GetCoolRate() * coolTimesPerSecond).ToString() + "/s";

        if (flashPower == "Cooldown") {
            // Flash text
            if (coolValueTextCoroutine != null) {
                StopCoroutine(coolValueTextCoroutine);
            }
            coolValueTextCoroutine = StartCoroutine(FlashUpgradeValueText(coolValueText));
        }
        
        // If max level
        if (coolLevel >= upgradeCoolPrices.Length) {
            coolUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = false;
            coolUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(1, 0, 0);

            coolUpgradePriceText.transform.parent.GetChild(0).gameObject.SetActive(false);
            coolUpgradePriceText.text = "MAX";
        } else {
            coolUpgradePriceText.transform.parent.parent.GetComponent<Button>().interactable = true;
            coolUpgradePriceText.transform.parent.parent.GetComponent<Image>().color = new(0, 195/255f, 0);
            coolUpgradePriceText.transform.parent.parent.GetComponent<ButtonAffordability>().price = upgradeCoolPrices[coolLevel];

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