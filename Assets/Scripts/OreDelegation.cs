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
    public GameObject oreMaterialPanel;
    public GameObject contentGO;

    // Track components of each ore panel
    private Outline[] orePanelOutlines;
    private Image[] orePanelOutlineBars;
    private TextMeshProUGUI[] materialLevelTexts;
    private TextMeshProUGUI[] materialPriceTexts;
    private TextMeshProUGUI[] materialUpgradePriceTexts;
    private Slider[] levelProgressBars;
    private RectTransform[] milestoneTransforms;
    private ButtonAffordability[] buttonAffordabilities;

    private int[] oresPerTier;
    // Lowercase verion of materialNames
    private bool[] isOre;
    private Coroutine flashOutlineCoroutine;

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
        Debug.Log("Generating: " + PlayerPrefs.GetString("Cohort", "No Cohort"));
        string cohort = PlayerPrefs.GetString("Cohort", "No Cohort");

        // A/B testing a new refinery upgrade panel
        if (cohort == "A" || cohort == "B")
        {
            ClearAlternateGrid();
            PrepareAlternateGrid();
            return;
        }

        // Make sure everything else is clear for sure
        ClearGrid();

        int length = mineRenderer.selectedMaterialNames.Length;
        orePanelOutlines = new Outline[length];
        orePanelOutlineBars = new Image[length];
        materialLevelTexts = new TextMeshProUGUI[length];
        materialPriceTexts = new TextMeshProUGUI[length];
        materialUpgradePriceTexts = new TextMeshProUGUI[length];
        levelProgressBars = new Slider[length];
        milestoneTransforms = new RectTransform[length];
        buttonAffordabilities = new ButtonAffordability[length];

        int requiredOreIndex = RefineryUpgradePad.Instance.GetRequiredOreIndex();
        int requiredOreIndexTier = MineRenderer.Instance.GetOreTierByIndex(requiredOreIndex);

        int generatedPanels = 0;

        for (int i = 0; i != length; i++)
        {
            // Determine which ores to show
            bool foundOre = true;

            // Always show the required ore no matter what
            // Only show the other ores if player has previously mined this ore, otherwise show it as a mystery ore
            // If the ore's tier is greater than the tier of the required ore, don't show anything at all after this index
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

            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(contentGO.transform);

            panelTransform.localScale = new(1, 1, 1);

            string oreName = mineRenderer.selectedMaterialNames[i];

            // Set outline colour and top bar colour
            orePanelOutlines[i] = panelTransform.GetChild(0).GetChild(0).GetComponent<Outline>();
            orePanelOutlineBars[i] = panelTransform.GetChild(0).GetChild(1).GetComponent<Image>();
            materialPriceTexts[i] = panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
            materialLevelTexts[i] = panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>();
            materialUpgradePriceTexts[i] = panelTransform.GetChild(5).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();

            if (foundOre)
            {
                // If found the ore, show its price, level, name, image and colours
                //orePanelOutlines[i].effectColor = oreTileColours[GetOriginalTileIndexByName(oreName)];
                //orePanelOutlineBars[i].color = oreTileColours[GetOriginalTileIndexByName(oreName)];

                panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = oreName;

                materialPriceTexts[i].text = RefineryUpgradePad.Instance.playerState.FormatPrice(new System.Numerics.BigInteger(RefineryUpgradePad.Instance.GetActualMaterialPrice(i)));
                materialLevelTexts[i].text = GetLocalizedValue("LEVEL {0}", RefineryUpgradePad.Instance.GetOreUpgradeLevel(i));

                Image image = panelTransform.GetChild(4).GetComponent<Image>();
                image.sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];
                image.color = new(1, 1, 1);

                // Save as its own variable, otherwise it keeps a reference to the variable i
                int oreIndex = i;
                // Add onclick listener and hold button component
                panelTransform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => RefineryUpgradePad.Instance.PurchaseOreUpgrade(oreIndex));
                // Hold to purchase
                HoldButton holdButton = panelTransform.GetChild(5).gameObject.AddComponent<HoldButton>();
                holdButton.SetAction(() => RefineryUpgradePad.Instance.PurchaseOreUpgrade(oreIndex));
                // Button affordability
                buttonAffordabilities[i] = panelTransform.GetChild(5).GetComponent<ButtonAffordability>();

                levelProgressBars[i] = panelTransform.GetChild(6).GetComponent<Slider>();

                milestoneTransforms[i] = panelTransform.GetChild(7).GetComponent<RectTransform>();

                // We pass false for 'reachedMilestone', even though it may have been reached because it shouldn't show anything at all
                UpdateOreMaterialPanel(i, false, false);
            }
            else
            {
                orePanelOutlines[i].effectColor = Color.black;
                orePanelOutlineBars[i].color = Color.black;

                // If not found, then show as mystery ore
                panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("NOT FOUND");

                materialPriceTexts[i].text = "?";

                materialLevelTexts[i].text = "--";
                materialLevelTexts[i].color = Color.white;

                panelTransform.GetChild(5).GetComponent<Button>().interactable = false;

                Destroy(panelTransform.GetChild(5).GetComponent<ButtonAffordability>());

                materialUpgradePriceTexts[i].text = "--";
            }

            generatedPanels++;

            // For tutorial
            if (i == 0)
            {
                RefineryUpgradePad.Instance.limestoneUpgradeImage = newMaterialPanel.transform.GetChild(5).GetComponent<Image>();
            }
        }

        // 3 per row
        int rows = (int)Math.Ceiling(generatedPanels / 3d);

        GridLayoutGroup contentGridLayoutGroup = contentGO.GetComponent<GridLayoutGroup>();
        float bigContentHeight = oreMaterialPanel.GetComponent<RectTransform>().sizeDelta.y * rows + contentGridLayoutGroup.padding.top + contentGridLayoutGroup.padding.bottom + ((rows - 1) * contentGridLayoutGroup.spacing.y);

        // Clamp height and adjust padding
        float minHeight = contentGO.transform.parent.parent.GetComponent<RectTransform>().rect.height;
        if (bigContentHeight < minHeight)
        {
            bigContentHeight = minHeight;
            contentGridLayoutGroup.padding.top = 0;
            contentGridLayoutGroup.padding.bottom = 350;
        }

        RectTransform bigContentRect = contentGO.GetComponent<RectTransform>();

        // Resize the scroll view content height to fit the rows using the height of all panels
        bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight);

        ScrollRect scrollRect = contentGO.transform.parent.parent.GetComponent<ScrollRect>();

        // If theres only 1 row, it will be in the Middle-Center by default
        // If there's more, make it Upper-Center
        if (rows > 1)
        {
            contentGridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        }

        // If larger than the minimum height, then scroll to make the target depth ores in the view
        if (bigContentHeight > minHeight)
        {
            // 0 = bottom, 1 = top

            // If first row, manually set it to the top
            if (PlayerState.Instance.GetRecommendedDrillTier() == 1)
            {
                scrollRect.verticalNormalizedPosition = 1;
            }
            // Otherwise calculate the position
            else
            {
                scrollRect.verticalNormalizedPosition = 1f - ((float)PlayerState.Instance.GetRecommendedDrillTier() / rows);
            }

            scrollRect.vertical = true;
        }
        else
        {
            scrollRect.vertical = false;
        }
    }

    public void PrepareAlternateGrid()
    {
        
    }

    // Clear grid when closing, then reprepare it when opening in case user changes language
    public void ClearGrid()
    {
        int childCount = contentGO.transform.childCount;

        for (int i = 0; i != childCount; i++)
        {
            Destroy(contentGO.transform.GetChild(i).gameObject);
        }
    }

    public void ClearAlternateGrid() {

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

        if (RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex) >= RefineryUpgradePad.Instance.GetRequiredOreUpgradeLevel())
        {
            // Hide price tag, show MAX text
            buttonTransform.GetChild(0).gameObject.SetActive(false);
            buttonTransform.GetChild(1).gameObject.SetActive(true);

            // Destroy hold button component, it may not have been added yet in some edge cases, that's why we do this
            if (buttonTransform.TryGetComponent(out HoldButton hold))
            {
                Destroy(hold);
            }

            Destroy(buttonAffordabilities[oreIndex]);

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
        levelProgressBars[oreIndex].maxValue = RefineryUpgradePad.Instance.GetNextOreMilestone(oreIndex) - lastMilestone;
        levelProgressBars[oreIndex].value = RefineryUpgradePad.Instance.GetOreUpgradeLevel(oreIndex) - lastMilestone;

        // If we should flash the outline (an upgrade was made)
        if (flashOutline)
        {
            // If there is no outline currently flashing
            if (flashOutlineCoroutine == null)
            {
                flashOutlineCoroutine = StartCoroutine(FlashOrePanelOutline(orePanelOutlines[oreIndex], orePanelOutlineBars[oreIndex]));
            }

            // If upgrade milestone was reached
            if (reachedMilestone)
            {
                StartCoroutine(BobMilestonePanel(milestoneTransforms[oreIndex]));
            }
        }
    }

    private IEnumerator FlashOrePanelOutline(Outline outlineToFlash, Image outlineBarToFlash)
    {
        Color originalColor = outlineToFlash.effectColor;
        // Don't multiply the alpha by 0.5
        Color darkerColor = new Color(originalColor.r * 0.5f, originalColor.g * 0.5f, originalColor.b * 0.5f, originalColor.a);

        float duration = 0.25f;
        float halfDuration = duration / 2f;

        Color newColor;

        // Transition to darker color
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            newColor = Color.Lerp(originalColor, darkerColor, t / halfDuration);
            outlineToFlash.effectColor = newColor;
            outlineBarToFlash.color = newColor;
            yield return null;
        }

        // Transition back to original color
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            newColor = Color.Lerp(darkerColor, originalColor, t / halfDuration);
            outlineToFlash.effectColor = newColor;
            outlineBarToFlash.color = newColor;
            yield return null;
        }

        outlineToFlash.effectColor = originalColor; // Ensure final color is exact
        outlineBarToFlash.color = originalColor;

        flashOutlineCoroutine = null;
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