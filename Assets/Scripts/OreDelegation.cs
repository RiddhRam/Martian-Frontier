using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    // Lowercase verion of materialNames
    private string[] oreNames;
    private bool[] isOre;

    void Awake() {
        oreNames = new string[materials.Length];
        for (int i = 0; i != materials.Length; i++) {
            oreNames[i] = materials[i].name;
        }
    }

    void Start() {
        oresPerTier = GameObject.Find("Mine").GetComponent<MineRenderer>().oresPerTier;

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

        for (int i = 0; i != materialNames.Length; i++) {

            long price = materialPrices[i];

            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(contentGO.transform);

            panelTransform.localScale = new(1, 1, 1);

            // Set the name and price
            panelTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(price);
            panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = materialNames[i];
            panelTransform.GetChild(3).GetComponent<Image>().sprite = materialHighResSprites[i];
        }

        int rows = 3;
        float bigContentHeight = oreMaterialPanel.GetComponent<RectTransform>().sizeDelta.y * rows;
        
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

    public string[] GetOreNames() {
        return oreNames;
    }

    public int GetTileIndexByName(string name) {
        for (int i = 0; i != materialNames.Length; i++) {
            if (materialNames[i] == name) {
                return i;
            }
        }

        // Shouldnt reach here
        return 0;
    }

    public bool VerifyIfOre(int tileIndex) {
        return isOre[tileIndex];
    }
}