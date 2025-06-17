using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Is also the controller for the upgrade panel
public class RefineryUpgradePad : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField] UIDelegation uIDelegation;
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
    [SerializeField] GameObject refineryScreen;
    // The current panel showing in the refinery panel
    private string currentTab = "Ores";
    public Image oreTabButton;
    public GameObject orePanel;
    public Image proceedTabButton;
    public GameObject proceedPanel;

    [Header("Proceed Panel")]
    public TextMeshProUGUI mineCounter;
    public TextMeshProUGUI upgradeRequirement;
    public TextMeshProUGUI cashProceedAmountText;
    public Button proceedButton;

    int requiredOreIndex;
    int requiredOreUpgradeLevel;
    double cashProceedAmount;

    const float orePriceMultiplierPerLevel = 1.08f;
    const float oreUpgradePriceMultiplierPerLevel = 1.2f;

    [Header("For Tutorial")]
    public bool flashButton;
    public Image closeButtonImage;
    public Image limestoneUpgradeImage;
    public Image proceedPanelButtonImage;

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

    void OnTriggerEnter2D(Collider2D collision)
    {

        // Only the Player Trigger trigger can activate the pad, not the body or drill
        // Only the player vehicle can open the UI panel on their local game
        if (collision.name != "Player Trigger" || !collision.transform.parent.parent.name.Contains("Player Vehicle"))
        {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;

        // Update in case of translations
        UpdateUpgradeRequirementText();

        uIDelegation.HideAll();
        oreDelegation.PrepareGrid();
        uIDelegation.RevealElement(refineryScreen);

        // Stops player from moving
        JoystickMovement.Instance.joystickVec = new();
    }

    public void SwitchTabs(string newTab)
    {
        if (currentTab == newTab)
        {
            return;
        }

        // Ores, key: "Ores"
        if (newTab == "Ores")
        {
            // Disable old tab
            proceedTabButton.color = new(1, 1, 1, 90 / 255f);
            proceedTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50 / 255f, 50 / 255f, 50 / 255f);
            proceedPanel.SetActive(false);

            // Enable new one
            oreTabButton.color = new(1, 0, 0);
            oreTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1);
            orePanel.SetActive(true);
        }
        // Proceed to next mine, key: "Proceed"
        else
        {
            oreTabButton.color = new(1, 1, 1, 90 / 255f);
            oreTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50 / 255f, 50 / 255f, 50 / 255f);
            orePanel.SetActive(false);

            proceedTabButton.color = new(1, 0, 0);
            proceedTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1);
            proceedPanel.SetActive(true);
        }

        currentTab = newTab;
    }

    // Indicate next vehicle for unlock
    public void SetProceedPanelVehicle(GameObject nextDrill)
    {
        Transform nextDrillTransform = Instantiate(nextDrill).transform;

        // Move to panel and scale down
        nextDrillTransform.SetParent(proceedPanel.transform.GetChild(1));
        nextDrillTransform.localScale = new(0.8f, 0.8f, 0.8f);

        // Update positioning
        RectTransform rt = nextDrillTransform.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new(-rt.sizeDelta.x, 100);
    }

    // Set next requirement needed
    public void SetProceedPanelRequirement(int mineCount) {

        // ore 0, level 25
        if (mineCount == 1)
        {
            requiredOreIndex = 0;
            requiredOreUpgradeLevel = upgradeMilestones[0];
        }
        // ore 1, level 25
        else if (mineCount == 2)
        {
            requiredOreIndex = 1;
            requiredOreUpgradeLevel = upgradeMilestones[1];
        }
        // 50
        else if (mineCount == 3)
        {
            requiredOreIndex = 5;
            requiredOreUpgradeLevel = upgradeMilestones[2];
        }
        else if (mineCount >= 4)
        {
            requiredOreIndex = 8;

            // 100
            if (mineCount == 4)
            {
                requiredOreUpgradeLevel = upgradeMilestones[4];
            }
            // 200
            else if (mineCount == 5)
            {
                requiredOreUpgradeLevel = upgradeMilestones[6];
            }
            // 250
            else if (mineCount >= 6)
            {
                requiredOreUpgradeLevel = upgradeMilestones[7];
            }
        }

        UpdateUpgradeRequirementText();

        CheckIfProceedAvailable();
    }

    private void UpdateUpgradeRequirementText()
    {        
        mineCounter.text = oreDelegation.GetLocalizedValue("MINE {0}", mineRenderer.mineCount);
        upgradeRequirement.text = oreDelegation.GetLocalizedValue("UPGRADE {0} TO LEVEL {1}!", GetRequiredOreName(), requiredOreUpgradeLevel);
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

        // If not finished tutorial, you can not continue yet
        if (!DataPersistenceManager.Instance.GetGameData().finishedTutorial)
        {
            uIDelegation.ShowError("FINISH THE TUTORIAL FIRST!");
            return;
        }

        // Player can proceed
        playerState.ProceedToNextMine();
    }

    private double GetCashProceedAmount() {
        double amount = GetMaterialUpgradePriceAtLevel(requiredOreIndex, requiredOreUpgradeLevel) * 2;

        // Minimum
        /*if (amount < 50_000)
        {
            amount = 50_000;
        }*/

        return amount;
    }

    // Returns false if player can't afford upgrade, true otherwise
    public bool PurchaseOreUpgrade(int oreIndex) {
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

        AudioDelegator.Instance.PlayAudio(oreSoundEffectsSource, oreUpgradeSound, 0.2f);

        AnalyticsDelegator.Instance.OreUpgrade(mineRenderer.selectedMaterialNames[oreIndex], newLevel, mineRenderer.mineCount);
        
        return true;
    }

    private void UpgradeOre(int oreIndex) {
        if (oreUpgrades.ContainsKey(oreIndex))
        {
            oreUpgrades[oreIndex]++;
            return;
        }

        oreUpgrades[oreIndex] = 1;
    }

    public double GetActualMaterialPrice(int oreIndex)
    {
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        // Grows by 8% per level
        return Math.Floor(originalMaterialPrices[oreIndex] * GetOrePriceMultiplier(oreUpgradeLevel));
    }

    public double GetActualMaterialPriceAtLevel(int oreIndex, int level)
    {
        return Math.Floor(originalMaterialPrices[oreIndex] * GetOrePriceMultiplier(level));
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

    public double GetMaterialUpgradePrice(int oreIndex)
    {
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        // Upgrade price outpaces the material price. Grows by 20% instead of 8%. Also starts at half the current material price
        return Math.Floor(originalMaterialPrices[oreIndex] * 0.5 * Math.Pow(oreUpgradePriceMultiplierPerLevel, oreUpgradeLevel));
    }

    public double GetMaterialUpgradePriceAtLevel(int oreIndex, int level)
    {
        return Math.Floor(originalMaterialPrices[oreIndex] * 0.5 * Math.Pow(oreUpgradePriceMultiplierPerLevel, level));
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

    public void FlashCloseButton()
    {
        flashButton = true;

        Color originalColor = closeButtonImage.color;
        Color darkColor = new(160 / 255f, 160 / 255f, 160 / 255f);

        StartCoroutine(FlashButton(closeButtonImage, originalColor, darkColor, new(1.2f, 1.2f, 1.2f)));
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

    private IEnumerator FlashButton(Image buttonImage, Color originalColor, Color darkColor, Vector3? largeScale = null)
    {
        float duration = 0.4f; // time to go from original to dark and back
        float t = 0f;
        bool goingDarker = true;

        RectTransform rectTransform = null;
        Vector3 initialScale = Vector3.zero;

        // If large scale is provided, then we need to animate the scale too
        if (largeScale.HasValue)
        {
            rectTransform = buttonImage.GetComponent<RectTransform>();
            initialScale = rectTransform.localScale;
        }

        while (flashButton)
        {
            t += Time.deltaTime / duration;

            // Color
            // Darken
            if (goingDarker)
                buttonImage.color = Color.Lerp(originalColor, darkColor, t);
            // Brighten
            else
                buttonImage.color = Color.Lerp(darkColor, originalColor, t);
            
            // Scale, if provided
            if (largeScale.HasValue)
            {
                Vector3 big = largeScale.Value;
                if (goingDarker)
                    rectTransform.localScale = Vector3.Lerp(initialScale, big, t);
                else
                    rectTransform.localScale = Vector3.Lerp(big, initialScale, t);
            }

            if (t >= 1f)
            {
                t = 0f;
                goingDarker = !goingDarker;
            }

            yield return null;
        }

        // Return to original colour
        buttonImage.color = originalColor;
    }
    
    public bool BoughtOneUpgrade() {
        foreach (var key in oreUpgrades.Keys) {
            if (oreUpgrades[key] > 0) {
                return true;
            }
        }

        return false;
    }
}