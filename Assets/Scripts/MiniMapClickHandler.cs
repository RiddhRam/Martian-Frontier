using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMapClickHandler : MonoBehaviour
{
    [SerializeField] private RectTransform teleportDisplay;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private UpgradesDelegator upgradesDelegator;
    [SerializeField] private RectTransform uIRectTransform;
    [SerializeField] private RectTransform imageRectTransform;

    private int errorCounter = 0;

    private Vector3 currentPosition;
    private PointerEventData pointerEventData;

    public void PointerDown() {
        errorCounter = 60;
        UpdateTeleportDisplayPosition(Input.mousePosition);
    }

    public void Drag(BaseEventData baseEventData) {
        pointerEventData = baseEventData as PointerEventData;
        UpdateTeleportDisplayPosition(pointerEventData.position);
    }

    public void UpdateTeleportDisplayPosition(Vector2 newPointerPosition) {

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uIRectTransform,   // The Canvas RectTransform
            newPointerPosition, 
            Camera.main,   // The Camera
            out localPoint
        );

        Vector2 normalizedPoint = new Vector2(
            (localPoint.x + imageRectTransform.rect.width * 0.5f) / imageRectTransform.rect.width,
            (localPoint.y + imageRectTransform.rect.height * 0.5f) / imageRectTransform.rect.height
        );

        Ray ray = mapCamera.ViewportPointToRay(new Vector3(normalizedPoint.x, normalizedPoint.y, 10));

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
            errorCounter++;

            if (errorCounter > 60) {
                upgradesDelegator.InvalidTeleportLocation();
                errorCounter = 0;
            }
            return;
        }

        teleportDisplay.gameObject.SetActive(true);
        teleportDisplay.anchoredPosition = new(localPoint.x, localPoint.y + 100);
        currentPosition = worldPosition;
    }

    // Called from confirm button
    public void Teleport() {
        upgradesDelegator.Teleport(currentPosition);
        teleportDisplay.gameObject.SetActive(false);
    }
}