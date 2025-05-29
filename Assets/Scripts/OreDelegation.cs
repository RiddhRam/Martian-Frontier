using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using System.Collections;

public class OreDelegation : MonoBehaviour
{
    private MineRenderer mineRenderer;
    private RefineryUpgradePad refineryUpgradePad;

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
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        refineryUpgradePad = mineRenderer.refineryUpgradePad;
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

    public void PrepareGrid() {

        int length = mineRenderer.selectedMaterialNames.Length;
        orePanelOutlines = new Outline[length];
        orePanelOutlineBars = new Image[length];
        materialLevelTexts = new TextMeshProUGUI[length];
        materialPriceTexts = new TextMeshProUGUI[length];
        materialUpgradePriceTexts = new TextMeshProUGUI[length];
        levelProgressBars = new Slider[length];
        milestoneTransforms = new RectTransform[length];
        buttonAffordabilities = new ButtonAffordability[length];

        for (int i = 0; i != length; i++)
        {
            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(contentGO.transform);

            panelTransform.localScale = new(1, 1, 1);

            string oreName = mineRenderer.selectedMaterialNames[i];

            // Set outline colour and top bar colour
            orePanelOutlines[i] = panelTransform.GetChild(0).GetChild(0).GetComponent<Outline>();
            orePanelOutlines[i].effectColor = oreTileColours[GetOriginalTileIndexByName(oreName)];

            orePanelOutlineBars[i] = panelTransform.GetChild(0).GetChild(1).GetComponent<Image>();
            orePanelOutlineBars[i].color = oreTileColours[GetOriginalTileIndexByName(oreName)];

            // Set the price, level, name, image and upgrade button
            materialPriceTexts[i] = panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
            materialPriceTexts[i].text = refineryUpgradePad.playerState.FormatPrice(new System.Numerics.BigInteger(refineryUpgradePad.GetActualMaterialPrice(i)));

            materialLevelTexts[i] = panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>();
            materialLevelTexts[i].text = GetLocalizedValue("LEVEL {0}", refineryUpgradePad.GetOreUpgradeLevel(i));

            panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = oreName;

            panelTransform.GetChild(4).GetComponent<Image>().sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];

            materialUpgradePriceTexts[i] = panelTransform.GetChild(5).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();

            // Save as its own variable, otherwise it keeps a reference to the variable i
            int oreIndex = i;
            // Add onclick listener and hold button component
            panelTransform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => refineryUpgradePad.PurchaseOreUpgrade(oreIndex));
            // Hold to purchase
            HoldButton holdButton = panelTransform.GetChild(5).gameObject.AddComponent<HoldButton>();
            holdButton.SetAction(() => refineryUpgradePad.PurchaseOreUpgrade(oreIndex));
            // Button affordability
            buttonAffordabilities[i] = panelTransform.GetChild(5).GetComponent<ButtonAffordability>();

            levelProgressBars[i] = panelTransform.GetChild(6).GetComponent<Slider>();

            milestoneTransforms[i] = panelTransform.GetChild(7).GetComponent<RectTransform>();

            // We pass false for 'reachedMilestone', even though it may have been reached because it shouldn't show anything at all
            UpdateOreMaterialPanel(i, false, false);
        }

        int rows = 3;
        // 130 = vertical padding
        // (rows - 1) * 200 = spacing between each row
        float bigContentHeight = oreMaterialPanel.GetComponent<RectTransform>().sizeDelta.y * rows + 130 + ((rows - 1) * 150);
        
        RectTransform bigContentRect = contentGO.GetComponent<RectTransform>();
        // Resize the scroll view content height to fit the rows using the height of all panels
        bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, bigContentHeight);
    }

    // Clear grid when closing, then reprepare it when opening in case user changes language
    public void ClearGrid() {
        int childCount = contentGO.transform.childCount;

        for (int i = 0; i != childCount; i++) {
            Destroy(contentGO.transform.GetChild(i).gameObject);
        }
    }

    public void UpdateOreMaterialPanel(int oreIndex, bool flashOutline, bool reachedMilestone)
    {
        // Update text
        materialPriceTexts[oreIndex].text = refineryUpgradePad.playerState.FormatPrice(new System.Numerics.BigInteger(refineryUpgradePad.GetActualMaterialPrice(oreIndex)));
        materialLevelTexts[oreIndex].text = GetLocalizedValue("LEVEL {0}", refineryUpgradePad.GetOreUpgradeLevel(oreIndex));

        Transform buttonTransform = materialUpgradePriceTexts[oreIndex].transform.parent.parent;
        Button button = buttonTransform.GetComponent<Button>();

        System.Numerics.BigInteger newPrice = new(refineryUpgradePad.GetMaterialUpgradePrice(oreIndex));

        // If player can't afford, make it disabled initially. Otherwise it will show up as interactable for a split second
        button.interactable = !(newPrice > refineryUpgradePad.playerState.GetUserCash());
        
        // 500 is max level currently
        if (refineryUpgradePad.GetOreUpgradeLevel(oreIndex) >= refineryUpgradePad.GetMaxOreLevel())
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
            materialUpgradePriceTexts[oreIndex].text = refineryUpgradePad.playerState.FormatPrice(newPrice);
            buttonAffordabilities[oreIndex].price = newPrice;
        }

        int lastMilestone = refineryUpgradePad.GetLastOreMilestone(oreIndex);

        // Update progress bar
        levelProgressBars[oreIndex].maxValue = refineryUpgradePad.GetNextOreMilestone(oreIndex) - lastMilestone;
        levelProgressBars[oreIndex].value = refineryUpgradePad.GetOreUpgradeLevel(oreIndex) - lastMilestone;

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
        Color darkerColor = originalColor * 0.5f; // Darken by reducing brightness

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
        while (elapsedTime < duration / 2)
        {
            doubleProfitPanel.anchoredPosition = Vector2.Lerp(targetPosition, originalPosition, elapsedTime / (duration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
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