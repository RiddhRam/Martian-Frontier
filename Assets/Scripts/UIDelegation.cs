using System;
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
    // The first elements a user sees, these are the ones they see while playing the game
    // Secondary elements are the menus they open like the shop or map camera
    public GameObject[] primaryElements;
    //private string[] materialNames;
    public GameObject materialButton;
    public Sprite[] materialSprites;
    public String[] materialNames;
    private bool showCargoButton;
 
    void Start()
    {
        ToggleCargoButton(false);
        //materialNames = GameObject.Find("Mine").GetComponent<MineRenderer>().GetMaterialNames();
    }

    public void ToggleCargoButton(bool newValue) {
        showCargoButton = newValue;
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
            showCargoButton = true;
        } else {
            showCargoButton = false;
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
            if (primaryElements[i].name.Contains("Cargo") && !showCargoButton) {
                continue;
            }

            primaryElements[i].SetActive(true);
        }
    }

    // Reveal a single element, typically a secondary element, and only used after HideAll()
    public void RevealElement(GameObject element) {
        element.SetActive(true);
        Camera.main.GetComponent<GameCameraController>().ToggleZooming(false);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
        Camera.main.GetComponent<GameCameraController>().ToggleZooming(true);
    }

    // Used when opening the map, or closing
    public void ToggleCamera() {
        mapCamera.SetActive(!mapCamera.activeSelf);
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
            newMaterialButton.transform.GetChild(3).GetComponent<Image>().sprite = materialSprites[i];
            
            newMaterialButton.transform.localScale = new(1, 1, 1);

            itemsToDisplay++;

            // Get the Button component
            Button button = newMaterialButton.GetComponent<Button>();

            // Add an OnClick listener to the button
            button.onClick.AddListener(() => OnMaterialButtonClick(newMaterialButton));
            
        }
    
        // Calculate the number of rows
        GridLayoutGroup gridLayoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
        int columns = Mathf.Max(1, Mathf.FloorToInt(scrollViewContent.GetComponent<RectTransform>().rect.width / gridLayoutGroup.cellSize.x));
        int rows = Mathf.CeilToInt((float) itemsToDisplay / columns);

        // Resize the scroll view content height to fit the rows (400 * # of rows)
        RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, rows * 600);
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
}
