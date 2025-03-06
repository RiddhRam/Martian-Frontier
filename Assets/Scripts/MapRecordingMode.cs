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
    public JoystickMovement joystickMovement;
    public PlayerMovement playerMovement;
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
    public float moveSpeed;
    public GameObject arrowPanel;
    public GameObject arrowSeperator;
    public RectTransform arrow;
    public RectTransform currentArrow;
    public RectTransform playerArrow;
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

        originalBlocksMined = playerState.GetBlocksMined();
        originalMineValue = uncollectedMaterialsDelegator.GetMineValue();

        // Hide map icons layer
        thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));

        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;

        if (routeRoulette) {
            thisCamera.orthographicSize = 19;
            cargoValueText.transform.parent.gameObject.SetActive(true);
            arrowPanel.SetActive(true);
            arrowSeperator.SetActive(true);
            currentArrow.transform.parent.gameObject.SetActive(true);

            RectTransform rectTransform = cargoValueText.transform.parent.GetComponent<RectTransform>();

            Vector3 localPos = rectTransform.localPosition;
            rectTransform.localPosition = new(localPos.x, 160, localPos.z);

            rectTransform.offsetMin = new Vector2(-160f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(160f, rectTransform.offsetMax.y);
            panelOutline.effectColor = new(0, 0, 0);
            panelOutline.gameObject.GetComponent<Image>().color = new(0, 0, 0);
        } else {
            Time.timeScale = 0.5f;
            thisCamera.orthographicSize = 21;
            depthProgressSlider.transform.parent.gameObject.SetActive(true);
            panelOutline.effectColor = new(44/255f, 44/255f, 44/255f);
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

    void FixedUpdate()
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
            playerVehicle.eulerAngles = arrow.eulerAngles;

            float angle = (playerVehicle.eulerAngles.z + 90) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            playerVehicle.position += moveSpeed * Time.deltaTime * (Vector3)direction;
            
            transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y - 10, transform.position.z);
            
            float joystickAngle = Mathf.Atan2(joystickMovement.joystickVec.y, joystickMovement.joystickVec.x) * Mathf.Rad2Deg;
            playerArrow.eulerAngles = new(0, 0, joystickAngle - 90);

            timer += Time.deltaTime;
            if (timer >= targetTime)
            {
                timer = 0f;
                targetTime = Random.Range(3f, 4f);
                float currentAngle = arrow.localEulerAngles.z;
                float newAngle;
                bool invalidAngle;

                do
                {
                    newAngle = Random.Range(0f, 361f);
                    // The sprite is offset by 90 degress CCW
                    // Check if we need to block left or right angles
                    bool goingLeft = newAngle > 0f && newAngle < 180f; // Angles pointing left
                    bool goingRight = newAngle < 0f || newAngle > 180f; // Angles pointing right
                    bool goingUp = newAngle < 90f || newAngle > 270f;

                    invalidAngle = Mathf.Abs(Mathf.DeltaAngle(currentAngle, newAngle)) < 90f ||
                                        Mathf.Abs(Mathf.DeltaAngle(currentAngle + 180f, newAngle)) < 40f ||
                                        (transform.position.x < -35f && goingLeft) ||
                                        (transform.position.x > 35f && goingRight) || 
                                        transform.position.y > -55 && goingUp;

                } 
                while (invalidAngle);

                arrow.eulerAngles = new Vector3(0, 0, newAngle);
                currentArrow.eulerAngles = arrow.eulerAngles;
            }

            //float zDifference = Mathf.DeltaAngle(arrow.rotation.eulerAngles.z, playerVehicle.transform.rotation.eulerAngles.z);
            //Debug.Log(zDifference);

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
        valueText.text = "$" + FormatPrice(uncollectedMaterialsDelegator.GetMineValue() - originalMineValue);
        
        if (routeRoulette) {
            cargoValueText.text = valueText.text;
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
        originalMineValue = uncollectedMaterialsDelegator.GetMineValue();

        Transform vehicle = playerVehicle.transform.GetChild(0);
        BoxCollider2D boxCollider2D = vehicle.GetChild(1).GetComponent<BoxCollider2D>();
        if (boxCollider2D) {
            boxCollider2D.size = new(boxCollider2D.size.x + 2, boxCollider2D.size.y);
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