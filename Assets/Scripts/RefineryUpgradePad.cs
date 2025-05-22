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
    [SerializeField] PlayerState playerState;
    JoystickMovement joystickMovement;
    [SerializeField] GameObject refineryScreen;

    [Header("Upgrades")]
    // oreIndex: level
    public SerializableDictionary<int, int> oreUpgrades;
    private long[] originalMaterialPrices;

    [Header("Tab Delegation")]
    // The current panel showing in the refinery panel
    private string currentTab = "Ores";
    public Image oreTabButton;
    public GameObject orePanel;
    public Image proceedTabButton;
    public GameObject proceedPanel;

    [Header("Proceed Panel")]
    public TextMeshProUGUI mineCounter;
    public TextMeshProUGUI upgradeRequirement;
    public Button proceedButton;

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
        nextDrillTransform.SetParent(proceedPanel.transform);
        nextDrillTransform.localScale = new(1.5f, 1.5f, 1.5f);

        // Reposition
        RectTransform rt = nextDrillTransform.GetComponent<RectTransform>();
        rt.offsetMin = new(0, rt.offsetMin.y);
        rt.offsetMax = new(0, rt.offsetMax.y);

        Vector2 pos = rt.anchoredPosition;
        pos.y = -1100f;
        rt.anchoredPosition = pos;
    }

    // Set next requirement needed
    public void SetProceedPanelRequirement(int mineCount)
    {
        proceedPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = oreDelegation.GetLocalizedValue("MINE {0}", mineCount);
        
        CheckIfProceedAvailable();
    }

    // If player meets upgrade requirement, hide requirement and show the proceed amount
    public void CheckIfProceedAvailable()
    {

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
        Debug.Log(oreIndex + ": " + GetOreUpgradeLevel(oreIndex));

        // Redisplay the proper prices by clearing everything and generating again 
        // inefficient, but it's very simple and performance isn't demanding here. Also player doesn't even notice
        oreDelegation.ClearGrid();
        oreDelegation.PrepareGrid();
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

        return Math.Floor(originalMaterialPrices[oreIndex] * Math.Pow(1.08, oreUpgradeLevel));
    }

    public double GetMaterialUpgradePrice(int oreIndex)
    {
        // Must mine 15 of the ore at its current price in order to upgrade once. On average this means 1 mine can give you 1-2 upgrades.
        return GetActualMaterialPrice(oreIndex) * 15;
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