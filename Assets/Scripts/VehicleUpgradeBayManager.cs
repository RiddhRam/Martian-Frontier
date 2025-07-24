using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleUpgradeBayManager : MonoBehaviour, IDataPersistence
{
    private static VehicleUpgradeBayManager _instance;
    public static VehicleUpgradeBayManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<VehicleUpgradeBayManager>();
            }
            return _instance;
        }
    }

    [Header("Driller Prefabs")]
    public GameObject grinderDrill;
    public GameObject twinDrill;
    public GameObject viperDrill;
    public GameObject boreDrill;
    public GameObject tempestDrill;
    public GameObject specterDrill;

    private GameObject[] allDrillPrefabs;

    [Header("Drill Bodies")]
    public Sprite[] grinderBodies;
    public Sprite[] twinBodies;
    public Sprite[] viperBodies;
    public Sprite[] boreBodies;
    public Sprite[] tempestBodies;
    public Sprite[] specterBodies;

    private Sprite[][] allBodies;

    [Header("Drill Drillers")]
    public Sprite[] baseDrills;
    public Sprite[] wideDrills;
    public RuntimeAnimatorController[] boreDrills;
    public RuntimeAnimatorController[] boreUIDrills;

    private Sprite[][] allNormalDrills;

    [Header("For Displaying")]
    public DrillerController drillerController;
    public GameObject[] drillUIPositions;
    public GameObject upgradeBayNoticeIcon;
    public GameObject upgradeBayOptionPrefab;
    public Transform scrollViewContent;
    public GameObject upgradeBayPanel;

    [Header("Other Scripts")]
    public PlayerState playerState;
    [SerializeField] AudioDelegator audioDelegator;
    public bool loaded = false;

    [Header("Audio")]
    [SerializeField] AudioClip upgradeSound;
    [SerializeField] AudioSource oreSoundEffectsSource;

    [Header("For Tutorial")]
    public bool flashButton;
    public Image closeButtonImage;
    public Image droneUpgradeButtonImage;
    public Image droneProfitButtonImage;

    private SerializableDictionary<string, VehicleUpgrade> vehicleUpgradeLevels;
    private SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    private List<string> customizationsOwned;

    const int coolTimesPerSecond = 50; // 50 fps

    // 51 values
    /*private static readonly int[] upgradeHeatValues = new int[] {
        60, 100, 120, 130, 150, 170, 200, 220, 250, 290,
        330, 380, 430, 490, 560, 630, 720, 820, 940, 1100,
        1200, 1400, 1600, 1800, 2000, 2300, 2600, 3000, 3400, 3900,
        4500, 5100, 5800, 6600, 7500, 8500, 9700, 11000, 13000, 14000,
        16000, 19000, 21000, 24000, 27000, 31000, 36000, 41000, 46000, 53000, 60000
    };*/

    private static readonly int[] upgradeHeatValues = new int[] {
        150, 850, 2400, 7000, 20000, 60000
    };

    // 50-level curve: 5 000 → 2 700 000 000 000
    /*private static readonly ulong[] upgradeCoolPrices = new ulong[]
    {
        2_500UL,            7_500UL,            11_000UL,           17_000UL,           26_000UL,
        39_000UL,           59_000UL,           89_000UL,           130_000UL,          200_000UL,
        300_000UL,          450_000UL,          680_000UL,          1_000_000UL,        1_500_000UL,
        2_300_000UL,        3_500_000UL,        5_300_000UL,        8_000_000UL,        12_000_000UL,
        18_000_000UL,       27_000_000UL,       41_000_000UL,       62_000_000UL,       93_000_000UL,
        140_000_000UL,      210_000_000UL,      320_000_000UL,      480_000_000UL,      720_000_000UL,
        1_100_000_000UL,    1_700_000_000UL,    2_600_000_000UL,    3_900_000_000UL,    5_900_000_000UL,
        8_900_000_000UL,    13_000_000_000UL,   20_000_000_000UL,   30_000_000_000UL,   45_000_000_000UL,
        68_000_000_000UL,   100_000_000_000UL,  150_000_000_000UL,  230_000_000_000UL,  350_000_000_000UL,
        530_000_000_000UL,  800_000_000_000UL,  1_200_000_000_000UL,1_800_000_000_000UL,2_700_000_000_000UL
    };*/

    private static readonly ulong[] upgradeCoolPrices = new ulong[]
    {
        4_000UL,                // original tier 0
        3_000_000UL,              // sum of tiers 1–10
        110_000_000UL,           // sum of tiers 11–20
        31_000_000_000UL,        // sum of tiers 21–30
        2_000_000_000_000UL,      // sum of tiers 31–40
        71_000_000_000_000UL     // sum of tiers 41–49
    };

    // 51 values
    /*private static readonly float[] upgradeCoolValues = new float[] {
        0.50f, 0.58f, 0.68f, 0.79f, 0.92f, 1.1f,  1.2f,  1.4f,  1.7f,  2f,
        2.3f,  2.7f,  3.1f,  3.6f,  4.2f,  4.9f,  5.7f,  6.6f,  7.7f,  9f,
        10f,   12f,   14f,   16f,   19f,   22f,   26f,   30f,   35f,   41f,
        48f,   56f,   65f,   75f,   88f,  100f,  120f,  140f,  160f,  190f,
        220f,  250f,  300f,  350f,  400f,  470f,  540f,  630f,  740f,  860f,
        1000f
    };*/

    private static readonly float[] upgradeCoolValues = new float[] {
        1.17f, 6.6f, 18.6f, 54.35f, 155.29f, 465.88f
    };

    // 6 values
    private static readonly ulong[] upgradeDronePrices = new ulong[]
    {
        5, 350_000UL, 40_000_000UL, 2_000_000_000UL, 100_000_000_000UL, 5_000_000_000_000UL
    };

    private static readonly string[] upgradeBenefitTypes = new string[] {
        "BUY A DRONE", "INCREASE HEAT LIMIT", "INCREASE COOLDOWN", "SPEED BOOST", "{0}X PROFITS", "{0}X {1} ORE PROFITS"
    };
    public Sprite[] upgradeBenefitImages;

    public int firstProfitUpgradePrice;

    // Used so we can track if an upgrade is available or not
    private readonly List<UpgradeBayOptionData> upgradeOptions = new();
    private HashSet<string> upgradeBayOptionsPurchased;
    private readonly List<UpgradeBayOptionDisplayData> upgradeBayOptionDisplayDatas = new();

    // Upgrade values
    private int heatLevel = 0;
    private int coolLevel = 0;
    private int droneCount = 0;
    public float speedBoost = 1f;
    private List<float> oreProfitMultipliers = new();
    private float profitMultiplier = 1;

    const string allDronesKey = "ALL DRONES";

    public void PreparePanel(bool reveal)
    {
        ClearPanel(false);

        // Generate Upgrades
        GenerateUpgradeOptionDisplays();

        // Scroll down
        scrollViewContent.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;

        if (reveal)
        {
            UIDelegation.Instance.RevealElement(upgradeBayPanel);
        }
    }

    public void ClearPanel(bool resetScale)
    {
        upgradeBayOptionDisplayDatas.Clear();
        int childCount = scrollViewContent.childCount;

        for (int i = 0; i != childCount; i++)
        {
            Destroy(scrollViewContent.GetChild(i).gameObject);
        }

        if (resetScale)
        {
            UIDelegation.Instance.HideElement(upgradeBayPanel);
            upgradeBayPanel.transform.localScale = UIDelegation.Instance.GetFullOpenScale(upgradeBayPanel.name);
        }
    }

    private void UpdatePanels()
    {
        if (scrollViewContent.childCount == 0)
        {
            return;
        }

        try
        {
            int destroyedPanels = 1;

            foreach (var option in upgradeOptions)
            {
                if (DontShowUpgrade(option))
                {
                    DestroyUpgradeOption(option.baseType);
                    destroyedPanels++;
                }
            }

            UpdateContentSize();
        }
        catch
        {
            // Reprepare if there was an error
            PreparePanel(false);
        }
    }

    private void DestroyUpgradeOption(string baseType)
    {
        for (int i = upgradeBayOptionDisplayDatas.Count - 1; i != -1; i--)
        {
            if (upgradeBayOptionDisplayDatas[i].baseType == baseType)
            {
                Destroy(upgradeBayOptionDisplayDatas[i].display);
                upgradeBayOptionDisplayDatas.RemoveAt(i);
                break;
            }
        }
    }

    public int GetHeatLimit(string keyName)
    {
        //return upgradeHeatValues[GetDrillUpgradeLevel(allDronesKey, "INCREASE HEAT LIMIT")];
        return upgradeHeatValues[heatLevel];
    }

    public float GetCoolRate(string keyName)
    {
        //return upgradeCoolValues[GetDrillUpgradeLevel(allDronesKey, "INCREASE COOLDOWN")];
        return upgradeCoolValues[coolLevel];
    }

    public int GetDroneCount()
    {
        // Number of drones = drone level
        return droneCount;
    }

    public float GetProfitMultiplier()
    {
        return profitMultiplier;
    }

    public float GetOreProfitMultiplier(int oreIndex)
    {
        return oreProfitMultipliers[oreIndex];
    }

    // Returns false if player can't afford upgrade, true otherwise
    public bool PurchaseUpgrade(UpgradeBayOptionData upgradeBayOptionData, bool alreadyOwned)
    {
        // Find upgrade price
        ulong upgradePrice = upgradeBayOptionData.price;

        // If player doesn't have enough money, and they don't already own this
        if (!playerState.VerifyEnoughCash(upgradePrice) && !alreadyOwned)
        {
            return false;
        }

        int lastSpaceIndex = upgradeBayOptionData.baseType.LastIndexOf(' ');
        string benefitType = upgradeBayOptionData.baseType.Substring(0, lastSpaceIndex);

        // Destroy the game object of what we just purchased
        DestroyUpgradeOption(upgradeBayOptionData.baseType);

        // Cooldown
        if (benefitType == "INCREASE COOLDOWN")
        {
            // Don't use upgrades that are lower value then the current one (usually happens when loading the game)
            if (coolLevel < upgradeBayOptionData.extraData[0])
            {
                coolLevel = upgradeBayOptionData.extraData[0];
            }
        }
        // Heat
        else if (benefitType == "INCREASE HEAT LIMIT")
        {
            if (heatLevel < upgradeBayOptionData.extraData[0])
            {
                heatLevel = upgradeBayOptionData.extraData[0];
            }
        }
        // Drone
        else if (benefitType == "BUY A DRONE")
        {
            StartCoroutine(SpawnNewDrone());
        }
        else if (benefitType == "SPEED BOOST")
        {
            // 10% per upgrade
            speedBoost += 0.1f;
        }
        // Double profits
        else if (benefitType == "{0}X PROFITS")
        {
            profitMultiplier *= upgradeBayOptionData.extraData[0];
        }
        else if (benefitType == "{0}X {1} ORE PROFITS")
        {
            oreProfitMultipliers[upgradeBayOptionData.extraData[1]] *= upgradeBayOptionData.extraData[0];
        }

        // Make any other changes if needed (update content size and delete any other options)
        UpdatePanels();

        // If player already owns it then we're done here
        if (alreadyOwned)
        {
            return true;
        }

        playerState.SubtractCash(upgradePrice);
        upgradeBayOptionsPurchased.Add(upgradeBayOptionData.baseType);
        audioDelegator.PlayAudio(oreSoundEffectsSource, upgradeSound, 0.2f);

        AnalyticsDelegator.Instance.VehicleUpgrade(upgradeBayOptionData.upgradeType, RefineryUpgradePad.Instance.mineRenderer.mineCount);

        return true;
    }

    public void GenerateUpgradeOptions()
    {
        int[] upgradeMilestones = RefineryUpgradePad.Instance.GetUpgradeMilestones();
        int requiredOreUpgradeLevel = RefineryUpgradePad.Instance.GetRequiredOreUpgradeLevel();

        // Min of 1, Max of 6
        // Determines how many upgrades to show. Each iteration is the same set of upgrade types
        // Difference is the price, and for some upgrade types the benefit is exponentially higher than the last iteration
        int rampUpIterationsNeeded = 1;
        for (int i = 0; i != upgradeMilestones.Length; i++)
        {
            if (requiredOreUpgradeLevel == upgradeMilestones[i])
            {
                rampUpIterationsNeeded = i;
                break;
            }
        }

        if (rampUpIterationsNeeded > 6)
        {
            rampUpIterationsNeeded = 6;
        }

        firstProfitUpgradePrice = (int)upgradeCoolPrices[0] * 5;

        // Level 1 and up
        if (rampUpIterationsNeeded >= 1)
        {
            AddNewOption("BUY A DRONE", "BUY A DRONE", upgradeDronePrices[0], 0);
            // 0 = The index of upgradeHeatValues to use or upgradeCoolValues
            AddNewOption("INCREASE HEAT LIMIT", "INCREASE HEAT LIMIT", upgradeCoolPrices[0], 1, new int[] { 1 });
            AddNewOption("INCREASE COOLDOWN", "INCREASE COOLDOWN", upgradeCoolPrices[0], 2, new int[] { 1 });
            // index 0 = profit multiplier, index 1 = ore index
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)firstProfitUpgradePrice, -1, new int[] { 2, 0 });
            AddNewOption("SPEED BOOST", "SPEED BOOST", (ulong)(firstProfitUpgradePrice * 2f), 3);
        }

        // Level 2 and up
        if (rampUpIterationsNeeded >= 2)
        {
            AddNewOption("{0}X PROFITS", "{0}X PROFITS", (ulong)(upgradeCoolPrices[0] * 13f), 4, new int[] { 2 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[0] * 17f), -1, new int[] { 2, 1 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[0] * 28f), -1, new int[] { 2, 2 });
            AddNewOption("BUY A DRONE", "BUY A DRONE", upgradeDronePrices[1], 0);
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeDronePrices[1] * 1.5f), -1, new int[] { 2, 0 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeDronePrices[1] * 2f), -1, new int[] { 2, 1 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeDronePrices[1] * 6f), -1, new int[] { 2, 2 });
            AddNewOption("SPEED BOOST", "SPEED BOOST", (ulong)(upgradeDronePrices[1] * 8f), 3);
        }

        // Level 3 and up
        if (rampUpIterationsNeeded >= 3)
        {
            AddNewOption("INCREASE HEAT LIMIT", "INCREASE HEAT LIMIT", upgradeCoolPrices[1], 1, new int[] { 2 });
            AddNewOption("INCREASE COOLDOWN", "INCREASE COOLDOWN", upgradeCoolPrices[1], 2, new int[] { 2 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[1] * 3f), -1, new int[] { 2, 3 });
            AddNewOption("BUY A DRONE", "BUY A DRONE", upgradeDronePrices[2], 0);
            AddNewOption("INCREASE HEAT LIMIT", "INCREASE HEAT LIMIT", upgradeCoolPrices[2], 1, new int[] { 3 });
            AddNewOption("INCREASE COOLDOWN", "INCREASE COOLDOWN", upgradeCoolPrices[2], 2, new int[] { 3 });
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[2] * 12f), -1, new int[] { 2, 3 });
            AddNewOption("{0}X PROFITS", "{0}X PROFITS", (ulong)(upgradeCoolPrices[2] * 20f), 4, new int[] { 2 });
        }

        // Level 4 and up
        if (rampUpIterationsNeeded >= 4)
        {
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[2] * 22f), -1, new int[] { 2, 4 });
            AddNewOption("SPEED BOOST", "SPEED BOOST", (ulong)(upgradeCoolPrices[2] * 30f), 3);
            AddNewOption("{0}X {1} ORE PROFITS", "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[2] * 40f), -1, new int[] { 2, 4 });
        }

        // Level 5 and up
        /*if (rampUpIterationsNeeded >= 5)
        {
            AddNewOption(PlayerState.Instance.GetLocalizedValue("{0}X {1} ORE PROFITS", 2, MineRenderer.Instance.selectedMaterialNames[4]), "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[2] * 60f), -1, new int[] { 2, 4 });
            // index 0 = profit multiplier, index 1 = ore index
            AddNewOption(PlayerState.Instance.GetLocalizedValue("{0}X {1} ORE PROFITS", 2, MineRenderer.Instance.selectedMaterialNames[4]), "{0}X {1} ORE PROFITS", (ulong)(upgradeCoolPrices[2] * 2000f), -1, new int[] { 2, 4 });
        }*/
    }

    private void AddNewOption(string upgradeType, string baseType, ulong price, int imageIndex, int[] extraData = null)
    {
        int optionIndex = 0;

        foreach (var upgradeOption in upgradeOptions)
        {
            // the upgrade option baseType contains an index at the end of the string (as you can see with .Add below), so use .Contains, not ==
            if (upgradeOption.baseType.Contains(baseType))
            {
                optionIndex++;
            }
        }

        upgradeOptions.Add(new(upgradeType, baseType + " " + optionIndex, price, imageIndex, extraData));
    }

    public void GenerateUpgradeOptionDisplays()
    {

        foreach (var option in upgradeOptions)
        {
            if (DontShowUpgrade(option))
            {
                continue;
            }

            string benefitType = option.upgradeType;

            GameObject upgradeBayOptionGameObject = Instantiate(upgradeBayOptionPrefab);
            UpgradeBayOption upgradeBayOption = upgradeBayOptionGameObject.GetComponent<UpgradeBayOption>();

            // Display the upgrade type
            // -1 imageIndex means to grab the image from somewhere else
            if (option.imageIndex != -1)
            {
                upgradeBayOption.upgradeBenefitTypeImage.sprite = upgradeBenefitImages[option.imageIndex];
            }
            else
            {
                // {0}X {1} ORE PROFITS is the only one that currently uses a custom image
                string oreName = MineRenderer.Instance.selectedMaterialNames[option.extraData[1]];
                OreDelegation oreDelegation = OreDelegation.Instance;
                upgradeBayOption.upgradeBenefitTypeImage.sprite = oreDelegation.materialHighResSprites[oreDelegation.GetOriginalTileIndexByName(oreName)];
            }

            // Set the description text if needed, and hide the mini icon if not needed
            if (benefitType == "INCREASE HEAT LIMIT")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType);
                upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("NEW: {0}", upgradeHeatValues[option.extraData[0]]);
                upgradeBayOption.upgradeBenefitTypeMiniImage.gameObject.SetActive(false);
            }
            else if (benefitType == "INCREASE COOLDOWN")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType);
                upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("NEW: {0}", (upgradeCoolValues[option.extraData[0]] * coolTimesPerSecond) + "/s");
                upgradeBayOption.upgradeBenefitTypeMiniImage.gameObject.SetActive(false);
            }
            else if (benefitType == "BUY A DRONE")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType);
                upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("GAIN 1 EXTRA DRONE");
                upgradeBayOption.upgradeBenefitTypeMiniImage.gameObject.SetActive(false);
            }
            else if (benefitType == "SPEED BOOST")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType);
                upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("DRONES MOVE FASTER");
                upgradeBayOption.upgradeBenefitTypeMiniImage.gameObject.SetActive(false);
            }
            else if (benefitType == "{0}X PROFITS")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType, option.extraData[0]);
                upgradeBayOption.upgradeBenefitDescriptionText.gameObject.SetActive(false);
                upgradeBayOption.upgradeBenefitTypeMiniImage.gameObject.SetActive(false);
            }
            else if (benefitType == "{0}X {1} ORE PROFITS")
            {
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType, option.extraData[0], MineRenderer.Instance.selectedMaterialNames[option.extraData[1]]);
                upgradeBayOption.upgradeBenefitDescriptionText.gameObject.SetActive(false);
            }

            // Setup button
            upgradeBayOption.button.onClick.AddListener(() => PurchaseUpgrade(option, false));
            ulong price = option.price;
            upgradeBayOption.cashPriceText.text = PlayerState.Instance.FormatPrice(price);
            upgradeBayOption.buttonAffordability.price = price;

            // For displaying properly
            upgradeBayOptionGameObject.transform.SetParent(scrollViewContent);
            upgradeBayOptionGameObject.transform.localScale = Vector3.one;

            // For tutorial
            if (option.baseType == upgradeBenefitTypes[0] + " 0")
            {
                droneUpgradeButtonImage = upgradeBayOption.button.GetComponent<Image>();
            }
            else if (option.baseType == upgradeBenefitTypes[5] + " 0")
            {
                droneProfitButtonImage = upgradeBayOption.button.GetComponent<Image>();
            }

            UpgradeBayOptionDisplayData upgradeBayOptionDisplayData = new();
            upgradeBayOptionDisplayData.display = upgradeBayOptionGameObject;
            upgradeBayOptionDisplayData.baseType = option.baseType;

            upgradeBayOptionDisplayDatas.Add(upgradeBayOptionDisplayData);
        }

        UpdateContentSize();
    }

    private void UpdateContentSize()
    {
        VerticalLayoutGroup verticalLayoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
        float bigContentHeight = upgradeBayOptionPrefab.GetComponent<RectTransform>().sizeDelta.y * upgradeBayOptionDisplayDatas.Count + verticalLayoutGroup.padding.top + verticalLayoutGroup.padding.bottom + ((upgradeBayOptionDisplayDatas.Count - 1) * verticalLayoutGroup.spacing);

        RectTransform bigContentRect = scrollViewContent.GetComponent<RectTransform>();

        // Resize the scroll view content height to fit the rows using the height of all panels
        bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight);
    }

    private bool DontShowUpgrade(UpgradeBayOptionData option)
    {
        // If this upgrade was already purchased, don't display it
        if (AlreadyPurchased(option.baseType))
        {
            return true;
        }

        // If a heat or cooldown upgrade, don't show it if player skipped a lower one
        if ((option.upgradeType == "INCREASE COOLDOWN" && coolLevel > option.extraData[0]) || (option.upgradeType == "INCREASE HEAT LIMIT" && heatLevel > option.extraData[0]))
        {       
            return true;
        }

        return false;
    }

    private IEnumerator NotifyPlayerOfUpgrades()
    {
        // If still in the tutorial, wait a bit before starting to not mix up the player
        yield return new WaitUntil(() => TutorialManager.Instance.tutorialScreenIndex > 6);

        while (true)
        {
            bool affordable = false;

            System.Numerics.BigInteger cash = PlayerState.Instance.GetUserCash();

            foreach (var optionData in upgradeOptions)
            {
                // If player can afford an upgrade, enable the notice icon, otherwise disable it
                // If price is 0, then it's not shown to the player so don't count that
                if (cash < optionData.price || optionData.price == 0 || AlreadyPurchased(optionData.baseType))
                {
                    continue;
                }

                affordable = true;
                break;
            }

            upgradeBayNoticeIcon.SetActive(affordable);

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private bool AlreadyPurchased(string optionBaseType)
    {
        if (upgradeBayOptionsPurchased.Any(purchased => purchased.Contains(optionBaseType)))
        {
            return true;
        }

        return false;
    }

    public void LoadData(GameData data)
    {

        allDrillPrefabs = new GameObject[] {
            grinderDrill,
            twinDrill,
            viperDrill,
            boreDrill,
            tempestDrill,
            specterDrill
        };

        allBodies = new Sprite[][] {
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
        this.upgradeBayOptionsPurchased = data.upgradeBayOptionsPurchased;
        this.vehicleCustomizations = data.vehicleCustomizations;
        this.customizationsOwned = data.customizationsOwned;
        // The original multipliers for each ore is 1, and there are 9 of them. The purchased upgrade multipliers are added afer this
        this.oreProfitMultipliers = Enumerable.Repeat(1.0f, 9).ToList();

        StartCoroutine(WaitForMineToLoad());
    }

    private IEnumerator WaitForMineToLoad()
    {
        // Wait for mine to load first, because we need the material names
        yield return new WaitUntil(() => MineRenderer.Instance.soloMineLoaded);

        GenerateUpgradeOptions();

        foreach (var optionPurchased in upgradeBayOptionsPurchased)
        {
            (string upgradeType, int _) = SplitOptionPurchased(optionPurchased);

            // Check if it's a removed upgrade
            if (!upgradeBenefitTypes.Contains(upgradeType))
            {
                continue;
            }

            int index = upgradeOptions.FindIndex(option => option.baseType == optionPurchased);

            if (index == -1)
            {
                continue;
            }

            // Give the user their upgrade back
            PurchaseUpgrade(upgradeOptions[index], true);
            upgradeOptions.RemoveAt(index);
        }

        StartCoroutine(NotifyPlayerOfUpgrades());

        loaded = true;
    }

    public (string upgradeType, int iteration) SplitOptionPurchased(string optionPurchased)
    {
        // Seperate the upgrade type and iteration number
        // iteration number is the last part of the string, after the last space
        int lastSpaceIndex = optionPurchased.LastIndexOf(' ');

        string upgradeType = optionPurchased.Substring(0, lastSpaceIndex);
        int iteration = int.Parse(optionPurchased.Substring(lastSpaceIndex + 1));

        return (upgradeType, iteration);
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
        data.upgradeBayOptionsPurchased = this.upgradeBayOptionsPurchased;
        data.vehicleCustomizations = this.vehicleCustomizations;
        data.customizationsOwned = this.customizationsOwned;
    }

    public GameObject[] GetAllDrillPrefabs()
    {
        return allDrillPrefabs;
    }

    // For tutorial
    public void FlashDroneUpgradeButton()
    {
        flashButton = true;

        Color originalColor = droneUpgradeButtonImage.color;
        Color darkColor = new(originalColor.r * 0.7f, originalColor.g * 0.7f, originalColor.b * 0.7f, 1);

        StartCoroutine(FlashButton(droneUpgradeButtonImage, originalColor, darkColor));
    }

    public void FlashProfitUpgradeButton()
    {
        flashButton = true;

        Color originalColor = droneProfitButtonImage.color;
        Color darkColor = new(originalColor.r * 0.7f, originalColor.g * 0.7f, originalColor.b * 0.7f, 1);

        StartCoroutine(FlashButton(droneProfitButtonImage, originalColor, darkColor));
    }

    public void FlashCloseButton()
    {
        flashButton = true;

        Color originalColor = closeButtonImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(closeButtonImage, originalColor, darkColor));
    }

    private IEnumerator FlashButton(Image buttonImage, Color originalColor, Color darkColor)
    {
        float duration = 0.5f; // time to go from original to dark and back
        float t = 0f;
        bool goingDarker = true;

        while (flashButton && buttonImage != null)
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

        if (buttonImage)
        {
            buttonImage.color = originalColor;
        }
    }

    public bool BoughtOneDroneUpgrade()
    {
        if (upgradeBayOptionsPurchased == null)
        {
            return false;
        }

        foreach (var optionPurchased in upgradeBayOptionsPurchased)
        {
            // Use .Contains and not == here, because the upgrades have the upgrade level at the end of the string, so it will not be an exact match
            if (optionPurchased.Contains("BUY A DRONE"))
            {
                return true;
            }
        }

        return false;
    }

    public bool BoughtOneOtherUpgrade()
    {
        if (upgradeBayOptionsPurchased == null)
        {
            return false;
        }

        foreach (var optionPurchased in upgradeBayOptionsPurchased)
        {
            if (!optionPurchased.Contains("BUY A DRONE"))
            {
                return true;
            }
        }

        return false;
    }

    public bool BoughtProfitUpgrade()
    {
        if (upgradeBayOptionsPurchased == null)
        {
            return false;
        }

        foreach (var optionPurchased in upgradeBayOptionsPurchased)
        {
            if (optionPurchased.Contains("PROFITS"))
            {
                return true;
            }
        }

        return false;
    }

    public Sprite[] GetAllDrillBodySprites(int drillerIndex)
    {
        return allBodies[drillerIndex];
    }

    public Sprite[] GetAllDrillDrillerSprites(int drillTypeIndex)
    {
        return allNormalDrills[drillTypeIndex];
    }

    public ulong GetUpgradePrice(string benefitType, int iteration)
    {
        ulong price;
        if (benefitType == "INCREASE HEAT LIMIT" || benefitType == "INCREASE COOLDOWN")
        {
            // Using cooldown prices for both for now
            price = upgradeCoolPrices[iteration];
        }
        else if (benefitType == "BUY A DRONE")
        {
            price = upgradeDronePrices[iteration];
        }
        else if (benefitType == "2X PROFITS")
        {
            price = (ulong)(upgradeCoolPrices[iteration] * 8f);
        }
        // ("INCREASE ORE SPAWN RATE")
        else
        {
            price = upgradeDronePrices[iteration] * 7;
        }

        // Can't be more than half the required amount to go next level
        if (price > RefineryUpgradePad.Instance.GetCashProceedAmount() * 0.5f)
        {
            price = (ulong)(RefineryUpgradePad.Instance.GetCashProceedAmount() * 0.5);
        }
        return price;
    }

    private IEnumerator SpawnNewDrone()
    {
        // Wait for loading to finish
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);
        droneCount++;
        NPCManager.Instance.CreateNPC();

        // If the first drone the player bought, make them follow it, whether or not its the tutorial level
        if (GetDroneCount() == 1)
        {
            TutorialManager.Instance.MakePlayerFollowDrone();
        }
    }
}