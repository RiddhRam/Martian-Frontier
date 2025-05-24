using System;
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
    JoystickMovement joystickMovement;
    [SerializeField] AudioDelegator audioDelegator;

    [Header("Audio")]
    [SerializeField] AudioClip oreUpgradeSound;
    [SerializeField] AudioSource uIAudio;

    [Header("Upgrades")]
    // key: oreIndex, value: level
    public SerializableDictionary<int, int> oreUpgrades;
    private long[] originalMaterialPrices;

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

    void Awake()
    {
        joystickMovement = JoystickMovement.Instance;

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

        // Only the drill/hauler can activate this pad, not the body
        // Only the player vehicle can open the UI panel on their local game
        if (!(collision.GetComponent<DrillerController>() || !collision.transform.parent.parent.name.Contains("Player Vehicle")))
        {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;

        uIDelegation.HideAll();
        oreDelegation.PrepareGrid();
        uIDelegation.RevealElement(refineryScreen);

        // Stops player from moving
        joystickMovement.joystickVec = new();
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
    public void SetProceedPanelRequirement(int mineCount, MineRenderer mineRenderer)
    {
        mineCounter.text = oreDelegation.GetLocalizedValue("MINE {0}", mineCount);

        if (mineCount == 1)
        {
            requiredOreIndex = 0;
            requiredOreUpgradeLevel = 10;
        }
        else if (mineCount == 2)
        {
            requiredOreIndex = 1;
            requiredOreUpgradeLevel = 25;
        }
        else if (mineCount == 3)
        {
            requiredOreIndex = 5;
            requiredOreUpgradeLevel = 50;
        }
        else if (mineCount >= 4)
        {
            requiredOreIndex = 8;
            //requiredOreUpgradeLevel =

            // Max level is currently 500
            if (requiredOreUpgradeLevel > 500) {
                requiredOreUpgradeLevel = 500;
            }
        }
        
        upgradeRequirement.text = oreDelegation.GetLocalizedValue("UPGRADE {0} TO LEVEL {1}!", mineRenderer.selectedMaterialNames[requiredOreIndex], requiredOreUpgradeLevel);
        
        CheckIfProceedAvailable();
    }

    // If player meets upgrade requirement, hide requirement and show the proceed amount
    public void CheckIfProceedAvailable()
    {
        // Requirement not met
        if (GetOreUpgradeLevel(requiredOreIndex) < requiredOreUpgradeLevel)
        {
            return;
        }

        proceedButton.interactable = true;
        proceedButton.transform.GetChild(0).gameObject.SetActive(false);

        // Show user cash amount required for uprade
        upgradeRequirement.gameObject.SetActive(false);

        cashProceedAmount = GetCashProceedAmount();
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
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            //return;
        }

        // Player can proceed
        playerState.ProceedToNextMine();
    }

    private double GetCashProceedAmount() {
        return GetMaterialUpgradePriceAtLevel(requiredOreIndex, requiredOreUpgradeLevel) * 2;
    }

    public void PurchaseOreUpgrade(int oreIndex)
    {
        System.Numerics.BigInteger price = new(GetMaterialUpgradePrice(oreIndex));

        if (!playerState.VerifyEnoughCash(price))
        {
            uIDelegation.ShowError("NOT ENOUGH CASH!");
            return;
        }

        playerState.SubtractCash(price);
        UpgradeOre(oreIndex);
        audioDelegator.PlayAudio(uIAudio, oreUpgradeSound, 0.2f);

        // Redisplay the proper prices by clearing everything and generating again 
        // inefficient, but it's very simple and performance isn't demanding here. Also player doesn't even notice
        oreDelegation.UpdateOreMaterialPanelText(oreIndex);

        CheckIfProceedAvailable();
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

    public double GetActualMaterialPrice(int oreIndex)
    {
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        // Grows by 8% per level
        return Math.Floor(originalMaterialPrices[oreIndex] * Math.Pow(1.08, oreUpgradeLevel));
    }

    public double GetActualMaterialPriceAtLevel(int oreIndex, int level)
    {
        // Grows by 8% per level
        return Math.Floor(originalMaterialPrices[oreIndex] * Math.Pow(1.08, level));
    }

    public double GetMaterialUpgradePrice(int oreIndex)
    {
        int oreUpgradeLevel = GetOreUpgradeLevel(oreIndex);

        // Upgrade price outpaces the material price. Grows by 12% instead of 8%. Also starts at 12 times the current material price
        return Math.Floor(GetActualMaterialPrice(oreIndex) * 12 * Math.Pow(1.12, oreUpgradeLevel));
    }

    public double GetMaterialUpgradePriceAtLevel(int oreIndex, int level)
    {
        return Math.Floor(GetActualMaterialPriceAtLevel(oreIndex, level) * 12 * Math.Pow(1.12, level));
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
}