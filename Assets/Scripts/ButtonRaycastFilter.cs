using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ButtonRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private Image image;
    private Sprite sprite;
    private Texture2D texture;

    void Awake()
    {
        image = GetComponent<Image>();
        sprite = image.sprite;
        texture = sprite.texture;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (sprite == null || texture == null) return false;

        RectTransform rectTransform = image.rectTransform;

        // Convert screen point to local point in the RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint);

        // Convert local point to normalized sprite coordinates (0 to 1)
        Rect rect = rectTransform.rect;
        float x = (localPoint.x - rect.x) / rect.width;
        float y = (localPoint.y - rect.y) / rect.height;

        // Convert to texture coordinates
        int texX = Mathf.FloorToInt(x * sprite.rect.width + sprite.rect.x);
        int texY = Mathf.FloorToInt(y * sprite.rect.height + sprite.rect.y);

        if (texX < 0 || texY < 0 || texX >= texture.width || texY >= texture.height)
            return false;

        Color pixel = texture.GetPixel(texX, texY);
        return pixel.a > 0f;
    }
}
