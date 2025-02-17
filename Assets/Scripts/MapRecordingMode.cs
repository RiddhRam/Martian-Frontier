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

    public TextMeshProUGUI depthText;
    public TextMeshProUGUI mineText;
    public TextMeshProUGUI valueText;

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
        
        
        transform.position = new(mainCamera.transform.position.x, mainCamera.transform.position.y, transform.position.z);

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
        depthText.text = FormatPositionY((int) -playerVehicle.position.y -5);
        mineText.text = FormatPrice(playerState.GetBlocksMined() - originalBlocksMined);
        valueText.text = "$" + FormatPrice(GetMineValue() - originalMineValue);
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
            boxCollider2D.size = new(boxCollider2D.size.x + 2, boxCollider2D.size.y);
        }
    }
}