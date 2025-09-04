using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Is also the controller for the upgrade panel
public class RefineryUpgradePad : MonoBehaviour
{
    private static RefineryUpgradePad _instance;
    public static RefineryUpgradePad Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<RefineryUpgradePad>();
            }
            return _instance;
        }
    }

    [Header("Scripts")]
    [SerializeField] OreDelegation oreDelegation;
    public PlayerState playerState;
    public MineRenderer mineRenderer;

    [Header("Audio")]
    [SerializeField] AudioClip oreUpgradeSound;
    [SerializeField] AudioSource oreSoundEffectsSource;

    [Header("Upgrades")]
    // key: oreIndex, value: level
    public SerializableDictionary<int, int> oreUpgrades;
    private long[] originalMaterialPrices;
    private static readonly int[] upgradeMilestones = new int[] { 10, 25, 50, 75, 100, 150, 200, 250 };

    [Header("Tab Delegation")]
    public GameObject proceedPanel;

    [Header("Proceed Panel")]
    public TextMeshProUGUI mineName;
    public TextMeshProUGUI nextMineName;
    public TextMeshProUGUI upgradeRequirement;
    public TextMeshProUGUI cashProceedAmountText;
    public Button proceedButton;
    public Slider proceedProgress;
    static readonly string[] mineNames = new string[]
    {
        "Ares Landing",
        "Olympus City",
        "Endurance Point",
        "Fortitude",
        "New Houston",
        "The Dustbowl",
        "Redview",
        "The Nexus",
        "Orbital Gate",
        "Red Vista",
        "Neo Terminal",
        "Port Nova",
        "Kiruna II",
        "Red London",
        "Obelisk",
        "Armstrong Spire",
        "Hope's Landing",
        "Glory's Claim",
        "Unity Station",
        "Pioneer's Rest",
        "Barsoom",
        "Breakthrough",
        "Last Refuge",
        "Solitude",
        "Apex City",
        "Ridgegate",
        "Breachpoint",
        "Silica Spire",
        "The Quarry",
        "Ironstone",
        "Bedrock Bastion",
        "Pyrite Point",
        "Vulcan's Forge",
        "Solace",
        "The Grid",
        "Bradbury",
        "Sky-Hub",
        "Kepler's Landing",
        "Goddard's Reach",
        "Rustpoint",
        "Cinderpit",
        "The Warren",
        "Skybreak Mine",
        "The Terminus",
        "Farpoint",
        "The Drillhead",
        "Red Fissure",
        "The Windbreak",
        "Marsgrad",
        "Port Armstrong",
        "Prospect Point",
        "Dawn's Reach",
        "Horizon's Gate",
        "Outcrop Oasis",
        "Red Rock",
        "Elysium City",
        "Westgate",
        "Ironclad",
        "Echo Base",
        "Lookout"
    };

    int requiredOreIndex;
    int requiredOreUpgradeLevel;
    double cashProceedAmount;

    const float orePriceMultiplierPerLevel = 1.08f;
    const float oreUpgradePriceMultiplierPerLevel = 1.20f;

    const float baseMaterialPriceMultiplier = 5f;

    [Header("For Tutorial")]
    public bool flashButton;
    public Image closeButtonImage;
    [HideInInspector] public Image limestoneUpgradeImage;
    public Image proceedPanelButtonImage;

    [Header("Notice Icons")]
    public GameObject proceedNoticeIcon;

    void Awake()
    {
        // Store this for reference later
        int[] materialPrices = oreDelegation.GetOriginalMaterialPrices();
        originalMaterialPrices = new long[materialPrices.Length];
        // Convert to long
        for (int i = 0; i != materialPrices.Length; i++)
        {
            originalMaterialPrices[i] = (long)materialPrices[i];
        }
    }

    void Start()
    {
        StartCoroutine(NotifyPlayerOfUpgrades());
        //StartCoroutine(HighlightUpgradeRequirement());
    }

    private IEnumerator NotifyPlayerOfUpgrades()
    {
        // If still in the tutorial, wait a bit to not mix up the player.
        //yield return new WaitUntil(() => TutorialManager.Instance.finishedTutorial || TutorialManager.Instance.tutorialScreenIndex >= 11);

        while (true)
        {
            bool affordable = CanAffordAnUpgrade();

            bool canProceed = false;

            if (GetOreUpgradeLevel(requiredOreIndex) >= requiredOreUpgradeLevel)
            {
                canProceed = true;
            }

            // Toggle all icons on the drones
            for (int i = 0; i != NPCManager.Instance.upgradeNoticeIcons.Length; i++)
            {
                if (NPCManager.Instance.upgradeNoticeIcons[i] == null)
                    continue;

                NPCManager.Instance.upgradeNoticeIcons[i].SetActive(affordable);
            }

            // Toggle the icon on the proceed button
            proceedNoticeIcon.SetActive(canProceed);

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private IEnumerator HighlightUpgradeRequirement()
    {
        Color originalColor = upgradeRequirement.color;
        Color highlightColor = new(57 / 255f, 255 / 255f, 20 / 255f);

        while (true)
        {
            float t = Mathf.PingPong(Time.time / 0.5f, 1f);
            upgradeRequirement.color = Color.Lerp(originalColor, highlightColor, t);
            yield return null;
        }
    }

    public bool CanAffordAnUpgrade()
    {
        // Do this everytime, in case mine renderer took too long to load the first time
        int[] oresPerTier = MineRenderer.Instance.oresPerTier;

        int tier = PlayerState.Instance.GetRecommendedDrillTier();
        if (tier < 1)
        {
            tier = 1;
        }

        bool affordable = false;
        int oreCounter = 0;

        System.Numerics.BigInteger cash = PlayerState.Instance.GetUserCash();

        // Check if we can afford any upgrade at or above the selected target depth
        for (int i = 0; i != tier; i++)
        {
            // If not at the right tier yet (tier is not zero-indexed so subtract 1)
            if (i < tier - 1)
            {
                oreCounter += oresPerTier[i];
                continue;
            }

            for (int j = 0; j != oresPerTier[i]; j++)
            {
                // Not max level, and can afford
                if (MineRenderer.Instance.discoveredOres.Contains(oreCounter) && GetOreUpgradeLevel(oreCounter) < GetRequiredOreUpgradeLevel() && (double)cash >= GetMaterialUpgradePrice(oreCounter))
                {
                    affordable = true;

                    break;
                }

                oreCounter++;
            }

            break;
        }

        // If we can't afford any upgrades at or above the selected target depth, then explicity check if we can afford the required ore
        if (!affordable)
        {
            if (GetOreUpgradeLevel(requiredOreIndex) < GetRequiredOreUpgradeLevel() && (double)cash >= GetMaterialUpgradePrice(requiredOreIndex))
            {
                affordable = true;
            }
        }

        return affordable;
    }

    public void PreparePanel()
    {
        // Update in case of translations
        UpdateUpgradeRequirementText();

        // Stops player from moving
        //JoystickMovement.Instance.joystickVec = new();
    }

    // Set next requirement needed
    public void SetProceedPanelRequirement(int mineCount)
    {

        // ore 1, level 25
        if (mineCount == 1)
        {
            requiredOreIndex = 0;
            requiredOreUpgradeLevel = upgradeMilestones[1];
        }
        // ore 3, level 50
        else if (mineCount == 2)
        {
            requiredOreIndex = 2;
            requiredOreUpgradeLevel = upgradeMilestones[2];
        }
        else if (mineCount == 3)
        {
            requiredOreIndex = 3;
            requiredOreUpgradeLevel = upgradeMilestones[3];
        }
        else if (mineCount == 4)
        {
            requiredOreIndex = 4;
            requiredOreUpgradeLevel = upgradeMilestones[4];
        }
        else if (mineCount == 5)
        {
            requiredOreIndex = 5;
            requiredOreUpgradeLevel = upgradeMilestones[5];
        }
        else if (mineCount == 6)
        {
            requiredOreIndex = 6;
            requiredOreUpgradeLevel = upgradeMilestones[5];
        }
        else if (mineCount >= 7)
        {
            requiredOreIndex = 8;

            // 200
            if (mineCount <= 8)
            {
                requiredOreUpgradeLevel = upgradeMilestones[6];
            }
            // 250
            else if (mineCount >= 9)
            {
                requiredOreUpgradeLevel = upgradeMilestones[7];
            }
        }

        PlayerState.Instance.SetMaxTargetDepth(MineRenderer.Instance.GetOreTierByIndex(requiredOreIndex));

        UpdateUpgradeRequirementText();

        CheckIfProceedAvailable();
    }

    private void UpdateUpgradeRequirementText()
    {
        // The current name is the value at the index of mineCount. 
        // It will wrap back around to the start of the array if mineCount is greater than or equal to the length of the array
        mineName.text = mineNames[mineRenderer.mineCount % mineNames.Length];

        // Same as above, but index is incremented by 1
        nextMineName.text = mineNames[(mineRenderer.mineCount + 1) % mineNames.Length];

        // Requirement to reach next level
        upgradeRequirement.text = oreDelegation.GetLocalizedValue("UPGRADE {0} TO LEVEL {1}!", GetRequiredOreName(), requiredOreUpgradeLevel);

        int upgradeLevel = GetOreUpgradeLevel(requiredOreIndex);
        int requiredlevel = GetRequiredOreUpgradeLevel();

        if (upgradeLevel >= requiredlevel)
        {
            proceedProgress.value = 1;
        }
        else
        {
            proceedProgress.value = (float)upgradeLevel / requiredlevel;
        }
        
    }

    // If player meets upgrade requirement, hide requirement and show the proceed amount
    public void CheckIfProceedAvailable()
    {
        // Requirement not met
        if (GetOreUpgradeLevel(requiredOreIndex) < requiredOreUpgradeLevel)
        {
            return;
        }

        cashProceedAmount = GetCashProceedAmount();

        proceedButton.transform.GetChild(0).gameObject.SetActive(false);

        // Interactable if player can afford or not
        ButtonAffordability buttonAffordability = proceedButton.GetComponent<ButtonAffordability>();
        buttonAffordability.price = new System.Numerics.BigInteger(cashProceedAmount);
        buttonAffordability.enabled = true;

        upgradeRequirement.gameObject.SetActive(false);

        // Show user cash amount required for upgrade
        cashProceedAmountText.text = playerState.FormatPrice(new System.Numerics.BigInteger(cashProceedAmount));
        cashProceedAmountText.transform.parent.gameObject.SetActive(true);
    }

    public void ProceedToNextVehicle()
    {
        if (cashProceedAmount == 0)
        {
            cashProceedAmount = GetCashProceedAmount();
        }

        if (!playerState.VerifyEnoughCash(new System.Numerics.BigInteger(cashProceedAmount)))
        {
            return;
        }

        // Player can proceed
        playerState.ProceedToNextMine();
    }

    public double GetCashProceedAmount()
    {
        double amount = GetMaterialUpgradePrice(requiredOreIndex, requiredOreUpgradeLevel) * 2;

        return amount;
    }

    // Returns false if player can't afford upgrade, true otherwise
    public bool PurchaseOreUpgrade(int oreIndex, bool alternate = false)
    {
        System.Numerics.BigInteger price = new(GetMaterialUpgradePrice(oreIndex));

        if (!playerState.VerifyEnoughCash(price))
        {
            return false;
        }

        playerState.SubtractCash(price);
        // Get it before, then add one, in case there's a small delay and player is spam buying
        int newLevel = GetOreUpgradeLevel(oreIndex) + 1;
        UpgradeOre(oreIndex);

        // If a milestone was reached, display a special effect, otherwise do nothing
        bool reachedMilestone = false;
        for (int i = 0; i != upgradeMilestones.Length; i++)
        {
            if (newLevel == upgradeMilestones[i])
            {
                reachedMilestone = true;
                break;
            }
        }

        oreDelegation.UpdateOreMaterialPanel(oreIndex, true, reachedMilestone);

        CheckIfProceedAvailable();

        AudioDelegator.Instance.PlayAudio(oreSoundEffectsSource, oreUpgradeSound, 0.15f);

        AnalyticsDelegator.Instance.OreUpgrade(mineRenderer.selectedMaterialNames[oreIndex], newLevel, mineRenderer.mineCount);

        return true;
    }

    private void UpgradeOre(int oreIndex)
    {
        if (oreUpgrades.ContainsKey(oreIndex))
        {
            oreUpgrades[oreIndex]++;
            return;
        }

        oreUpgrades[oreIndex] = 1;
    }

    // How much the ore is worth when selling
    public double GetActualMaterialPrice(int oreIndex, int level = -1)
    {
        if (level == -1)
        {
            level = GetOreUpgradeLevel(oreIndex);
        }

        // Grows by 8% per level
        return Math.Floor(originalMaterialPrices[oreIndex] * GetOrePriceMultiplier(level)) * VehicleUpgradeBayManager.Instance.GetProfitMultiplier() * VehicleUpgradeBayManager.Instance.GetOreProfitMultiplier(oreIndex);
    }

    public double GetOrePriceMultiplier(int level)
    {
        double multiplier = 1;

        int lastMilestone = 0;

        for (int i = 0; i != upgradeMilestones.Length; i++)
        {
            // If level passes the next milestone
            if (level >= upgradeMilestones[i])
            {
                // Multiply from the last milestone up to 1 less than the next milestone
                multiplier *= Math.Pow(orePriceMultiplierPerLevel, (upgradeMilestones[i] - 1) - lastMilestone);

                // Double it because it reached the next milestone
                multiplier *= 2;

                // Set last milestone
                lastMilestone = upgradeMilestones[i];
            }
            // If it doesn't
            else
            {
                // Multiply from the last milestone up to the current level
                multiplier *= Math.Pow(orePriceMultiplierPerLevel, level - lastMilestone);
                break;
            }
        }

        return multiplier;
    }

    // How much the upgrade costs
    public double GetMaterialUpgradePrice(int oreIndex, int level = -1)
    {
        // If no level provided, then get the current level
        if (level == -1)
        {
            level = GetOreUpgradeLevel(oreIndex);
        }

        // Upgrade price outpaces the material price. Grows by 20% instead of 8%. Also starts at (baseMaterialPriceMultiplier * x) the current material price
        return Math.Floor(originalMaterialPrices[oreIndex] * baseMaterialPriceMultiplier * Math.Pow(oreUpgradePriceMultiplierPerLevel, level));
    }

    public int GetOreUpgradeLevel(int oreIndex)
    {
        // Hasn't been upgraded yet
        if (!oreUpgrades.ContainsKey(oreIndex))
        {
            return 0;
        }

        // Has been upgraded
        return oreUpgrades[oreIndex];
    }

    public int GetNextOreMilestone(int oreIndex)
    {
        // Returns the next milestone for the ore to reach
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        int milestone = upgradeMilestones[0];

        // Start at 2nd index
        for (int i = 1; i != upgradeMilestones.Length; i++)
        {
            // Check if smaller than the last milestone
            if (oreUpgradeLevel < upgradeMilestones[i - 1])
            {
                break;
            }

            milestone = upgradeMilestones[i];
        }

        return milestone;
    }

    public int GetLastOreMilestone(int oreIndex)
    {
        // Returns the last milestone the ore reached
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        int milestone = 0;

        for (int i = 0; i != upgradeMilestones.Length; i++)
        {
            if (oreUpgradeLevel < upgradeMilestones[i])
            {
                break;
            }

            milestone = upgradeMilestones[i];
        }

        return milestone;
    }

    public int GetMaxOreLevel()
    {
        return upgradeMilestones[upgradeMilestones.Length - 1];
    }

    public string GetRequiredOreName()
    {
        return mineRenderer.selectedMaterialNames[requiredOreIndex];
    }

    public int GetRequiredOreIndex()
    {
        return requiredOreIndex;
    }

    public int GetRequiredOreUpgradeLevel()
    {
        return requiredOreUpgradeLevel;
    }

    public void FlashCloseButton()
    {
        flashButton = true;

        Color originalColor = closeButtonImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(closeButtonImage, originalColor, darkColor));
    }

    public void FlashOreUpgradeButton()
    {
        flashButton = true;

        Color originalColor = limestoneUpgradeImage.color;
        Color darkColor = originalColor * 0.7f;

        StartCoroutine(FlashButton(limestoneUpgradeImage, originalColor, darkColor));
    }

    public void FlashProceedPanelButton()
    {
        flashButton = true;

        // it's flipped on purpose. Button should stay red after being clicked
        Color originalColor = new(1, 0, 0);
        // Expicitly set as white to make it feel brighter
        Color darkColor = new(1, 1, 1);

        StartCoroutine(FlashButton(proceedPanelButtonImage, originalColor, darkColor));
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

    public bool BoughtThreeUpgrades()
    {
        if (oreUpgrades == null)
        {
            return false;
        }

        foreach (var key in oreUpgrades.Keys)
        {
            if (oreUpgrades[key] >= 3)
            {
                return true;
            }
        }

        return false;
    }

    public bool BoughtTenUpgrades()
    {
        if (oreUpgrades == null)
        {
            return false;
        }

        foreach (var key in oreUpgrades.Keys)
        {
            if (oreUpgrades[key] >= 10)
            {
                return true;
            }
        }

        return false;
    }

    public bool Bought25Upgrades()
    {
        if (oreUpgrades == null)
        {
            return false;
        }

        foreach (var key in oreUpgrades.Keys)
        {
            if (oreUpgrades[key] >= 25)
            {
                return true;
            }
        }

        return false;
    }

    public int[] GetUpgradeMilestones()
    {
        return upgradeMilestones;
    }
}