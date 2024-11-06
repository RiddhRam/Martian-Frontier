using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour
{
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public RuleTile level1Rock;  // Tile to place on the grid
    public TileBase IronOre;
    public TileBase SulfurOre;
    public TileBase LimestoneRock;

    private TileBase[] level1Ores;
    private readonly Vector2Int gridSize = new(25, 12); // 25x12 grid

    // Start is called before the first frame update
    void Start()
    {
        // These ores are used for the veins of level 1 chunks
        level1Ores = new TileBase[] { LimestoneRock, SulfurOre, IronOre };
    }

    // Places tiles in a 25x12 rectangle, starting from (-50, -5) and going to the right and downward
    public void CreateTiles(int chunkRow)
    {
        GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
        mineTilemapGameObject.transform.SetParent(transform);
        mineTilemapGameObject.name = "Row " + chunkRow;

        Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();

        // Find the level of the rocks
        /*
        int level = 3;
        if (chunkRow <= 12) {
            level = 1;
        } else if (chunkRow <= 24) {
            level = 2;
        }*/

        // Generate 4 chunks in each tilemap
        for (int i = -50; i != 50; i += 25) {
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * 12 - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile
            // Set the base tile of the chunk
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(i + x, (chunkRow - 1) * -12 - 5 - y, 0);
                    mineTilemap.SetTile(tilePosition, level1Rock);
                }
            }

            // Now place ore veins throughout the chunk
            int oreVeinCount = Random.Range(1, 3);
            GenerateOreVeins(mineTilemap, oreVeinCount, i, chunkRow);
        }
    }

    private void GenerateOreVeins(Tilemap mineTilemap, int veinCount, int chunkX, int chunkRow)
    {
        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            int centerX = Random.Range(0, gridSize.x);
            int centerY = Random.Range(0, gridSize.y);
            int radius = Random.Range(2, 4); // Radius of 3-5 tiles for variation

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            TileBase oreToPlace = SelectOreBasedOnDepth(chunkRow);

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Create a random offset to make the blob shape irregular
                    float distanceFromCenter = Mathf.Sqrt(x * x + y * y) + Random.Range(-0.5f, 0.5f);

                    // Only place tiles within the defined radius and randomness threshold
                    if (distanceFromCenter <= radius)
                    {
                        int tileX = centerX + x;
                        int tileY = centerY + y;

                        // Ensure we stay within grid bounds
                        if (tileX >= 0 && tileX < gridSize.x && tileY >= 0 && tileY < gridSize.y)
                        {
                            Vector3Int tilePosition = new(chunkX + tileX, (chunkRow - 1) * -12 - 5 - tileY, 0);
                            mineTilemap.SetTile(tilePosition, oreToPlace);
                        }
                    }
                }
            }
        }
    }

    // Method to select an ore based on depth
    private TileBase SelectOreBasedOnDepth(int chunkRow)
    {
        // Calculate the probability weights based on depth
        float depthFactor = Mathf.Clamp01(chunkRow / 20f);  // Lower 10f to make the rarity change faster, increase to change it slower

        // Select an index between 0 and the highest array index. Random.Value can add 1 to the index, or it adds 0, no in between since the index will be an integer
        int selectedOreIndex = (int) Mathf.Clamp(depthFactor * level1Ores.Length + Random.value, 0, level1Ores.Length - 1);
        
        // Higher depths increase chances for rarer ores at the end of the array
        return level1Ores[selectedOreIndex];
    }
}

