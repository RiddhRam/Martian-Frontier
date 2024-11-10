using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDelegation : MonoBehaviour
{
    public GameObject mapCamera;
    public GameObject scrollViewContent;
    public GameObject playerVehicle;
    public GameObject sliderCount;
    public GameObject destroyButton;

    // Higher resolution UI version of the minerals, because they will be larger now in the cargo panel
    public GameObject IronMaterialButton;
    public GameObject SulfurMaterialButton;
    public GameObject LimestoneMaterialButton;

    // The first elements a user sees, these are the ones they see while playing the game
    // Secondary elements are the menus they open like the shop or map camera
    private GameObject[] primaryElements;
    //private string[] materialNames;
    private GameObject[] materialButtons;

    //private int materialButtonNameFontSize = 27;
    private int materialButtonCountFontSize = 35;

    void Start()
    {
        UpdatePrimaryElements();
        //materialNames = GameObject.Find("Mine").GetComponent<MineRenderer>().GetMaterialNames();
        materialButtons = new GameObject[] { LimestoneMaterialButton, SulfurMaterialButton, IronMaterialButton };
        ResizeContentCell(scrollViewContent);

        int baseButtonFontSize = 48;
        float referenceWidth = 1080f;
        float referenceHeight = 1920f;

        // Calculate scaling factors based on the current screen resolution
        float widthScaleFactor = Screen.width / referenceWidth;
        float heightScaleFactor = Screen.height / referenceHeight;

        int adjustedFontSize = Mathf.RoundToInt(baseButtonFontSize * widthScaleFactor);

        foreach (GameObject button in primaryElements)
        {
            // Adjust TextMeshPro font size
            TextMeshProUGUI buttonText = button.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (buttonText != null) {
                buttonText.fontSize = adjustedFontSize;
            }

            // Adjust RectTransform width, height, and position
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                // Scale width and height
                buttonRect.sizeDelta = new Vector2(
                    buttonRect.sizeDelta.x * widthScaleFactor,
                    buttonRect.sizeDelta.y * heightScaleFactor
                );

                // Scale position relative to screen height
                buttonRect.anchoredPosition = new Vector2(
                    buttonRect.anchoredPosition.x * widthScaleFactor,
                    buttonRect.anchoredPosition.y * heightScaleFactor
                );
            }
        }
    }

    public void UpdatePrimaryElements() {
        // First, count how many active children there are
        int activeCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
            {
                activeCount++;
            }
        }

        // Now, create an array of the correct size
        primaryElements = new GameObject[activeCount];

        // This is the index of the primaryElements array that we add to
        // meanwhile i in the for loop below is the current iteration of all children
        int currentIndex = 0;
        // Fill the array with active children
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
            {
                primaryElements[currentIndex] = transform.GetChild(i).gameObject;
                currentIndex++;
            }
        }
    }

    // Hide all base elements, and only used before opening a secondary element like the camera
    public void HideAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(false);
        }
    }

    // Used after closing a secondary element
    public void RevealAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(true);
        }
    }

    // Reveal a single element, typically a secondary element, and only used after HideAll()
    public void RevealElement(GameObject element) {
        element.SetActive(true);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
    }

    // Used when opening the map, or closing
    public void ToggleCamera() {
        mapCamera.SetActive(!mapCamera.activeSelf);

        // If camera is active reduce framerate
        // This might not be neccessary it made no noticeable difference in singleplayer
        // May be needed for multiplayer
        if (mapCamera.activeSelf) {
            Application.targetFrameRate = 10;
            return;
        }

        // If not, set back to 60
        Application.targetFrameRate = 60;
    }

    // Used when clicking the cargo button to prepare the columns and rows of the Content in the scrollview
    public void PrepareCargoGrid() {
        // Also call this, incase user's screen dimensions changed, like a foldable phone
        ResizeContentCell(scrollViewContent);

        // Get the child of player vehicle, which should be a hauler, otherwise the cargo button would not show
        // Then get the HaulerController script of that hauler's game object
        // Then get it's material count
        int[] materialCount = playerVehicle.transform.GetChild(0).gameObject.GetComponent<HaulerController>().materialCount;
    
        int itemsToDisplay = 0;

        for (int i = 0; i != materialCount.Length; i++) {
            if (materialCount[i] > 0) {
                // Create the material button
                GameObject newMaterialButton = Instantiate(materialButtons[i]);
                // Add it to the content scroll view
                newMaterialButton.transform.SetParent(scrollViewContent.transform);

                Transform materialSprite = newMaterialButton.transform.GetChild(0);
    
                // Set it's count
                materialSprite.GetComponent<MaterialManagerUI>().SetCount(materialCount[i]);
                newMaterialButton.transform.localScale = new(1, 1, 1);

                // Set the font sizes
                materialSprite.GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = materialButtonCountFontSize;
                newMaterialButton.transform.GetChild(1).GetComponent<TextMeshProUGUI>().fontSize = materialButtonCountFontSize;

                materialSprite.GetComponent<MaterialManagerUI>().materialIndex = i;

                itemsToDisplay++;

                // Get the Button component
                Button button = newMaterialButton.GetComponent<Button>();
                
                if (button != null) {
                    // Add an OnClick listener to the button
                    int index = i;  // Capture the value of 'i' to avoid closure issues
                    button.onClick.AddListener(() => OnMaterialButtonClick(newMaterialButton));
                }
            }
        }
    
        // Calculate the number of rows
        GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
        int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
        int rows = Mathf.CeilToInt((float)itemsToDisplay / columns);

        // Resize the scroll view content height to fit the rows (400 * # of rows)
        RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, rows * 400);
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
        
        destroyButton.GetComponent<Button>().interactable = false;
    }

    // Used for keeping cells looking nice
    // Pass in the content game object of the scroll view
    public void ResizeContentCell(GameObject contentGameObject) {
        GridLayoutGroup gridLayoutGroup = contentGameObject.GetComponent<GridLayoutGroup>();

        // Get the width of the scroll view (parent container)
        float scrollViewWidth = contentGameObject.GetComponent<RectTransform>().rect.width;

        // Calculate the padding based on percentages of the scroll view width
        float leftPadding = scrollViewWidth * 0.05f;
        float rightPadding = scrollViewWidth * 0.025f;
        float topPadding = scrollViewWidth * 0.05f;
        float bottomPadding = scrollViewWidth * 0.05f;

        // Apply padding to the GridLayoutGroup
        gridLayoutGroup.padding = new RectOffset(
            Mathf.RoundToInt(leftPadding), 
            Mathf.RoundToInt(rightPadding), 
            Mathf.RoundToInt(topPadding), 
            Mathf.RoundToInt(bottomPadding)
        );

        // Set up cell size based on screen resolution (assuming 1080x1920 is the reference resolution)
        float referenceWidth = 1080f;
        //float referenceHeight = 1920f;
        //float screenAspect = (float)Screen.width / Screen.height;

        // Scale the reference cell size based on the current screen resolution while maintaining the 2:3 aspect ratio
        float cellWidth = 200f * (Screen.width / referenceWidth);
        float cellHeight = cellWidth * 1.5f; // Maintain 2:3 aspect ratio

        // Apply the new cell size to the GridLayoutGroup
        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);

        // Calculate the number of columns that can fit in one row
        float availableWidth = scrollViewWidth - (leftPadding + rightPadding);
        int columns = Mathf.FloorToInt(availableWidth / cellWidth);

        // Adjust the spacing to ensure 3-4 items fit in one row
        float spacing = (availableWidth - (columns * cellWidth)) / (columns - 1);
        gridLayoutGroup.spacing = new Vector2(spacing, 30f); // Assuming a fixed vertical spacing of 10

        // Adjust fonts

        //int baseButtonNameFontSize = 27;
        int baseButtonCountFontSize = 35;

        float referenceCellWidth = 200f;
        // Font scaling based on cell width
        float fontScaleFactor = cellWidth / referenceCellWidth;

        // Scale font sizes based on the scale factor
        //materialButtonNameFontSize = Mathf.RoundToInt(baseButtonNameFontSize * fontScaleFactor);
        materialButtonCountFontSize = Mathf.RoundToInt(baseButtonCountFontSize * fontScaleFactor);

    }

    private void OnMaterialButtonClick(GameObject materialSelected)
    {
        destroyButton.GetComponent<DestroyMaterial>().SelectMaterial(materialSelected.transform.GetChild(0).gameObject);
    }

    public int GetFontSize() {
        return materialButtonCountFontSize;
    }
}
