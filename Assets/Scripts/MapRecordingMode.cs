using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapRecordingMode : MonoBehaviour
{
    public Transform playerVehicle;
    public GameObject mapText;
    public GameObject depth;
    public TextMeshProUGUI depthText;

    [SerializeField]
    int minimumCameraSize;
    [SerializeField]
    int visionRadius;
    readonly float top = -4;
    [SerializeField]
    float farthestRight;
    [SerializeField]
    float farthestLeft;
    [SerializeField]
    float farthestDown;

    private Camera thisCamera;

    void Start()
    {
        mapText.SetActive(false);
        depth.SetActive(true);
        thisCamera = GetComponent<Camera>();

        // Hide map icons layer
        thisCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Map Icons"));
        
        Vector3 pos = playerVehicle.position;
        farthestRight = pos.x;
        farthestLeft = pos.x;
        farthestDown = pos.y;
    }

    void Update()
    {
        Vector3 pos = playerVehicle.position;

        if (pos.x > farthestRight)
            farthestRight = pos.x;

        if (pos.x < farthestLeft)
            farthestLeft = pos.x;

        if (pos.y < farthestDown)
            farthestDown = pos.y;

        ClampCamera();
        Zoom();
        UpdateDepth();
    }

    private void ClampCamera()
    {
        Vector3 clampedPosition = thisCamera.transform.position;
        clampedPosition.x = Mathf.Clamp((farthestLeft + farthestRight) / 2, farthestLeft - visionRadius, farthestRight + visionRadius);
        clampedPosition.y = -thisCamera.orthographicSize + top;
        thisCamera.transform.position = clampedPosition;
    }

    private void Zoom()
    {
        float width = farthestRight - farthestLeft + (visionRadius * 2);
        float height = top - farthestDown + (visionRadius * 2);
        float targetSize = Mathf.Max(width, height);
        targetSize = Mathf.Clamp(targetSize, minimumCameraSize, 252);
        thisCamera.orthographicSize = Mathf.Lerp(thisCamera.orthographicSize, targetSize, Time.deltaTime * 5);
    }

    public void UpdateDepth() {
        depthText.text = FormatPositionY((int) -playerVehicle.position.y -5);
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
}
