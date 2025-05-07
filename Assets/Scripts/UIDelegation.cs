using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class UIDelegation : MonoBehaviour
{
    public GameObject mapCamera;
    public GameObject mapCameraView;
    public GameObject teleportCameraView;
    private RenderTexture renderTexture;

    // Higher resolution UI version of the minerals, because they will be larger now in the cargo panel
    // The first elements a user sees, these are the ones they see while playing the game
    // Secondary elements are the menus they open like the shop or map camera
    public GameObject[] primaryElements;
    //private string[] materialNames;
    public GameObject materialButton;
    public GameObject errorMessage;
    private Sprite[] materialHighResSprites;
    private string[] materialNames;
    public int[] materialPrices;

    private GameCameraController mainCameraController;
    public AnalyticsDelegator analyticsDelegator;
    public OreDelegation oreDelegation;
 
    void Start()
    {
        mainCameraController = Camera.main.GetComponent<GameCameraController>();

        if (!oreDelegation) {
            Debug.Log("No ore delegation");
            return;
        }
        materialNames = oreDelegation.materialNames;
        materialHighResSprites = oreDelegation.materialHighResSprites;
        materialPrices = oreDelegation.GetMaterialPrices();
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
            // Reset all buttons back to scale 1. 
            // Need to do this because the button that was pressed down will be at 0.95 still 
            // since it didn't get the pointer up event if it was clicked
            UIButton uiButton = primaryElements[i].GetComponent<UIButton>();
            if (uiButton) {
                StartCoroutine(uiButton.ResetScale());
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
        if (teleportCameraView) {
            teleportCameraView.GetComponent<RawImage>().texture = renderTexture;
        }
    }
    
    public void ShowError(string error, params object[] args) {
        GameObject errorInstance = Instantiate(errorMessage);

        string message = GetLocalizedValue(error, args);
        errorInstance.GetComponent<TextMeshProUGUI>().text = message;

        // Place it within the safe area
        errorInstance.transform.SetParent(transform.GetChild(0), false);
        errorInstance.transform.localPosition = new(0, 400 ,0);

        analyticsDelegator.ShowError(error);
    }

    private string GetLocalizedValue(string key, params object[] args)
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
