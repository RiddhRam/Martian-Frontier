using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using System.Collections;
using System;

public class OreDelegation : MonoBehaviour
{
    private static OreDelegation _instance;
    public static OreDelegation Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<OreDelegation>();
            }
            return _instance;
        }
    }

    private MineRenderer mineRenderer;

    [Header("Important Values")]
    public string[] materialNames;
    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    [SerializeField] private int[] materialPrices;
    public Sprite[] materialHighResSprites;
    public TileBase[] oreTileValues;
    public Color[] oreTileColours;

    [Header("UI")]

    // Track components of each ore panel
    private TextMeshProUGUI[] materialLevelTexts;
    private TextMeshProUGUI[] materialPriceTexts;
    private TextMeshProUGUI[] materialUpgradePriceTexts;
    private Slider[] levelProgressBars;
    private RectTransform[] milestoneTransforms;
    private ButtonAffordability[] buttonAffordabilities;

    public GameObject refineryAlternatePanel;
    public GameObject[] alternatePanels;
    private Image[] levelProgressBarsImages;

    private int[] oresPerTier;
    // Lowercase verion of materialNames
    private bool[] isOre;
    private Coroutine flashOutlineCoroutine;
    public Canvas canvas;

    void Awake() {
        mineRenderer = MineRenderer.Instance;
        oresPerTier = mineRenderer.oresPerTier;

        int tileCount = oresPerTier.Length;

        for (int i = 0; i != oresPerTier.Length; i++) {
            tileCount += oresPerTier[i];
        }

        isOre = new bool[tileCount];

        int index = 1;
        for (int i = 0; i != oresPerTier.Length; i++) {
            for (int j = 0; j != oresPerTier[i]; j++) {
                isOre[index] = true;
                index++;
            }
            index++;
        }
    }

    public void PrepareGrid()
    {
        int length = mineRenderer.selectedMaterialNames.Length;
        materialLevelTexts = new TextMeshProUGUI[length];
        materialPriceTexts = new TextMeshProUGUI[length];
        materialUpgradePriceTexts = new TextMeshProUGUI[length];
        milestoneTransforms = new RectTransform[length];
        buttonAffordabilities = new ButtonAffordability[length];

        levelProgressBarsImages = new Image[length];

        int requiredOreIndex = RefineryUpgradePad.Instance.GetRequiredOreIndex();
        int requiredOreIndexTier = MineRenderer.Instance.GetOreTierByIndex(requiredOreIndex);

        for (int i = 0; i != length; i++)
        {
            // Determine which ores to show
            bool foundOre = true;

            if (i != requiredOreIndex)
            {
                // If tier is higher than the required ores tier
                if (MineRenderer.Instance.GetOreTierByIndex(i) > requiredOreIndexTier)
                {
                    break;
                }

                // If not found ore yet
                if (!mineRenderer.discoveredOres.Contains(i))
                {
                    foundOre = false;

                    // If not found, and index is higher (but still same tier) then don't show anything at all after this index
                    if (i > requiredOreIndex)
                    {
                        break;
                    }
                }
            }

            alternatePanels[i].SetActive(true);
            Transform panelTransform = alternatePanels[i].transform;

            string oreName = mineRenderer.selectedMaterialNames[i];

            materialPriceTexts[i] = panelTransform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
            materialLevelTexts[i] = panelTransform.GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>();
            materialUpgradePriceTexts[i] = panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();

            if (foundOre)
            {
                panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = oreName;

                materialPriceTexts[i].text = RefineryUpgradePad.Instance.playerState.FormatPrice(new System.Numerics.BigInteger(RefineryUpgradePad.Instance.GetActualMaterialPrice(i)));
                materialLevelTexts[i].text = GetLocalizedValue("LEVEL {0}", RefineryUpgradePad.Instance.GetOreUpgradeLevel(i));

                materialPriceTexts[i].transform.parent.gameObject.SetActive(true);
                materialLevelTexts[i].transform.parent.gameObject.SetActive(true);

                Image image = panelTransform.GetChild(4).GetComponent<Image>();
                image.sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];
                image.color = new(1, 1, 1);

                // Save as its own variable, otherwise it keeps a reference to the variable i
                int oreIndex = i;
                // Add onclick listener and hold button component
                Button button = panelTransform.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                if (RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex) < RefineryUpgradePad.Instance.GetRequiredOreUpgradeLevel())
                {
                    button.onClick.AddListener(() => RefineryUpgradePad.Instance.PurchaseOreUpgrade(oreIndex, true));
                    if (panelTransform.GetComponent<HoldButton>() == null)
                    {
                        // Hold to purchase
                        HoldButton holdButton = panelTransform.gameObject.AddComponent<HoldButton>();
                        holdButton.SetAction(() => RefineryUpgradePad.Instance.PurchaseOreUpgrade(oreIndex, true));
                    }

                    // Button affordability
                    buttonAffordabilities[i] = panelTransform.GetComponent<ButtonAffordability>();
                    buttonAffordabilities[i].enabled = true;
                }
                else
                {
                    if (panelTransform.GetComponent<HoldButton>() != null)
                    {
                        Destroy(panelTransform.GetComponent<HoldButton>());
                    }

                    if (panelTransform.GetComponent<ButtonAffordability>() != null)
                    {
                        Destroy(panelTransform.GetComponent<ButtonAffordability>());
                    }
                }

                levelProgressBarsImages[i] = panelTransform.GetChild(2).GetChild(0).GetComponent<Image>();

                milestoneTransforms[i] = panelTransform.GetChild(5).GetComponent<RectTransform>();

                // We pass false for 'reachedMilestone', even though it may have been reached because it shouldn't show anything at all
                UpdateOreMaterialPanel(i, false, false);
            }
            else
            {
                // If not found, then show as mystery ore
                panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("NOT FOUND");

                materialPriceTexts[i].transform.parent.gameObject.SetActive(false);
                materialLevelTexts[i].transform.parent.gameObject.SetActive(false);

                panelTransform.GetComponent<Button>().interactable = false;

                panelTransform.GetComponent<ButtonAffordability>().enabled = false;

                materialUpgradePriceTexts[i].text = "--";
            }
            
        }

        // Center the panel onto the drone
        Vector3 worldPos = GameCameraController.Instance.droneToFollow.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out localPoint);

        refineryAlternatePanel.GetComponent<RectTransform>().anchoredPosition = localPoint;

        UIDelegation.Instance.RevealElement(refineryAlternatePanel);
    }

    public void UpdateOreMaterialPanel(int oreIndex, bool flashOutline, bool reachedMilestone)
    {
        // Update text
        materialPriceTexts[oreIndex].text = RefineryUpgradePad.Instance.playerState.FormatPrice(new System.Numerics.BigInteger(RefineryUpgradePad.Instance.GetActualMaterialPrice(oreIndex)));
        materialLevelTexts[oreIndex].text = GetLocalizedValue("LEVEL {0}", RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex));

        Transform buttonTransform = materialUpgradePriceTexts[oreIndex].transform.parent.parent;
        Button button = buttonTransform.GetComponent<Button>();

        System.Numerics.BigInteger newPrice = new(RefineryUpgradePad.Instance.GetMaterialUpgradePrice(oreIndex));

        // If player can't afford, make it disabled initially. Otherwise it will show up as interactable for a split second
        button.interactable = !(newPrice > RefineryUpgradePad.Instance.playerState.GetUserCash());

        bool maxLevel = false;

        if (RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex) >= RefineryUpgradePad.Instance.GetRequiredOreUpgradeLevel())
        {
            maxLevel = true;
            // Hide price tag, show MAX text
            buttonTransform.GetChild(1).gameObject.SetActive(false);
            buttonTransform.GetChild(6).gameObject.SetActive(true);

            // Destroy hold button component, it may not have been added yet in some edge cases, that's why we do this
            if (buttonTransform.TryGetComponent(out HoldButton hold))
            {
                Destroy(hold);
            }

            if (buttonAffordabilities[oreIndex] != null)
            {
                Destroy(buttonAffordabilities[oreIndex]);
            }

            // Disable button if max
            button.interactable = false;
            buttonTransform.GetComponent<Image>().color = new(1, 0, 0);
        }
        else
        {
            // Update price text
            materialUpgradePriceTexts[oreIndex].text = RefineryUpgradePad.Instance.playerState.FormatPrice(newPrice);
            buttonAffordabilities[oreIndex].price = newPrice;
        }

        int lastMilestone = RefineryUpgradePad.Instance.GetLastOreMilestone(oreIndex);

        // Update progress bar
        if (maxLevel)
        {
            levelProgressBarsImages[oreIndex].color = new(1, 112f / 255, 67f / 255);
            levelProgressBarsImages[oreIndex].fillAmount = 1;
        }
        else
        {
            levelProgressBarsImages[oreIndex].fillAmount = (float)(RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex) - lastMilestone) / (RefineryUpgradePad.Instance.GetNextOreMilestone(oreIndex) - lastMilestone);
        }
        
        // If we should flash the outline (an upgrade was made)
        if (flashOutline)
        {
            // If upgrade milestone was reached
            if (reachedMilestone)
            {
                StartCoroutine(BobMilestonePanel(milestoneTransforms[oreIndex]));
            }
        }

    }

    private IEnumerator BobMilestonePanel(RectTransform doubleProfitPanel)
    {
        doubleProfitPanel.gameObject.SetActive(true);

        // Bobs the double profit panel up and down
        Vector2 originalPosition = doubleProfitPanel.anchoredPosition;
        Vector2 targetPosition = originalPosition + new Vector2(0, 20); // Move up by 20 on the Y-axis

        float elapsedTime = 0f;
        float duration = 0.5f;

        // Move up
        while (elapsedTime < duration / 2)
        {
            doubleProfitPanel.anchoredPosition = Vector2.Lerp(originalPosition, targetPosition, elapsedTime / (duration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        doubleProfitPanel.anchoredPosition = targetPosition; // Ensure it's exactly at the target position

        // Move down
        elapsedTime = 0f;
        while (elapsedTime < duration / 2 && doubleProfitPanel != null)
        {
            doubleProfitPanel.anchoredPosition = Vector2.Lerp(targetPosition, originalPosition, elapsedTime / (duration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // If panel closes while bobbing, then this is null because it gets destroyed
        if (doubleProfitPanel == null)
        {
            yield break;
        }

        doubleProfitPanel.anchoredPosition = originalPosition; // Ensure it's exactly at the original position

        doubleProfitPanel.gameObject.SetActive(false);
    }

    public int[] GetOriginalMaterialPrices()
    {
        return materialPrices;
    }

    public int GetOriginalTileIndexByName(string oreName)
    {
        for (int i = 0; i != materialNames.Length; i++) {
            if (materialNames[i] == oreName) {
                return i;
            }
        }

        // Shouldnt reach here
        return 0;
    }

    public bool VerifyIfOre(int tileIndex) {
        return isOre[tileIndex];
    }

    public string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key);;

        // If no translation, just return the key
        if (entry == null) {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }
}