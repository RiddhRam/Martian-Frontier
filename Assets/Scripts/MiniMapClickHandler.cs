using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MiniMapClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Camera mapCamera;
    [SerializeField] private UpgradesDelegator upgradesDelegator;
    RawImage displayImage;

    void Start()
    {
        displayImage = GetComponent<RawImage>();
    }

    // This function is called when the RawImage is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // Get the local position within the RawImage UI element
        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        
        // Convert local point to normalized coordinates (0-1 range)
        Vector2 normalizedPoint = new Vector2(
            (localPoint.x + rectTransform.rect.width * 0.5f) / rectTransform.rect.width,
            (localPoint.y + rectTransform.rect.height * 0.5f) / rectTransform.rect.height
        );

        // Convert normalized point to viewport coordinates for the secondary camera
        // Note: RawImage might have different aspect ratio than the camera,
        // so we may need to account for that depending on your setup
        Ray ray = mapCamera.ViewportPointToRay(new Vector3(normalizedPoint.x, normalizedPoint.y, 10));
        // If the ray doesn't hit anything, we can use a fixed distance from the camera
        Vector3 worldPosition = ray.GetPoint(10f); // 10 units away from camera

        Collider2D[] colliders = Physics2D.OverlapBoxAll(worldPosition, new(2, 2), 0);

        bool validSpace = true;

        foreach (var collider in colliders) {
            if (collider.name.Contains("Large Fog Of War") || collider.name.Contains("Soil") || collider.name.Contains("Generate") || collider.CompareTag("Mine Tag")) {
                validSpace = false;
                break;
            }
        }

        if (colliders.Length == 0) {
            validSpace = false;
        }

        if (!validSpace) {
            upgradesDelegator.InvalidTeleportLocation();
            return;
        }
    
        upgradesDelegator.Teleport(worldPosition);
    }
}