using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UIDelegation : MonoBehaviour
{
    public GameObject mapCamera;
    public GameObject mapCameraView;
    private RenderTexture renderTexture;
    public GameObject scrollViewContent;
    public GameObject playerVehicle;
    public GameObject sliderCount;
    public GameObject destroyButton;
    public GameObject oreRefineryCanvas;

    // Higher resolution UI version of the minerals, because they will be larger now in the cargo panel
    // The first elements a user sees, these are the ones they see while playing the game
    // Secondary elements are the menus they open like the shop or map camera
    public GameObject[] primaryElements;
    //private string[] materialNames;
    public GameObject materialButton;
    public GameObject errorMessage;
    public GameObject[] cargoProgressBars;
    public GameObject[] cargoCounters;
    private Sprite[] materialHighResSprites;
    private string[] materialNames;
    private bool showCargoInfo;
    private GameCameraController mainCameraController;
    private AnalyticsDelegator analyticsDelegator;
 
    void Start()
    {
        ToggleCargoInfo(false);
        materialNames = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().materialNames;
        materialHighResSprites = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().materialHighResSprites;
        mainCameraController = Camera.main.GetComponent<GameCameraController>();
        analyticsDelegator = AnalyticsDelegator.Instance;
    }

    public void ToggleCargoInfo(bool newValue) {
        showCargoInfo = newValue;
    }

    // Hide all base elements, and only used before opening a secondary element like the camera
    public void HideAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(false);
        }
    }

    // Used after closing a secondary element
    public void RevealAll() {

        if (playerVehicle.transform.GetChild(0).GetComponent<HaulerController>()) {
            showCargoInfo = true;
        } else {
            showCargoInfo = false;
        }

        for (int i = 0; i < primaryElements.Length; i++) {
            // Reset all buttons back to scale 1. 
            // Need to do this because the button that was pressed down will be at 0.95 still 
            // since it didn't get the pointer up event if it was clicked
            UIButton uiButton = primaryElements[i].GetComponent<UIButton>();
            if (uiButton) {
                StartCoroutine(uiButton.ResetScale());
            }

            // If its the cargo button, and its supposed to stay hidden, dont reveal it
            if (primaryElements[i].name.Contains("Cargo") && !showCargoInfo) {
                continue;
            }

            primaryElements[i].SetActive(true);
        }
    }

    // Reveal a single element, typically a secondary element, and only used after HideAll()
    public void RevealElement(GameObject element) {
        element.SetActive(true);
        mainCameraController.ToggleZooming(false);
        analyticsDelegator.OpenUIPanel(element.name);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
        mainCameraController.ToggleZooming(true);
    }

    // Used when opening the map, or closing
    public void ToggleCamera() {
        mapCamera.SetActive(!mapCamera.activeSelf);

        // Make sure its active
        if (!mapCamera.activeSelf) {
            return;
        }

        primaryElements[7].SetActive(true);

        float aspectRatio = (float) Screen.height / Screen.width;

        if (aspectRatio >= 1.7) {
            aspectRatio /= 1.12f;
        } else {
            aspectRatio /= 1.15f;
        }
        
        // Create a new RenderTexture
        renderTexture = new RenderTexture((int) (Screen.height / aspectRatio), Screen.height, 24, RenderTextureFormat.ARGB32); // 24 is the depth buffer bit size
        renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.S8_UInt;
        renderTexture.Create();

        // Assign the RenderTexture to the mapCamera's target texture
        mapCamera.GetComponent<Camera>().targetTexture = renderTexture;
        mapCameraView.GetComponent<RawImage>().texture = renderTexture;
    }

    // Used when clicking the cargo button to prepare the columns and rows of the Content in the scrollview
    public void PrepareCargoGrid() {
        // Get the child of player vehicle, which should be a hauler, otherwise the cargo button would not show
        // Then get the HaulerController script of that hauler's game object
        // Then get it's material count
        int[] materialCount = playerVehicle.transform.GetChild(0).gameObject.GetComponent<HaulerController>().GetMaterialCount();
    
        int itemsToDisplay = 0;

        for (int i = 0; i != materialCount.Length; i++) {

            // Should never be less than but just in case
            if (materialCount[i] <= 0) {
                materialCount[i] = 0;
                continue;
            }

            // Create the material button
            GameObject newMaterialButton = Instantiate(materialButton);
            // Add it to the content scroll view
            newMaterialButton.transform.SetParent(scrollViewContent.transform);
            
            // Set up material manager ui
            MaterialManagerUI materialManagerUI = newMaterialButton.GetComponent<MaterialManagerUI>();
            materialManagerUI.SetCount(materialCount[i]);
            materialManagerUI.materialName = materialNames[i];
            materialManagerUI.materialIndex = i;

            newMaterialButton.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = materialNames[i].ToUpper();
            newMaterialButton.transform.GetChild(3).GetComponent<Image>().sprite = materialHighResSprites[i];
            
            newMaterialButton.transform.localScale = new(1, 1, 1);

            itemsToDisplay++;

            // Get the Button component
            Button button = newMaterialButton.GetComponent<Button>();

            // Add an OnClick listener to the button
            button.onClick.AddListener(() => OnMaterialButtonClick(newMaterialButton));
            
        }

        // Resize the content panel
        GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
        int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / (gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x)));
        int rows = Mathf.CeilToInt((float) itemsToDisplay / columns);

        RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();

        // Resize the scroll view content height to fit the rows (top padding + cell height * rows + vertical spacing between cell rows * (rows - 1))
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 35 + 1000 * rows + 40 * (rows - 1));
        scrollViewContent.GetComponent<RectTransform>().sizeDelta = new (0, contentRect.sizeDelta.y);
    }

    // Empty the grid for next time
    public void EmptyCargoGrid() {
        // Loop through all children of scrollViewContent and destroy each one
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }

        // These shouldn't be usable anymore
        sliderCount.GetComponent<Slider>().interactable = false;
        sliderCount.GetComponent<Slider>().value = 0;
        destroyButton.GetComponent<Button>().interactable = false;
    }

    // Could be in one line but whatever
    private void OnMaterialButtonClick(GameObject materialSelected)
    {
        destroyButton.GetComponent<DestroyMaterial>().SelectMaterial(materialSelected);
    }

    public void ShowError(string error, params object[] args) {
        GameObject errorInstance = Instantiate(errorMessage);

        string message = GetLocalizedValue(error, args);
        errorInstance.GetComponent<TextMeshProUGUI>().text = message;

        // Place it within the safe area
        errorInstance.transform.SetParent(transform.GetChild(0), false);
        errorInstance.transform.localPosition = new(0, 400 ,0);

        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.ShowError(error);
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        // Get the localized string using the key
        var entry = table.GetEntry(key);

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public GameObject[] GetCargoProgressBars() {
        return cargoProgressBars;
    }

    public GameObject[] GetCargoCounters() {
        return cargoCounters;
    }
}
