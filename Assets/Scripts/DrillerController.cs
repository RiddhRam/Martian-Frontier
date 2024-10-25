using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    // Not actually a radius, it's a 1x1 square
    private readonly int radius = 1;

    void OnTriggerEnter2D(Collider2D collision)
    {
        Tilemap tilemap = collision.GetComponent<Tilemap>();

        if (!tilemap) {
            return;
        }
        
        Vector3 spriteWorldPos = transform.position;
        Vector3Int spriteTilePos = tilemap.WorldToCell(spriteWorldPos);

        float closestDistance = 10;
        Vector3Int nearestTilePos = Vector3Int.zero;

        // Iterate over nearby tiles within the radius
        // Not actually a radius, it's a 1x1 square
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentTilePos = spriteTilePos + new Vector3Int(x, y, 0);

                // Check if the tile exists
                if (tilemap.HasTile(currentTilePos))
                {
                    Vector3 tileWorldPos = tilemap.GetCellCenterWorld(currentTilePos);
                    float distance = Vector3.Distance(spriteWorldPos, tileWorldPos);

                    // Keep track of the closest tile
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        nearestTilePos = currentTilePos;
                    }
                }
            }
        }

        if (closestDistance < float.MaxValue)
        {
            tilemap.SetTile(nearestTilePos, null);

            // Enable and renable quickly so the trigger event can occur again
            tilemap.GetComponent<TilemapCollider2D>().enabled = false;
            tilemap.GetComponent<TilemapCollider2D>().enabled = true;
        }
    }
}
