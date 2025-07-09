using System.Collections;
using System.Collections.Generic;
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
        2_500UL,                // original tier 0
        870_000UL,              // sum of tiers 1–10
        53_000_000UL,           // sum of tiers 11–20
        3_200_000_000UL,        // sum of tiers 21–30
        200_000_000_000UL,      // sum of tiers 31–40
        7_900_000_000_000UL     // sum of tiers 41–49
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
        0.7f, 6.6f, 22f, 75f, 250f, 860f
    };

    // 6 values
    private static readonly ulong[] upgradeDronePrices = new ulong[]
    {
        5, 60_000UL, 3_500_000UL, 210_000_000UL, 13_000_000_000UL, 530_000_000_000UL
    };

    private static readonly string[] upgradeBenefitTypes = new string[] {
        "BUY A DRONE", "INCREASE HEAT LIMIT", "INCREASE COOLDOWN", "2X PROFITS"
    };
    public Sprite[] upgradeBenefitImages;

    // Used so we can track if an upgrade is available or not
    private readonly List<ButtonAffordability> buttonAffordabilities = new();
    private readonly List<UpgradeBayOptionData> upgradeOptions = new();

    const string allDronesKey = "ALL DRONES";

    public void PreparePanel()
    {
        ClearPanel();

        // Generate Upgrades
        GenerateUpgradeOptionDisplays();

        // Stop player from moving;
        //JoystickMovement.Instance.joystickVec = Vector2.zero;
    }

    public void ClearPanel()
    {
        int childCount = scrollViewContent.childCount;

        for (int i = 0; i != childCount; i++)
        {
            Destroy(scrollViewContent.GetChild(i).gameObject);
        }

        buttonAffordabilities.Clear();
        upgradeOptions.Clear();
    }

    private int GetDrillUpgradeLevel(string drillName, string upgradeType)
    {
        drillName = allDronesKey;

        if (!vehicleUpgradeLevels.ContainsKey(drillName))
        {
            return 0;
        }

        if (upgradeType == "INCREASE HEAT LIMIT")
        {
            return vehicleUpgradeLevels[drillName].heatLevel;
        }

        if (upgradeType == "INCREASE COOLDOWN")
        {
            return vehicleUpgradeLevels[drillName].coolLevel;
        }

        // "BUY A DRONE"
        return vehicleUpgradeLevels[drillName].droneLevel;
    }

    public int GetHeatLimit(string keyName)
    {
        // Starts at 90
        // 10 endurance per level
        //return upgradeHeatValues[GetDrillUpgradeLevel(keyName, "INCREASE HEAT LIMIT")];
        return upgradeHeatValues[GetDrillUpgradeLevel(allDronesKey, "INCREASE HEAT LIMIT")];
    }

    public float GetCoolRate(string keyName)
    {
        // 6 per level (0.12f per update, and 50fps)
        //return upgradeCoolValues[GetDrillUpgradeLevel(keyName, "INCREASE COOLDOWN")];
        return upgradeCoolValues[GetDrillUpgradeLevel(allDronesKey, "INCREASE COOLDOWN")];
    }

    public int GetDroneCount(string keyName)
    {
        // Number of drones = drone level
        return GetDrillUpgradeLevel(allDronesKey, "BUY A DRONE");
    }

    private void UpdateUpgradesDictionary(int upgrade, string type)
    {
        // Make a new one if non existent and initialize to 0
        if (!vehicleUpgradeLevels.ContainsKey(allDronesKey))
        {
            vehicleUpgradeLevels[allDronesKey] = new VehicleUpgrade(0, 0, 0);
        }

        if (type == "INCREASE HEAT LIMIT")
        {
            vehicleUpgradeLevels[allDronesKey].heatLevel += upgrade;
            return;
        }

        // Change by amount
        if (type == "INCREASE COOLDOWN")
        {
            vehicleUpgradeLevels[allDronesKey].coolLevel += upgrade;
            return;
        }

        // "BUY A DRONE"
        vehicleUpgradeLevels[allDronesKey].droneLevel += upgrade;
    }

    // Returns false if player can't afford upgrade, true otherwise
    public bool PurchaseUpgrade(string type, int iteration)
    {
        // Find upgrade price
        ulong upgradePrice = GetUpgradePrice(type, iteration);

        // Cooldown
        if (type == "INCREASE COOLDOWN")
        {
        }
        // Heat
        else if (type == "INCREASE HEAT LIMIT")
        {

        }
        // Drone
        else if (type == "BUY A DRONE")
        {
            NPCManager.Instance.CreateNPC();
        }

        // Transact amount
        if (!playerState.VerifyEnoughCash(upgradePrice))
        {
            return false;
        }

        playerState.SubtractCash(upgradePrice);

        // Increment by one
        UpdateUpgradesDictionary(1, type);

        // Update displays
        //UpdateUpgradeDetails(type);

        audioDelegator.PlayAudio(oreSoundEffectsSource, upgradeSound, 0.2f);

        AnalyticsDelegator.Instance.VehicleUpgrade(type, GetDrillUpgradeLevel("", type), RefineryUpgradePad.Instance.mineRenderer.mineCount);

        // If the first drone the player bought, make them follow it, whether or not its the tutorial level
        if (type == "BUY A DRONE" && GetDrillUpgradeLevel("", "BUY A DRONE") == 1)
        {
            TutorialManager.Instance.MakePlayerFollowDrone();
        }

        return true;
    }

    public void GenerateUpgradeOptionDisplays()
    {
        int[] upgradeMilestones = RefineryUpgradePad.Instance.GetUpgradeMilestones();
        int requiredOreUpgradeLevel = RefineryUpgradePad.Instance.GetRequiredOreUpgradeLevel();

        // Min of 1, Max of 7
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

        for (int i = 0; i != rampUpIterationsNeeded; i++)
        {
            int iteration = i;
            for (int j = 0; j != upgradeBenefitTypes.Length; j++)
            {
                GameObject upgradeBayOptionGameObject = Instantiate(upgradeBayOptionPrefab);

                string benefitType = upgradeBenefitTypes[j];

                UpgradeBayOption upgradeBayOption = upgradeBayOptionGameObject.GetComponent<UpgradeBayOption>();
                UpgradeBayOptionData upgradeBayOptionData = new(upgradeBayOptionGameObject,
                                                                benefitType,
                                                                i);

                // Display the upgrade type
                upgradeBayOption.upgradeBenefitTypeImage.sprite = upgradeBenefitImages[j];
                upgradeBayOption.upgradeBenefitNameText.text = PlayerState.Instance.GetLocalizedValue(benefitType);

                
                // Set the description text to show the new value if its either one of these
                if (benefitType == "INCREASE HEAT LIMIT")
                {
                    upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("NEW: {0}", upgradeHeatValues[iteration]);
                }
                else if (benefitType == "INCREASE COOLDOWN")
                {
                    upgradeBayOption.upgradeBenefitDescriptionText.text = PlayerState.Instance.GetLocalizedValue("NEW: {0}", (upgradeCoolValues[iteration] * coolTimesPerSecond) + "/s");
                }
                // If it's neither one of these, then hide the description text because its not needed
                else
                {
                    upgradeBayOption.upgradeBenefitDescriptionText.gameObject.SetActive(false);
                }

                // Setup button
                upgradeBayOption.button.onClick.AddListener(() => PurchaseUpgrade(benefitType, iteration));
                ulong price = GetUpgradePrice(benefitType, iteration);
                upgradeBayOption.cashPriceText.text = PlayerState.Instance.FormatPrice(price);
                upgradeBayOption.buttonAffordability.price = price;

                // This if for notifying the player if an upgrade is available
                buttonAffordabilities.Add(upgradeBayOption.buttonAffordability);

                // For displaying properly
                upgradeBayOptionGameObject.transform.SetParent(scrollViewContent);
                upgradeBayOptionGameObject.transform.localScale = Vector3.one;
                upgradeOptions.Add(upgradeBayOptionData);
            }
        }

        VerticalLayoutGroup verticalLayoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
        float bigContentHeight = upgradeBayOptionPrefab.GetComponent<RectTransform>().sizeDelta.y * upgradeOptions.Count + verticalLayoutGroup.padding.top + verticalLayoutGroup.padding.bottom + ((upgradeOptions.Count - 1) * verticalLayoutGroup.spacing);

        RectTransform bigContentRect = scrollViewContent.GetComponent<RectTransform>();

        // Resize the scroll view content height to fit the rows using the height of all panels
        bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight);
    }

    private IEnumerator NotifyPlayerOfUpgrades()
    {
        // If still in the tutorial, wait a bit before starting to not mix up the player
        yield return new WaitUntil(() => TutorialManager.Instance.tutorialScreenIndex > 6);

        while (true)
        {
            bool affordable = false;

            System.Numerics.BigInteger cash = PlayerState.Instance.GetUserCash();

            for (int i = 0; i != buttonAffordabilities.Count; i++)
            {
                // If player can afford an upgrade and its not maxed, enable the notice icon, otherwise disable it
                /*if ((i == 0 && (GetDrillUpgradeLevel("", "INCREASE HEAT LIMIT") >= maxOtherCount || cash < GetHeatPrice()))
                || (i == 1 && (GetDrillUpgradeLevel("", "INCREASE COOLDOWN") >= maxOtherCount || cash < GetCoolPrice()))
                || (i == 2 && (GetDrillUpgradeLevel("", "BUY A DRONE") >= maxDroneCount || cash < GetDronePrice())))
                {
                    continue;
                }

                affordable = true;
                break;*/
            }

            upgradeBayNoticeIcon.SetActive(affordable);

            yield return new WaitForSecondsRealtime(0.5f);
        }
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
        this.vehicleCustomizations = data.vehicleCustomizations;
        this.customizationsOwned = data.customizationsOwned;

        StartCoroutine(NotifyPlayerOfUpgrades());

        loaded = true;
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
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
        /*flashButton = true;
        Image droneUpgradeButtonImage = droneUpgradePriceText.transform.parent.parent.GetComponent<Image>();

        Color originalColor = droneUpgradeButtonImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(droneUpgradeButtonImage, originalColor, darkColor));*/
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

    public bool BoughtOneDroneUpgrade()
    {
        if (vehicleUpgradeLevels == null)
        {
            return false;
        }

        foreach (var key in vehicleUpgradeLevels.Keys)
        {
            if (vehicleUpgradeLevels[key].droneLevel > 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool BoughtOneOtherUpgrade()
    {
        if (vehicleUpgradeLevels == null)
        {
            return false;
        }

        foreach (var key in vehicleUpgradeLevels.Keys)
        {
            if (vehicleUpgradeLevels[key].heatLevel > 0 || vehicleUpgradeLevels[key].coolLevel > 0)
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
        if (benefitType == "INCREASE HEAT LIMIT" || benefitType == "INCREASE COOLDOWN")
        {
            // Using cooldown prices for both for now
            return upgradeCoolPrices[iteration];
        }
        else if (benefitType == "BUY A DRONE")
        {
            return upgradeDronePrices[iteration];
        }
        else if (benefitType == "2X PROFITS")
        {
            return (ulong)(upgradeCoolPrices[iteration] * 8f);
        }
        // ("INCREASE ORE SPAWN RATE")
        else
        {
            return upgradeDronePrices[iteration] * 7;
        }
    }
}