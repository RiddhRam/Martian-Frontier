using TMPro;
using UnityEngine;

public class MapRecordingMode : MonoBehaviour
{
    public Transform playerVehicle;
    public GameObject mapText;
    public GameObject videoInfo;
    public PlayerState playerState;
    public UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    public OreDelegation oreDelegation;

    public TextMeshProUGUI depthText;
    public TextMeshProUGUI mineText;
    public TextMeshProUGUI valueText;

    [SerializeField]
    int minimumCameraSize;
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
    [SerializeField]
    System.Numerics.BigInteger originalBlocksMined;

    Camera thisCamera;

    void Start()
    {
        mapText.SetActive(false);
        videoInfo.SetActive(true);
        thisCamera = GetComponent<Camera>();

        originalBlocksMined = playerState.GetBlocksMined();

        // Hide map icons layer
        thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));
        
        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;
    }

    [ContextMenu("Reset Camera")]
    public void ResetCamera() {
        transform.position = new(0, -256, -17);
        thisCamera.orthographicSize = 252;
        
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

        ClampCamera();
        Zoom();
        UpdateText();
    }

    private void ClampCamera()
    {
        Vector3 clampedPosition = thisCamera.transform.position;
        clampedPosition.x = Mathf.Clamp((farthestLeft + farthestRight) / 2, farthestLeft - visionRadius, farthestRight + visionRadius);
        clampedPosition.y = Mathf.Clamp(-thisCamera.orthographicSize + farthestTop + (visionRadius * 2), -600, -thisCamera.orthographicSize - 4.5f);
        thisCamera.transform.position = clampedPosition;
    }

    private void Zoom()
    {
        float width = farthestRight - farthestLeft + (visionRadius * 4);
        float height = (farthestTop - farthestDown + (visionRadius * 4)) / 2;
        float targetSize = Mathf.Max(width, height);
        targetSize = Mathf.Clamp(targetSize, minimumCameraSize, 252);
        thisCamera.orthographicSize = Mathf.Lerp(thisCamera.orthographicSize, targetSize, Time.deltaTime * 5);
    }

    public void UpdateText() {
        depthText.text = FormatPositionY((int) -playerVehicle.position.y -5);
        mineText.text = FormatPrice(playerState.GetBlocksMined() - originalBlocksMined);
        valueText.text = "$" + FormatPrice(GetMineValue());
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
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.#") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.#") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("0.#") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("0.#") + "B";
        }
        else if (price >= 1_000_000)
        {
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("0.#") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("0.#") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }
}
