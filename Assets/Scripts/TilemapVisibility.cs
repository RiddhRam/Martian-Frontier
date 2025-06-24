using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapRenderer))]
public class TilemapVisibility : MonoBehaviour
{
    [SerializeField] private TilemapRenderer tilemapRenderer;

    void OnBecameVisible()
    {
        // The tilemap is now in view, so we make sure its renderer is enabled.
        tilemapRenderer.enabled = true;
    }

    void OnBecameInvisible()
    {
        // The tilemap is out of view, so we disable its renderer to save performance.
        tilemapRenderer.enabled = false;
    }
}