using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class OreDelegation : MonoBehaviour
{
    public string[] materialNames;
    public GameObject[] materials;
    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    [SerializeField]
    private int[] materialPrices;
    public Sprite[] materialHighResSprites;
    public GameObject oreMaterialTierPanel;
    public GameObject oreMaterialPanel;
    public GameObject contentGO;
    private int[] oresPerTier;
    private string[] oreNames;

    void Start() {
        oresPerTier = GameObject.Find("Mine").GetComponent<MineRenderer>().oresPerTier;

        oreNames = new string[materials.Length];
        for (int i = 0; i != materials.Length; i++) {
            oreNames[i] = materials[i].name;
        }
    }

    public void PrepareGrid() {
        GameObject[] tierPanels = new GameObject[oresPerTier.Length];

        // Create a tier panel for each tier
        for (int i = 0; i != 3; i++) {
            GameObject newTierPanel = Instantiate(oreMaterialTierPanel);
            tierPanels[i] = newTierPanel;
            Transform panelTransform = tierPanels[i].transform;
            panelTransform.SetParent(contentGO.transform);
            // Have to make sure scale is right
            panelTransform.localScale = new(1, 1, 1);
            // Get the right translation
            string tierString = GetLocalizedValue("TIER {0}", i+1);
            // Set the name
            panelTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = tierString;
        }

        // Track number of items in each tier, to dynamically resize content height based on rows
        int[] tierItems = new int[3];

        // track current tier
        int tier = 0;
        // track index of current tier
        int counter = 0;

        for (int i = 0; i != materialNames.Length; i++) {

            if (counter >= oresPerTier[0]) {
                counter = 0;
                tier++;
            }

            long price = materialPrices[i];

            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(tierPanels[tier].transform.GetChild(1));

            panelTransform.localScale = new(1, 1, 1);

            // Set the name and price
            panelTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);
            panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = materialNames[i];
            panelTransform.GetChild(3).GetComponent<Image>().sprite = materialHighResSprites[i];
            
            counter++;
            tierItems[tier]++;
        }

        float bigContentHeight = 0;
        // Resize each tier panel
        for (int i = 0; i != 3; i++) {
            Transform scrollViewContent = tierPanels[i].transform.GetChild(1);
            // Calculate the number of rows
            GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
            int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
            int rows = Mathf.CeilToInt((float) tierItems[i] / columns);

            // Resize the scroll view content height to fit the rows (top padding of tier panels + cell height * rows + vertical spacing between cell rows * (rows - 1))
            RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 50 + 1000 * rows + 40 * (rows - 1));
            tierPanels[i].GetComponent<RectTransform>().sizeDelta = new (0, contentRect.sizeDelta.y);
            bigContentHeight += contentRect.sizeDelta.y;
        }

        RectTransform bigContentRect = contentGO.GetComponent<RectTransform>();
        // Resize the scroll view content height to fit the rows using the height of all panels and then factor in the spacing * (tiers - 1) which is (150 * 2) currently
        bigContentRect.sizeDelta = new Vector2(bigContentRect.sizeDelta.x, 150 + bigContentHeight + 150 * (tierPanels.Length - 1));
    }

    // Clear grid when closing, then reprepare it in case user changes language
    public void ClearGrid() {
        int childCount = contentGO.transform.childCount;

        for (int i = 0; i != childCount; i++) {
            Destroy(contentGO.transform.GetChild(i).gameObject);
        }
    }

    public int[] GetMaterialPrices() {
        return materialPrices;
    }
    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor(price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor(price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor(price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor(price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor(price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor(price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public string[] GetOreNames() {
        return oreNames;
    }

}