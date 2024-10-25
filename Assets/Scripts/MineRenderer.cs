using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour
{
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public RuleTile level1Rock;  // Tile to place on the grid
    public TileBase IronOre;
    public TileBase SulfurOre;
    public TileBase LimestoneRock;

    private TileBase[] level1Tiles;
    private readonly Vector2Int gridSize = new(25, 25); // 25x25 grid

    // Start is called before the first frame update
    void Start()
    {
        level1Tiles = new TileBase[] { level1Rock, IronOre, SulfurOre, LimestoneRock };
    }

    // Places tiles in a 25x25 square, starting from (-50, -5) and going to the right and downward
    public void CreateTiles(int chunkRow)
    {
        GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
        mineTilemapGameObject.transform.SetParent(transform);
        mineTilemapGameObject.name = "Row " + chunkRow;

        Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();

        // Find the level of the rock
        int level = 3;
        if (chunkRow <= 6) {
            level = 1;
        } else if (chunkRow <= 12) {
            level = 2;
        }

        for (int i = -50; i != 50; i += 25) {
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * 25 - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(i + x, (chunkRow - 1) * -25 - 5 - y, 0);
                    mineTilemap.SetTile(tilePosition, level1Rock);
                }
            }
        }

    }

}
