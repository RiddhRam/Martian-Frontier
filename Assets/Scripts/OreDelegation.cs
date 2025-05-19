using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class OreDelegation : MonoBehaviour
{
    private MineRenderer mineRenderer;
    public string[] materialNames;
    // The price of each material, before boosts
    // Aligns with materialCount's index from HaulerController
    [SerializeField] private int[] materialPrices;
    public Sprite[] materialHighResSprites;
    public TileBase[] oreTileValues;
    public Color[] oreTileColours;
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

        for (int i = 0; i != mineRenderer.selectedMaterialNames.Length; i++) {

            long price = materialPrices[i];

            GameObject newMaterialPanel = Instantiate(oreMaterialPanel);
            Transform panelTransform = newMaterialPanel.transform;
            // Add panel to the content scroll view of the right tier panel
            // This should just be a regular panel with a photo
            panelTransform.SetParent(contentGO.transform);

            panelTransform.localScale = new(1, 1, 1);

            string oreName =  mineRenderer.selectedMaterialNames[i];
            // Set the name and price
            panelTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(price);
            panelTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = oreName;
            panelTransform.GetChild(3).GetComponent<Image>().sprite = materialHighResSprites[GetOriginalTileIndexByName(oreName)];
        }

        int rows = 3;
        float bigContentHeight = oreMaterialPanel.GetComponent<RectTransform>().sizeDelta.y * rows + 200;
        
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

    private string FormatPrice(long price)
    {
        if (price >= 1_000_000)
        {
            // Truncate to 2 decimal places and format with "M"
            return (Mathf.Floor(price / 1_000_000f * 1000) / 1000).ToString("0.##") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 2 decimal places and format with "K"
            return (Mathf.Floor(price / 1_000f * 1000) / 1000).ToString("0.##") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public bool VerifyIfOre(int tileIndex) {
        return isOre[tileIndex];
    }
}