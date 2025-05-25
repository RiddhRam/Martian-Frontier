using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;

public class OreDelegation : MonoBehaviour
{
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
    private TextMeshProUGUI[] materialLevelTexts;
    private TextMeshProUGUI[] materialPriceTexts;
    private TextMeshProUGUI[] materialUpgradePriceTexts;

    private int[] oresPerTier;
    // Lowercase verion of materialNames
    private bool[] isOre;

    void Awake() {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
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
        materialLevelTexts = new TextMeshProUGUI[length];
        materialPriceTexts = new TextMeshProUGUI[length];
        materialUpgradePriceTexts = new TextMeshProUGUI[length];

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
            panelTransform.GetChild(0).GetChild(0).GetComponent<Outline>().effectColor = oreTileColours[GetOriginalTileIndexByName(oreName)];
            panelTransform.GetChild(0).GetChild(1).GetComponent<Image>().color = oreTileColours[GetOriginalTileIndexByName(oreName)];

            // Set the price, level, name, image and upgrade button
            materialPriceTexts[i] = panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
            materialPriceTexts[i].text = mineRenderer.refineryUpgradePad.playerState.FormatPrice(new System.Numerics.BigInteger(mineRenderer.refineryUpgradePad.GetActualMaterialPrice(i)));

            materialLevelTexts[i] = panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>();
            materialLevelTexts[i].text = GetLocalizedValue("LEVEL {0}", mineRenderer.refineryUpgradePad.GetOreUpgradeLevel(i));

            panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = oreName;

            panelTransform.GetChild(4).GetComponent<Image>().sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];

            materialUpgradePriceTexts[i] = panelTransform.GetChild(5).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();

            // Save as its own variable, otherwise it keeps a reference to the variable i
            int oreIndex = i;
            // Add onclick listener and hold button component
            panelTransform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => mineRenderer.refineryUpgradePad.PurchaseOreUpgrade(oreIndex));
            // Hold to purchase
            HoldButton holdButton = panelTransform.GetChild(5).gameObject.AddComponent<HoldButton>();
            holdButton.SetAction(() => mineRenderer.refineryUpgradePad.PurchaseOreUpgrade(oreIndex));

            UpdateOreMaterialPanelText(i);
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

    public void UpdateOreMaterialPanelText(int oreIndex)
    {
        materialPriceTexts[oreIndex].text = mineRenderer.refineryUpgradePad.playerState.FormatPrice(new System.Numerics.BigInteger(mineRenderer.refineryUpgradePad.GetActualMaterialPrice(oreIndex)));
        materialLevelTexts[oreIndex].text = GetLocalizedValue("LEVEL {0}", mineRenderer.refineryUpgradePad.GetOreUpgradeLevel(oreIndex));

        Transform buttonTransform = materialUpgradePriceTexts[oreIndex].transform.parent.parent;
        // 500 is max level currently
        if (mineRenderer.refineryUpgradePad.GetOreUpgradeLevel(oreIndex) >= mineRenderer.refineryUpgradePad.GetMaxOreLevel())
        {
            // Disable button if max
            buttonTransform.GetComponent<Button>().interactable = false;
            buttonTransform.GetComponent<Image>().color = new(1, 0, 0);

            // Hide price tag, show MAX text
            buttonTransform.GetChild(0).gameObject.SetActive(false);
            buttonTransform.GetChild(1).gameObject.SetActive(true);

            // Destroy hold button component
            if (buttonTransform.TryGetComponent(out HoldButton hold))
            {
                Destroy(hold);
            }
        }
        else
        {
            // Update price text
            materialUpgradePriceTexts[oreIndex].text = mineRenderer.refineryUpgradePad.playerState.FormatPrice(new System.Numerics.BigInteger(mineRenderer.refineryUpgradePad.GetMaterialUpgradePrice(oreIndex)));
        }
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