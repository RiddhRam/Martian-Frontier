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
 
    void Start()
    {
        UpdatePrimaryElements();
        //materialNames = GameObject.Find("Mine").GetComponent<MineRenderer>().GetMaterialNames();
        materialButtons = new GameObject[] { LimestoneMaterialButton, SulfurMaterialButton, IronMaterialButton };
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
        // Get the child of player vehicle, which should be a hauler, otherwise the cargo button would not show
        // Then get the HaulerController script of that hauler's game object
        // Then get it's material count
        int[] materialCount = playerVehicle.transform.GetChild(0).gameObject.GetComponent<HaulerController>().GetMaterialCount();
    
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

                materialSprite.GetComponent<MaterialManagerUI>().materialIndex = i;

                itemsToDisplay++;

                // Get the Button component
                Button button = newMaterialButton.GetComponent<Button>();
                

                // Add an OnClick listener to the button
                button.onClick.AddListener(() => OnMaterialButtonClick(newMaterialButton));
            }
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
        
        destroyButton.GetComponent<Button>().interactable = false;
    }

    private void OnMaterialButtonClick(GameObject materialSelected)
    {
        destroyButton.GetComponent<DestroyMaterial>().SelectMaterial(materialSelected.transform.GetChild(0).gameObject);
    }
}
