using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    public GameObject Iron;
    public GameObject Sulfur;
    public GameObject Limestone;

    // Not actually a radius, it's a square
    private int radius;
    private TileBase[] ores;
    private GameObject[] materials;
    private MineRenderer mineRenderer;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private int drillTier;
    // Does nothing, just for showing the user in the Garage
    public int width;
    [SerializeField]
    private int price;

    void Start() {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        ores = mineRenderer.GetOres();
        materials = new GameObject[] { Iron, Sulfur, Limestone };
        radius = Mathf.RoundToInt(GetComponent<BoxCollider2D>().size.x);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Mine Tag")) {
            return;
        }

        Tilemap tilemap = collision.GetComponent<Tilemap>();

        Vector3 spriteWorldPos = transform.position;
        Vector3Int spriteTilePos = tilemap.WorldToCell(spriteWorldPos);

        float closestDistance = 5;
        Vector3Int nearestTilePos = Vector3Int.zero;
        Vector3 centerTilePos = Vector3.zero;

        // Iterate over nearby tiles within the radius
        // Not actually a radius, it's a square
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentTilePos = spriteTilePos + new Vector3Int(x, y, 0);

                // Check if the tile exists
                if (!tilemap.HasTile(currentTilePos)) {
                    continue;
                }

                Vector3 tileWorldPos = tilemap.GetCellCenterWorld(currentTilePos);
                float distance = Vector3.Distance(spriteWorldPos, tileWorldPos);

                if (distance >= closestDistance) {
                    continue;
                }

                // Keep track of the closest tile
                closestDistance = distance;
                nearestTilePos = currentTilePos;
                centerTilePos = tileWorldPos; 
            }
        }

        if (closestDistance >= float.MaxValue) {
            return;
        }

        TileBase tileToDestroy = tilemap.GetTile(nearestTilePos);

        for (int i = 0; i != ores.Length; i++) {
            if (tileToDestroy != ores[i]) {
                continue;
            }

            GameObject materialToUse = materials[i];

            // If no neighbouring materials then this stays 0 and the new object will have a count of 1
            int oldCount = 0;

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                { 

                    Collider2D[] hitColliders = Physics2D.OverlapCircleAll(new Vector2(x + centerTilePos.x, y + centerTilePos.y), 0.1f);
                    
                    foreach (var hitCollider in hitColliders)
                    {       
                        // Make sure a gameobject was hit
                        if (hitCollider == null) {
                            continue;
                        }
                        
                        // Make sure they are the same materials
                        if (hitCollider.gameObject.name != materialToUse.name + "(Clone)") {
                            continue;
                        }
                
                        // If a neighbouring material was found, delete it,
                        // and keep track of that value deleted
                        // Don't set oldCount, use += in case there are more than 1;
                        // Also don't break for the same reason
                        Destroy(hitCollider.gameObject);
                        oldCount += hitCollider.gameObject.GetComponent<MaterialManager>().count;
                        break;
                    }
                }
            }

            GameObject material = Instantiate(materialToUse);
            material.transform.position = centerTilePos;
            material.GetComponent<MaterialManager>().SetCount(oldCount + 1);
            material.transform.SetParent(GameObject.Find("Mine").transform);
            break;
        }
        
        // Destroy the tile and reveal new tiles in the vision radius
        mineRenderer.DestroyTile(nearestTilePos);

        // Disable and renable quickly so the trigger event can occur again
        tilemap.GetComponent<TilemapCollider2D>().enabled = false;
        tilemap.GetComponent<TilemapCollider2D>().enabled = true;
    }

    public float GetPlayerSpeed() {
        return playerSpeed;
    }

    public int GetDrillTier() {
        return drillTier;
    }

    public int GetPrice() {
        return price;
    }
}
