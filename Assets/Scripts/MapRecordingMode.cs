using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapRecordingMode : MonoBehaviour
{
    public Transform playerVehicle;
    public GameObject mapText;
    public GameObject videoInfo;
    public PlayerState playerState;
    public UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    public OreDelegation oreDelegation;
    public RawImage mapCameraView;
    public Outline panelOutline;

    public TextMeshProUGUI cargoValueText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI mineText;
    public TextMeshProUGUI valueText;

    public Slider depthProgressSlider;
    public TextMeshProUGUI depthProgressSliderText;
    public GameObject depthIconGO;
    public int minY;
    public int maxY;
    public int currentTier;

    public bool routeRoulette;
    public GameObject arrowPanel;
    public RectTransform arrow;
    private float timer;
    private float targetTime;

    [SerializeField]
    int minimumCameraSize;
    [SerializeField]
    int maximumCameraSize;
    [SerializeField]
    int visionRadius;
    [SerializeField]
    float farthestRight;
    [SerializeField]
    float farthestLeft;
    [SerializeField]
    float farthestTop;
    [SerializeField]
    float farthestDown;
    System.Numerics.BigInteger originalBlocksMined;
    System.Numerics.BigInteger originalMineValue;

    Camera thisCamera;
    Camera mainCamera;

    HaulerController haulerController;

    void Start()
    {
        mapText.SetActive(false);
        //videoInfo.SetActive(true);
        thisCamera = GetComponent<Camera>();
        mainCamera = Camera.main;
        panelOutline.effectColor = new(44/255f, 44/255f, 44/255f);

        originalBlocksMined = playerState.GetBlocksMined();
        originalMineValue = GetMineValue();

        // Hide map icons layer
        thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));
        thisCamera.orthographicSize = 22;

        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;

        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        mineRenderer.minVeinRadius = 2;
        mineRenderer.maxVeinRadius = 3;
        mineRenderer.minVeinCount = 4;
        mineRenderer.maxVeinCount = 5;

        if (routeRoulette) {
            cargoValueText.transform.parent.gameObject.SetActive(true);
            arrowPanel.SetActive(true);
            RectTransform rectTransform = cargoValueText.transform.parent.GetComponent<RectTransform>();

            Vector3 localPos = rectTransform.localPosition;
            rectTransform.localPosition = new(localPos.x, 150, localPos.z);

            rectTransform.offsetMin = new Vector2(-160f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(160f, rectTransform.offsetMax.y);
        } else {
            depthProgressSlider.transform.parent.gameObject.SetActive(true);
            Time.timeScale = 0.5f;
        }
    }

    public void SetSliderBoundaries() {
        
        if (playerVehicle.position.y > -155)  {
            minY = -155;
            maxY = -6;
            currentTier = 1;
        } else if (playerVehicle.position.y > -325) {
            minY = -325;
            maxY = -165;
            currentTier = 2;
        } else if (playerVehicle.position.y > -325) {
            minY = -505;
            maxY = -335;
            currentTier = 3;
        }

    }

    void Update()
    {
        Vector3 pos = playerVehicle.position;

        if (pos.x > farthestRight)
            farthestRight = pos.x;

        if (pos.x < farthestLeft)
            farthestLeft = pos.x;

        if (pos.y > farthestTop)
            farthestTop = pos.y;

        if (pos.y < farthestDown)
            farthestDown = pos.y;
        
        //Zoom();
        //ClampCamera();
        
        if (routeRoulette) {
            transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y - 10, transform.position.z);

            timer += Time.deltaTime;
            if (timer >= targetTime)
            {
                timer = 0f;
                targetTime = Random.Range(3f, 5f);
                float currentAngle = arrow.localEulerAngles.z;
                float newAngle;

                do
                {
                    newAngle = Random.Range(0f, 361f);
                } 
                while (Mathf.Abs(Mathf.DeltaAngle(currentAngle, newAngle)) < 10f || 
                    Mathf.Abs(Mathf.DeltaAngle(currentAngle + 180f, newAngle)) < 10f);

                arrow.localEulerAngles = new Vector3(0, 0, newAngle);
            }

            float zDifference = Mathf.DeltaAngle(arrow.rotation.eulerAngles.z, playerVehicle.transform.rotation.eulerAngles.z);
            Debug.Log(zDifference);

        } else {
            transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y, transform.position.z);            
        }


        UpdateText();
    }

    private void ClampCamera()
    {
        
        if (thisCamera.orthographicSize >= maximumCameraSize - 0.5) {
            transform.position = Vector3.Lerp(transform.position, new(playerVehicle.position.x, playerVehicle.position.y, transform.position.z), Time.deltaTime * 5f);
        } else {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp((farthestLeft + farthestRight) / 2, farthestLeft - visionRadius, farthestRight + visionRadius);
            clampedPosition.y = Mathf.Clamp((farthestTop + farthestDown) / 2, -600, -thisCamera.orthographicSize - 4.5f);
            transform.position = clampedPosition;
        }
        
    }

    private void Zoom()
    {
        float width = farthestRight - farthestLeft + (visionRadius * 3);
        float height = (farthestTop - farthestDown + (visionRadius * 8))/2;
        float targetSize = Mathf.Max(width, height);
        targetSize = Mathf.Clamp(targetSize, minimumCameraSize, maximumCameraSize);
        thisCamera.orthographicSize = Mathf.Lerp(thisCamera.orthographicSize, targetSize, Time.deltaTime * 5);
    }

    public void UpdateText() {
        depthText.text = FormatPositionY((int) -playerVehicle.position.y - 5);
        mineText.text = FormatPrice(playerState.GetBlocksMined() - originalBlocksMined);
        valueText.text = "$" + FormatPrice(GetMineValue() - originalMineValue);
        
        if (routeRoulette) {
            cargoValueText.text = "$" + FormatPrice(haulerController.GetTotalCargoValue());
        } else {
            int previousTier = currentTier;
            SetSliderBoundaries();

            if (!haulerController) {

                if (currentTier != previousTier) {
                    // Flipped cuz we need positive values
                    depthProgressSlider.minValue = -maxY;
                    depthProgressSlider.maxValue = -minY;
                }

                depthProgressSlider.value = -playerVehicle.position.y;
                depthProgressSliderText.text = depthText.text;

            } else {
                depthIconGO.SetActive(false);
                depthProgressSlider.maxValue = 8000 * Mathf.Pow(10, currentTier);

                depthProgressSlider.value = (float) haulerController.GetTotalCargoValue();
                
                depthProgressSliderText.text = "$" + FormatPrice(haulerController.GetTotalCargoValue());
            }

            cargoValueText.text = valueText.text;
        }
        
    }

    public System.Numerics.BigInteger GetMineValue() {
        System.Numerics.BigInteger mineValue = 0;

        foreach (var kvp in uncollectedMaterialsDelegator.uncollectedMaterials) {
            mineValue += kvp.Value.count * oreDelegation.GetMaterialPrices()[kvp.Value.materialIndex];
        }

        return mineValue;
    }

    private string FormatPositionY(int positionY)
    {
        if (positionY <= 0) {
            return "0 M";
        }
        
        if (positionY >= 1_000)
        {
            // Truncate to 3 decimal places and format with "KM"
            return (positionY / 1_000) + " KM";
        } else {
            return positionY + " M";
        }
    }

    private string FormatPrice(System.Numerics.BigInteger price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.##") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("0.##") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("0.##") + "B";
        }
        else if (price >= 1_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("0.##") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("0.##") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }


    [ContextMenu("Reset Camera")]
    public void ResetCamera() {
        
        transform.position = new(0, -256, -17);
        thisCamera.orthographicSize = 22;
        
        Vector3 pos = playerVehicle.position;

        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;
        
        // Create a new RenderTexture
        RenderTexture renderTexture = new RenderTexture(1749, 2725, 24, RenderTextureFormat.ARGB32); // 24 is the depth buffer bit size
        renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.S8_UInt;
        renderTexture.Create();

        // Assign the RenderTexture to the mapCamera's target texture
        GetComponent<Camera>().targetTexture = renderTexture;
        mapCameraView.texture = renderTexture;
        originalBlocksMined = playerState.GetBlocksMined();
        originalMineValue = GetMineValue();

        Transform vehicle = playerVehicle.transform.GetChild(0);
        BoxCollider2D boxCollider2D = vehicle.GetChild(1).GetComponent<BoxCollider2D>();
        if (boxCollider2D) {
            boxCollider2D.size = new(boxCollider2D.size.x + 5, boxCollider2D.size.y);
            haulerController = null;
        } else {
            haulerController = vehicle.GetComponent<HaulerController>();
        }

        /*
        string fullPath = Path.Combine(Application.persistentDataPath, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        string tempPath = fullPath + ".csv";

        try {
            // Create directory to save file in if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = string.Join("\n", mineValues.Select((value, index) => $"{index},{value}"));

            // Write to a temporary file first
            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 2097152, useAsync: true))
            using (StreamWriter writer = new StreamWriter(stream)) {
                writer.WriteAsync(dataToStore);
            }

            // Replace the original file with the temporary file
            // If the original file exists, replace it. Otherwise, move the temp file.
            if (File.Exists(fullPath)) {
                File.Replace(tempPath, fullPath, null);
            } else {
                File.Move(tempPath, fullPath);
            }
        } 
        catch (System.Exception ex) {
            Debug.Log($"Error when trying to save data to file: {ex.Message}");
        }
        */
    }
}