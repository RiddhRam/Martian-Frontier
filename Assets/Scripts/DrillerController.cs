using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    public Tilemap tilemap;

    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 spriteWorldPos = transform.position;
        Vector3Int spriteTilePos = tilemap.WorldToCell(spriteWorldPos);

        float closestDistance = float.MaxValue;
        Vector3Int nearestTilePos = Vector3Int.zero;

        // Iterate over nearby tiles within the radius
        int radius = Mathf.CeilToInt(1f);
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
            Vector3 nearestTileWorldPos = tilemap.GetCellCenterWorld(nearestTilePos);
            tilemap.SetTile(nearestTilePos, null);
        }
    }
}
