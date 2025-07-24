using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class UIDelegation : MonoBehaviour
{
    private static UIDelegation _instance;
    public static UIDelegation Instance 
    {
        get  
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<UIDelegation>();
            }
            return _instance;
        }
    }

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
    public GameObject backgroundDarkness;

    public float fadeDuration = 0.25f;

    public OreDelegation oreDelegation;

    void Start()
    {

        if (!oreDelegation) {
            Debug.Log("No ore delegation");
            return;
        }
    }

    public void CloseActiveElement()
    {
        Transform safeArea = transform.GetChild(2);

        for (int i = 0; i != safeArea.childCount; i++)
        {
            if (safeArea.GetChild(i).gameObject.activeSelf)
            {
                GameObject activeGameobject = safeArea.GetChild(i).gameObject;

                StartCoroutine(FadeAndScaleOut(activeGameobject.GetComponent<CanvasGroup>(), activeGameobject.GetComponent<RectTransform>()));
            }
        }

        StartCoroutine(FadeAndScaleOut(backgroundDarkness.GetComponent<CanvasGroup>()));
        
        RevealAll();
    }

    // Hide all base elements, and only used before opening a secondary element like the camera
    public void HideAll()
    {
        for (int i = 0; i < primaryElements.Length; i++)
        {
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
    public void RevealElement(GameObject element)
    {
        GameCameraController.Instance.ToggleMovement(false);
        AnalyticsDelegator.Instance.OpenUIPanel(element.name);

        if (!element.name.Contains("Refinery"))
        {
            ToggleBackgroundDarkness(true);
        }

        StartCoroutine(FadeAndScaleIn(element.GetComponent<CanvasGroup>(), element.GetComponent<RectTransform>()));
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element)
    {
        GameCameraController.Instance.ToggleMovement(true);
        ToggleBackgroundDarkness(false);

        StartCoroutine(FadeAndScaleOut(element.GetComponent<CanvasGroup>(), element.GetComponent<RectTransform>()));
    }

    public IEnumerator FadeAndScaleIn(CanvasGroup canvasGroup, RectTransform rt = null)
    {
        Vector3 fullOpenScale = GetFullOpenScale(canvasGroup.gameObject.name);

        Outline outline = canvasGroup.GetComponent<Outline>();
        if (outline)
        {
            outline.enabled = false;
        }

        float elapsed = 0f;

        canvasGroup.alpha = 0f;
        if (rt)
        {
            rt.localScale = new(0.5f, 0.5f, 0.5f);
        }

        canvasGroup.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Scale
            if (rt)
            {
                rt.localScale = fullOpenScale * Mathf.Lerp(0.5f, 1f, t);
            }

            // Alpha
            canvasGroup.alpha = t;
            yield return null;
        }

        // Final values
        if (rt)
        {
            rt.localScale = fullOpenScale;
        }
        canvasGroup.alpha = 1f;
        
        if (outline)
        {
            outline.enabled = true;
        }
    }

    private IEnumerator FadeAndScaleOut(CanvasGroup canvasGroup, RectTransform rt = null)
    {
        Vector3 fullOpenScale = GetFullOpenScale(canvasGroup.gameObject.name);

        Outline outline = canvasGroup.GetComponent<Outline>();
        if (outline)
        {
            outline.enabled = false;
        }

        float elapsed = 0f;

        canvasGroup.alpha = 1f;

        if (rt)
        {
            rt.localScale = fullOpenScale;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1 - Mathf.Clamp01(elapsed / fadeDuration);

            // Scale
            if (rt)
            {
                rt.localScale = fullOpenScale * Mathf.Lerp(0.5f, 1f, t);
            }

            // Alpha
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.gameObject.SetActive(false);


        // Final values
        if (rt)
        {
            rt.localScale = fullOpenScale;
        }
        canvasGroup.alpha = 0f;
    }

    // Get the scale of that a panel should be after opening
    public Vector3 GetFullOpenScale(string element)
    {
        Vector3 fullOpenScale = Vector3.one;

        if (element == "Refinery Upgrade Panel")
        {
            float scaleToUse = RefineryController.Instance.GetAspectValue();
            fullOpenScale = new(scaleToUse, scaleToUse, scaleToUse);
        }
        else if (element == "Refinery Alternate Panel")
        {
            float scaleToUse = RefineryController.Instance.GetAspectValue(true);
            fullOpenScale = new(scaleToUse, scaleToUse, scaleToUse);
        }
        else if (element == "Drone Upgrades Panel")
        {
            fullOpenScale = new(0.69f, 0.69f, 0.69f);
        }
        else if (element == "Go To Team Panel" || element == "Target Depth Panel")
        {
            fullOpenScale = new(0.69f, 0.6243843f, 1);
        }
        else if (element == "Settings Panel")
        {
            fullOpenScale = new(0.85f, 0.7f, 1);
        }

        return fullOpenScale;
    }

    public void ToggleBackgroundDarkness(bool newState)
    {
        // If meant to enable it, then fade it in
        if (newState)
        {
            StartCoroutine(FadeAndScaleIn(backgroundDarkness.GetComponent<CanvasGroup>()));
        }
        else
        {
            StartCoroutine(FadeAndScaleOut(backgroundDarkness.GetComponent<CanvasGroup>()));
        }
    }

    // Used when opening the map, or closing
    // MAP CAMERA, NOT MAIN CAMERA
    public void ToggleCamera()
    {
        mapCamera.SetActive(!mapCamera.activeSelf);

        // Make sure its active
        if (!mapCamera.activeSelf)
        {
            return;
        }

        float aspectRatio = (float)Screen.height / Screen.width;

        if (aspectRatio >= 1.7)
        {
            aspectRatio /= 1.12f;
        }
        else
        {
            aspectRatio /= 1.15f;
        }

        // Create a new RenderTexture
        renderTexture = new RenderTexture((int)(Screen.height / aspectRatio), Screen.height, 24, RenderTextureFormat.ARGB32); // 24 is the depth buffer bit size
        renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.S8_UInt;
        renderTexture.Create();

        // Assign the RenderTexture to the mapCamera's target texture
        mapCamera.GetComponent<Camera>().targetTexture = renderTexture;
        mapCameraView.GetComponent<RawImage>().texture = renderTexture;
        if (teleportCameraView)
        {
            teleportCameraView.GetComponent<RawImage>().texture = renderTexture;
        }
    }
    
    public void ShowError(string error, params object[] args) {
        GameObject errorInstance = Instantiate(errorMessage);

        string message = GetLocalizedValue(error, args);
        errorInstance.GetComponent<TextMeshProUGUI>().text = message;

        // Place it within the UI
        errorInstance.transform.SetParent(transform, false);
        errorInstance.transform.localPosition = new(0, 400 ,0);

        AnalyticsDelegator.Instance.ShowError(error);
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
