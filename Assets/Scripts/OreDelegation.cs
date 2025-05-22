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

        for (int i = 0; i != mineRenderer.selectedMaterialNames.Length; i++)
        {
            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(contentGO.transform);

            panelTransform.localScale = new(1, 1, 1);

            string oreName = mineRenderer.selectedMaterialNames[i];
            // Set the price, name and image
            panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(mineRenderer.refineryUpgradePad.GetActualMaterialPrice(i));
            panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("LEVEL {0}", mineRenderer.refineryUpgradePad.GetOreUpgradeLevel(i));
            panelTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = oreName;
            panelTransform.GetChild(4).GetComponent<Image>().sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];

            // 500 is max level currently
            if (mineRenderer.refineryUpgradePad.GetOreUpgradeLevel(i) >= 500)
            {
                // Disable button if max
                panelTransform.GetChild(5).GetComponent<Button>().interactable = false;
                panelTransform.GetChild(5).GetComponent<Image>().color = new(1, 0, 0);

                // Hide price tag, show MAX text
                panelTransform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                panelTransform.GetChild(5).GetChild(1).gameObject.SetActive(true);
            }
            else
            {
                // Add on click listener to button and save index in its own variable, otherwise it will keep a reference to i
                int oreIndex = i;
                panelTransform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => mineRenderer.refineryUpgradePad.PurchaseOreUpgrade(oreIndex));
                panelTransform.GetChild(5).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(mineRenderer.refineryUpgradePad.GetMaterialUpgradePrice(i));
            }
            
        }

        int rows = 3;
        // 200 = vertical padding
        // (rows - 1) * 100 = spacing between each row
        float bigContentHeight = oreMaterialPanel.GetComponent<RectTransform>().sizeDelta.y * rows + 200 + ((rows - 1) * 100);
        
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

    public string FormatPrice(double price)
    {
        if (price >= 1_000_000_000_000_000_000_000d)
        {
            // Truncate to 2 decimal places and format with "ac"
            return (System.Math.Floor(price / 1_000_000_000_000_000_000_000d * 100d) / 100d).ToString("0.##") + "ac";
        }
        else if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 2 decimal places and format with "ab"
            return (System.Math.Floor(price / 1_000_000_000_000_000_000 * 100d) / 100d).ToString("0.##") + "ab";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 2 decimal places and format with "aa"
            return (System.Math.Floor(price / 1_000_000_000_000_000 * 100d) / 100d).ToString("0.##") + "aa";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 2 decimal places and format with "T"
            return (System.Math.Floor(price / 1_000_000_000_000 * 100d) / 100d).ToString("0.##") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 2 decimal places and format with "B"
            return (System.Math.Floor(price / 1_000_000_000 * 100d) / 100d).ToString("0.##") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 2 decimal places and format with "M"
            return (System.Math.Floor(price / 1_000_000 * 100d) / 100d).ToString("0.##") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 2 decimal places and format with "K"
            return (System.Math.Floor(price / 1_000 * 100d) / 100d).ToString("0.##") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
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