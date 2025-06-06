using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapRecordingMode : MonoBehaviour
{
    public Transform playerVehicle;
    public GameObject mapText;
    public PlayerState playerState;

    public TextMeshProUGUI cargoValueText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI mineText;
    public TextMeshProUGUI valueText;

    public TextMeshProUGUI mineValueText;
    public TextMeshProUGUI cashEarnedValueText;

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

    System.Numerics.BigInteger originalMineValue;
    System.Numerics.BigInteger originalCashEarned;

    Camera thisCamera;
    Camera mainCamera;

    // Mine values: 
    // Min and max vein radius: 2
    // Min and max vein count: 5

    public Transform frontWheels;
    private Coroutine glowCoroutine;

    private bool notSingleplayerScene = true;


    // #4ca0d7, #ffffff
    // #ffa500, #000000

    void OnEnable() {
        mapText.SetActive(false);
        //videoInfo.SetActive(true);
        thisCamera = GetComponent<Camera>();
        mainCamera = Camera.main;

        // Hide map icons layer
        if (!SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSingleplayerScene = false;

            thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));
            thisCamera.cullingMask |= 1 << LayerMask.NameToLayer("Vehicle");
            
        } else {
            mineValueText.transform.parent.parent.gameObject.SetActive(true);
            cashEarnedValueText.transform.parent.parent.gameObject.SetActive(true);
        }

        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;

        ResetCamera();
    }

    void FixedUpdate() {
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
        
        if (!notSingleplayerScene) {
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

        if (!notSingleplayerScene) {
            return;
        }

        cashEarnedValueText.text = playerState.FormatPrice(playerState.GetMoneyEarned() - originalCashEarned);
    }

    private IEnumerator GlowText() {
        Color startColor = new Color(57f / 255f, 255f/ 255f, 20f / 255f);
        Color endColor = Color.white;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mineValueText.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        // Ensure the final color is exactly the target color
        mineValueText.color = endColor;
    }

    [ContextMenu("Reset Camera")]
    public void ResetCamera() {

        if (!isActiveAndEnabled) {
            return;
        }
        
        transform.position = new(0, -91, -17);
        //transform.position = new(0, -165, -17);
        //transform.position = new(0, -256, -17);

        Vector3 pos = playerVehicle.position;

        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestTop = pos.y;
        farthestDown = pos.y;

        Transform vehicle = playerVehicle.transform.GetChild(0);
        BoxCollider2D boxCollider2D = vehicle.GetChild(1).GetComponent<BoxCollider2D>();
        if (boxCollider2D) {
            boxCollider2D.size = new(boxCollider2D.size.x + 2, boxCollider2D.size.y);
            frontWheels = null;
        } 
        else {
            // SetSpeed is called when a new vehicle is placed
            // When a new vehicle is placed we should also check if it needs animated wheels or not
            for (int i = 0; i != vehicle.childCount; i++) {
                if (vehicle.GetChild(i).name == "Front Wheels") {
                    frontWheels = vehicle.GetChild(i);
                    break;
                }
            }

            for (int i = 0; i != frontWheels.childCount; i++) {
                frontWheels.GetChild(i).GetComponent<PolygonCollider2D>().enabled = false;
            }
        }

        if (!notSingleplayerScene) {
            mineValueText.transform.parent.parent.parent.gameObject.SetActive(false);
            Camera.main.orthographicSize = 26f;
        }
        //Camera.main.orthographicSize = 30;

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