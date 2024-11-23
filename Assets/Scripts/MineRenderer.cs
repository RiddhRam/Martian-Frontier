using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour, IDataPersistence
{
    // Have to change through hierarchy not through here
    [SerializeField]
    private int visionRadius;
    public GameObject playerState;
    public GameObject largeFogOfWar;
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public TileBase unknownTile;
    public RuleTile level1Rock;  // Tile to place on the grid
    public TileBase IronOre;
    public TileBase SulfurOre;
    public TileBase LimestoneRock;

    // These are used to reveal which tile is at a position
    private TileBase[] tileValues;
    private readonly Vector2Int gridSize = new(25, 12); // 25x12 grid
    // Array of tile values for each chunk in each tilemap (row)
    // [chunk row] [tile world x-coordinate] [tile world y-coordinate]
    // Tiles will start in unplaced, then move to placed when revealed, then move to destroyed when destroyed
    // destroyed and placed are used to save the game
    private readonly SerializableDictionary<Vector2Int, int>[] unplacedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[36];
    private readonly SerializableDictionary<Vector2Int, int>[] placedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[36];
    private readonly SerializableDictionary<Vector2Int, int>[] destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[36];
    // Array of the tilemap Game objects
    private Tilemap[] tilemaps = new Tilemap[36];
    private readonly string[] materialNames = { "Limestone", "Sulfur", "Iron" };
    // The gameobject of each ore material to be instantied onto the map when mining ores
    [SerializeField]
    public GameObject[] materials;
    private UncollectedMaterialsDelegator materialsDelegator;
    private bool newGame = true;

    // Called before Start
    void Awake()
    {
        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // These are used to reveal which tile is at a position
        tileValues = new TileBase[] { level1Rock, LimestoneRock, SulfurOre, IronOre };
        if (newGame) {
            InitializeMine();
        }
    }

    // Called when game first loads, and the RefineryController calls this when it's battery reaches 0
    public void InitializeMine() {
        // Create first 4 rows
        for (int i = 1; i != 5; i++) {
            CreateTiles(i);
        }

        // Reveal the entry blocks, by calling destroy the tiles above the first few surface blocks
        // Even though there's no tiles here, it uses to vision radius to reveal other tiles around it
        // This is better since it doesn't just reveal the first few surface blocks
        DestroyTile(new(-4, -4));
        DestroyTile(new(-3, -4));
        DestroyTile(new(-2, -4));
        DestroyTile(new(-1, -4));
        DestroyTile(new(0, -4));
        DestroyTile(new(1, -4));
        DestroyTile(new(2, -4));
        DestroyTile(new(3, -4));
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

        SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue = new();
        SerializableDictionary<Vector2Int, int> placedTilemapsTileValue = new();
        SerializableDictionary<Vector2Int, int> destroyedTilemapsTileValue = new();

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
                    unplacedTilemapsTileValue.Add(new(tilePosition.x, tilePosition.y), 0);
                    mineTilemap.SetTile(tilePosition, unknownTile);
                }
            }

            // Now place ore veins throughout the chunk
            GenerateOreVeins(unplacedTilemapsTileValue, i, chunkRow);
        }

        unplacedTilemapsTileValues[chunkRow - 1] = unplacedTilemapsTileValue;
        placedTilemapsTileValues[chunkRow - 1] = placedTilemapsTileValue;
        destroyedTilemapsTileValues[chunkRow - 1] = destroyedTilemapsTileValue;
        tilemaps[chunkRow - 1] = mineTilemap;

        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (chunkRow == 36) {
            largeFogOfWar.transform.position = new Vector3(0, -3000, 0);
            return;
        }
        // If not last row, just move it down
        largeFogOfWar.transform.position = new Vector3(0, largeFogOfWar.transform.position.y - 12, 0);
    }

    private void GenerateOreVeins(SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue, int chunkX, int chunkRow)
    {
        int veinCount = Random.Range(2, 5);

        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            int centerX = Random.Range(0, gridSize.x);
            int centerY = Random.Range(0, gridSize.y);
            int radius = Random.Range(2, 4); // Radius of 2-4 tiles for variation

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            int oreToPlace = SelectOreBasedOnDepth(chunkRow, 1);

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Create a random offset to make the blob shape irregular
                    float distanceFromCenter = Mathf.Sqrt(x * x + y * y) + Random.Range(-0.5f, 0.5f);

                    // Only place tiles within the defined radius and randomness threshold
                    if (distanceFromCenter > radius) {
                        continue;
                    }

                    int tileX = centerX + x;
                    int tileY = centerY + y;

                    // Ensure we stay within grid bounds
                    if (tileX < 0 || tileX >= gridSize.x || tileY < 0 || tileY >= gridSize.y) {
                        continue;
                    }

                    // Get the position and place it in the SerializableDictionary
                    Vector2Int tilePosition = new(chunkX + tileX, (chunkRow - 1) * -12 - 5 - tileY);
                    unplacedTilemapsTileValue[tilePosition] = oreToPlace;
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
        
        // Move tile to placed
        int tileValue = unplacedTilemapsTileValues[tilemapIndex][tilePos];
        unplacedTilemapsTileValues[tilemapIndex].Remove(tilePos);
        placedTilemapsTileValues[tilemapIndex].Add(tilePos, tileValue);

        tilemaps[tilemapIndex].SetTile(new(tilePos.x, tilePos.y), tileValues[tileValue]);
    }

    public void DestroyTile(Vector3Int tileToDestroy) {
        int tilemapIndex = Mathf.FloorToInt((tileToDestroy.y + 5) / -12f);
        tilemapIndex = Mathf.Clamp(tilemapIndex, 0, 35);

        Tilemap tilemap = tilemaps[tilemapIndex];
        TileBase tileMined = tilemap.GetTile(tileToDestroy);

        // Move tile to destroyed
        // This is in a try catch because it will throw an error when initializing the mine
        try {
            int tileValue = placedTilemapsTileValues[tilemapIndex][new(tileToDestroy.x, tileToDestroy.y)];
            placedTilemapsTileValues[tilemapIndex].Remove(new(tileToDestroy.x, tileToDestroy.y));
            destroyedTilemapsTileValues[tilemapIndex].Add(new(tileToDestroy.x, tileToDestroy.y), tileValue);
        } catch {
        }

        tilemap.SetTile(tileToDestroy, null);

        // Search in a radius around tileToDestroy
        for (int x = -visionRadius; x <= visionRadius; x++) {
            for (int y = -visionRadius; y <= visionRadius; y++) {
                Vector2Int checkPos = new(tileToDestroy.x + x, tileToDestroy.y + y);

                // Calculate the Manhattan distance from the center tile
                int distance = Mathf.Abs(x) + Mathf.Abs(y);

                // If within the circular radius
                if (distance <= visionRadius) {
                    // Get the tilemap index, shouldbe anywhere between 0 and 35
                    tilemapIndex = Mathf.FloorToInt((tileToDestroy.y + y + 5) / -12f);
                    tilemapIndex = Mathf.Clamp(tilemapIndex, 0, 35);
                    
                    // Check if the tile exists in unplacedTilemapsTileValues
                    if (unplacedTilemapsTileValues[tilemapIndex].ContainsKey(checkPos)) {
                        RevealTile(checkPos);
                    }
                }
            }
        }
   
        if (tileToDestroy.y != -4) {
            bool oreMined = false;
            if (IdentifyTile(tileMined) != 0) {
                oreMined = true;
            }
            
            playerState.GetComponent<PlayerState>().NewBlockMined(oreMined);
        }
    }

    public TileBase[] GetOres() {
        return new TileBase[] { IronOre, SulfurOre, LimestoneRock };
    }

    public string[] GetMaterialNames() {
        return materialNames;
    }

    // Get the index of the tile
    private int IdentifyTile(TileBase tileToIdentify) {

        int index = 0;

        for (int i = 0; i != tileValues.Length; i++) {
            if (tileToIdentify == tileValues[i]) {
                index = i;
                break;
            }
        }

        return index;
    }

    public void LoadData(GameData data) {
        // this.materials = array of game objects for the materials
        // data.materials = dictionary of MaterialManager values at string keys, where the strings are the ids
        SerializableDictionary<string, MaterialManagerData> savedMaterials = data.materials;

        foreach (string id in savedMaterials.Keys) {
            GameObject newMaterial = Instantiate(materials[savedMaterials[id].materialIndex]);
            
            // Copy all the saved values into the loaded material
            MaterialManagerData savedMaterialManager = savedMaterials[id];
            // Need to manually put it in the right spot, do this before SetCount, so it happens before UpdateData() in MaterialManager
            newMaterial.transform.localPosition = savedMaterialManager.position;

            MaterialManager newMaterialManager = newMaterial.GetComponent<MaterialManager>();
            newMaterialManager.materialName = savedMaterialManager.materialName;
            newMaterialManager.materialIndex = savedMaterialManager.materialIndex;
            newMaterialManager.id = savedMaterialManager.id;
            newMaterialManager.SetCount(savedMaterialManager.count);
            
            materialsDelegator.AddMaterial(newMaterial);
        }

        //newGame = false;
    }

    public void SaveData(ref GameData data) {
        data.materials = materialsDelegator.uncollectedMaterials;
        data.placedTilemapsTileValues = this.placedTilemapsTileValues;
        data.destroyedTilemapsTileValues = this.destroyedTilemapsTileValues;
    }
}