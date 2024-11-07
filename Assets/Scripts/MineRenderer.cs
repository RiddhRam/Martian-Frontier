using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour
{
    // Have to change through hierarchy not through here
    public int visionRadius = 20;
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public TileBase unknownTile;
    public RuleTile level1Rock;  // Tile to place on the grid
    public TileBase IronOre;
    public TileBase SulfurOre;
    public TileBase LimestoneRock;

    // These ores are used for the veins of level 1 chunks
    private TileBase[] level1Ores;

    // These are used to reveal which tile is at a position
    private TileBase[] tileValues;
    private readonly Vector2Int gridSize = new(25, 12); // 25x12 grid
    // Array of tile values for each chunk in each tilemap (row)
    // [chunk row] [tile world x-coordinate] [tile world y-coordinate]
    private readonly Dictionary<Vector2Int, int>[] tilemapsTileValues = new Dictionary<Vector2Int, int>[36];
    // Array of the tilemap Game objects
    private Tilemap[] tilemaps = new Tilemap[36];

    // Start is called before the first frame update
    void Start()
    {
        // These ores are used for the veins of level 1 chunks
        level1Ores = new TileBase[] { LimestoneRock, SulfurOre, IronOre };

        // These are used to reveal which tile is at a position
        tileValues = new TileBase[] { level1Rock, LimestoneRock, SulfurOre, IronOre };

        for (int i = 1; i != 5; i++) {
            CreateTiles(i);
        }

        // Reveal the entry blocks
        RevealTile(new(-3, -5));
        RevealTile(new(-2, -5));
        RevealTile(new(-1, -5));
        RevealTile(new(0, -5));
        RevealTile(new(1, -5));
        RevealTile(new(2, -5));
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

        Dictionary<Vector2Int, int> chunkTileCoordinates = new();

        // Generate 4 chunks in each tilemap
        for (int i = -50; i != 50; i += 25) {
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * 12 - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile

            // Set the base tiles of the chunk to unknown tile
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(i + x, (chunkRow - 1) * -12 - 5 - y, 0);
                    
                    // Add this coordinate, use a base tile
                    // Level 1 base tile = 0, level 2 = 4, level 3 = 8
                    chunkTileCoordinates.Add(new(tilePosition.x, tilePosition.y), 0);
                    mineTilemap.SetTile(tilePosition, unknownTile);
                }
            }

            // Now place ore veins throughout the chunk
            int oreVeinCount = Random.Range(3, 5);
            GenerateOreVeins(chunkTileCoordinates, oreVeinCount, i, chunkRow);
        }

        tilemapsTileValues[chunkRow - 1] = chunkTileCoordinates;
        tilemaps[chunkRow-1] = mineTilemap;
    }

    private void GenerateOreVeins(Dictionary<Vector2Int, int> chunkTileCoordinates, int veinCount, int chunkX, int chunkRow)
    {
        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            int centerX = Random.Range(0, gridSize.x);
            int centerY = Random.Range(0, gridSize.y);
            int radius = Random.Range(2, 5); // Radius of 2-6 tiles for variation

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            int oreToPlace = SelectOreBasedOnDepth(chunkRow, 1);

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
                            Vector2Int tilePosition = new(chunkX + tileX, (chunkRow - 1) * -12 - 5 - tileY);
                            chunkTileCoordinates[tilePosition] = oreToPlace;
                        }
                    }
                }
            }
        }
    }

    // Method to select an ore based on depth
    private int SelectOreBasedOnDepth(int chunkRow, int level)
    {
        // Calculate the probability weights based on depth
        float depthFactor = Mathf.Clamp01(chunkRow / 10f);  // Lower 20f to make the rarity change faster, increase to change it slower

        // Select an index 
        // Random.Value can add 1 to the index, or it adds 0, no in between since the index will be an integer
        // Must be between 1-3 (level 1), 5-7 (level 2), 9-11 (level 3)

        int selectedOreIndex = (int) Mathf.Clamp(depthFactor * 3 + Random.value, 1, 3);
        
        // Higher depths increase chances for rarer ores at the end of the array
        return selectedOreIndex;
    }

    public void RevealTile(Vector2Int tilePos) {
        int tilemapIndex = Mathf.FloorToInt((tilePos.y + 5) / -12f);

        tilemaps[tilemapIndex].SetTile(new(tilePos.x, tilePos.y), tileValues[tilemapsTileValues[tilemapIndex][new(tilePos.x, tilePos.y)]]);
    }

    public void DestroyTile(Vector3Int tileToDestroy) {
        int tilemapIndex = Mathf.FloorToInt((tileToDestroy.y + 5) / -12f);
        tilemapIndex = Mathf.Clamp(tilemapIndex, 0, 35);

        Tilemap tilemap = tilemaps[tilemapIndex];

        tilemap.SetTile(tileToDestroy, null);
        tilemapsTileValues[tilemapIndex].Remove(new(tileToDestroy.x, tileToDestroy.y));

        // Search in a radius around tileToDestroy
        for (int x = -visionRadius; x <= visionRadius; x++) {
            for (int y = -visionRadius; y <= visionRadius; y++) {
                Vector2Int checkPos = new(tileToDestroy.x + x, tileToDestroy.y + y);

                tilemapIndex = Mathf.FloorToInt((tileToDestroy.y + y + 5) / -12f);
                tilemapIndex = Mathf.Clamp(tilemapIndex, 0, 35);
                
                // Check if the tile exists in tilemapsTileValues
                if (tilemapsTileValues[tilemapIndex].ContainsKey(checkPos)) {
                    RevealTile(checkPos);
                }
            }
        }
    }

    public TileBase[] GetOres() {
        return new TileBase[] { IronOre, SulfurOre, LimestoneRock };
    }
}

