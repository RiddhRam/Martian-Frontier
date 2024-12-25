using System.Collections.Generic;
using System.Linq;
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
    // These are used to reveal which tile is at a position, includes base rock tile, and ores
    public TileBase[] tileValues;
    private readonly int totalRows = 42;
    private readonly Vector2Int gridSize = new(25, 12);
    // Array of tile values for each chunk in each tilemap (row)
    // [chunk row] [tile world x-coordinate] [tile world y-coordinate]
    // Tiles will start in unplaced, then are copied (but not removed) to revealed when revealed, then remove from unplaced and revealed and placed in destroyed when destroyed
    // destroyed and revealed are used to save the game
    private SerializableDictionary<Vector2Int, int>[] unplacedTilemapsTileValues;
    private SerializableDictionary<Vector2Int, int>[] revealedTilemapsTileValues;
    // This doesn't need to be a dictionary, just a list, because we already know the tile value
    // If a tile is destroyed, it will be set to null
    // It's going to stay as a list as a future anti cheat measure
    // We can see if the user is creating materials out of nowhere or has made more money than possible from this mine
    private  SerializableDictionary<Vector2Int, int>[] destroyedTilemapsTileValues;
    // Array of the tilemap Game objects
    private Tilemap[] tilemaps;
    // The gameobject of each ore material to be instantied onto the map when mining ores
    private GameObject[] materials;
    private Sprite[] materialSprites;
    private UncollectedMaterialsDelegator materialsDelegator;
    [SerializeField]
    private int seed;
    [SerializeField]
    private int highestRow = 0;

    // 0 = Not created
    // 1 = in the process of initializing
    // 2 = initialized
    public int mineInitialization = 0;
    // Indicates the index of new tiers in tileValues
    public int[] tierThresholds = new int[3];
    public int[] oresPerTier = new int[3];

    // Called before Start
    void Awake()
    {
        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
        unplacedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalRows];
        revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalRows];
        destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalRows];
        tilemaps = new Tilemap[totalRows];

        // Set the thresholds to the right index based on the tile names
        for (int i = 0; i != tileValues.Length; i++) {
            string[] nameParts = tileValues[i].name.Split(' ');
            if (nameParts[0] == "Level") {
                tierThresholds[int.Parse(nameParts[1]) - 1] = i;
            }
        }

        for (int i = 0; i != tierThresholds.Length; i++) {
            if (i == tierThresholds.Length - 1) {
                oresPerTier[i] = tileValues.Length - tierThresholds[i] - 1;
                break;
            }
            oresPerTier[i] = tierThresholds[i+1] - tierThresholds[i] - 1;
        }
    
        OreDelegation oreDelegation = GameObject.Find("Ore Prices").GetComponent<OreDelegation>();
        materials = oreDelegation.materials;
        materialSprites = oreDelegation.materialSprites;
    }

    // Start is called before the first frame update
    void Start()
    {
        // This doesn't necessarily mean the player is new, just that a new mine is needed
        if (mineInitialization == 0) {
            InitializeMine();
        }
    }

    // Called when game first loads, and the RefineryController calls this when it's battery reaches 0
    public void InitializeMine() {

        // If mineInitialization == 1 then the user already saw the first few blocks before they left the game
        // Don't make a new seed, just use the last one
        if (mineInitialization < 2) {
            System.DateTime epoch = new System.DateTime(2024, 8, 8, 0, 0, 0, System.DateTimeKind.Utc);
            seed = (int)(System.DateTime.UtcNow - epoch).TotalSeconds;
            Random.InitState(seed);
        }
        
        mineInitialization = 1;

        // Make sure everything is clear
        unplacedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalRows];
        revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[unplacedTilemapsTileValues.Length];
        destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[unplacedTilemapsTileValues.Length];

        // Populate destroyedTilemapsTileValues and revealedTilemapsTileValues with empty dictionaries
        // unplacedTilemapsTileValues will be populated as each row is created
        for (int i = 0; i != unplacedTilemapsTileValues.Length; i++) {
            destroyedTilemapsTileValues[i] = new();
            revealedTilemapsTileValues[i] = new();
        }
        // Remove all keys
        materialsDelegator.uncollectedMaterials.Clear();

        // Create first 4 rows
        for (int i = 1; i != 5; i++) {
            CreateTiles(i);
        }

        // Reveal the entry blocks, by calling destroy the tiles above the first few surface blocks
        // Even though there's no tiles here, it uses to vision radius to reveal other tiles around it
        // This is better since it doesn't just reveal the first few surface blocks

        DestroyTile(new(-4, -4), true);
        DestroyTile(new(-3, -4), true);
        DestroyTile(new(-2, -4), true);
        DestroyTile(new(-1, -4), true);
        DestroyTile(new(0, -4), true);
        DestroyTile(new(1, -4), true);
        DestroyTile(new(2, -4), true);
        DestroyTile(new(3, -4), true);
        mineInitialization = 2;
        SaveGame();
        AnalyticsDelegator.Instance.InitializeMine(highestRow);
    }

    // Places tiles in a 25x12 rectangle, starting from (-75, -5) and going to the right and downward
    public void CreateTiles(int chunkRow)
    {
        highestRow = chunkRow;

        GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
        mineTilemapGameObject.transform.SetParent(transform);
        mineTilemapGameObject.name = "Row " + chunkRow;

        Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();

        // Find the level of the rocks
        int level = 0;
        int tileValueIndex = 0;
        if (chunkRow < 2 * totalRows/3 && chunkRow >= totalRows/3) {
            level = 1;
            tileValueIndex = 4;
        } else if (chunkRow >= 2 * totalRows/3) {
            level = 2;
            tileValueIndex = 8;
        }

        SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue = new();

        // Generate 6 grids in each tilemap
        for (int i = -75; i != 75; i += 25) {
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * -(gridSize.y) - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile

            // Set the base tiles of the chunk to unknown tile
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(i + x, (chunkRow - 1) * -(gridSize.y) - 5 - y, 0);
                    
                    // Add this coordinate, use a base tile
                    // Level 1 base tile = 0, level 2 = 4, level 3 = 8
                    unplacedTilemapsTileValue.Add(new(tilePosition.x, tilePosition.y), tileValueIndex);
                    mineTilemap.SetTile(tilePosition, unknownTile);
                }
            }

            // Now place ore veins throughout the chunk
            GenerateOreVeins(unplacedTilemapsTileValue, i, chunkRow, level);
        }

        unplacedTilemapsTileValues[chunkRow - 1] = unplacedTilemapsTileValue;
        tilemaps[chunkRow - 1] = mineTilemap;

        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (chunkRow == unplacedTilemapsTileValues.Length) {
            largeFogOfWar.transform.position = new Vector3(0, -3000, 0);
            return;
        }

        // If not last row, just move it down
        largeFogOfWar.transform.position = new Vector3(0, -220 - (chunkRow * gridSize.y), 0);
    }

    public void LoadTiles() {
        int savedHighestRow = highestRow;
        // highestRow is going to get reassigned in CreateTiles, so save it's value
        
        // We create all tiles first, that way there's no error when revealing tiles when we run DestroyTile
        for (int i = 0; i != savedHighestRow; i++) {
            // Destroy Generation Trigger
            if (i >= 4) {
                
                Destroy(GameObject.Find("Generate Row (" + (i + 1) + ")"));
            }

            // Create tiles for this row which populates unplacedTilemapsTileValues
            CreateTiles(i + 1);
        }

        for (int i = 0; i != savedHighestRow; i++) {
            List<Vector2Int> tileKeys = new List<Vector2Int>(revealedTilemapsTileValues[i].Keys);
            foreach (Vector2Int tileKey in tileKeys) {
                // If this tile is supposed to be destroyed, destroy it
                RevealTile(new(tileKey.x, tileKey.y));
            }

            // Then we go through unplacedTilemapsTileValues, reveal the placed ones and set the destroyed ones to null
            tileKeys = new List<Vector2Int>(destroyedTilemapsTileValues[i].Keys);
            foreach (Vector2Int tileKey in tileKeys) {

                // If this tile is supposed to be destroyed, destroy it
                DestroyTile(new(tileKey.x, tileKey.y), true);
            }
        }

    }

    private void GenerateOreVeins(SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue, int chunkX, int chunkRow, int level)
    {
        int veinCount = Random.Range(1, 2);

        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            int centerX = Random.Range(0, gridSize.x);
            int centerY = Random.Range(0, gridSize.y);
            int radius = Random.Range(2, 4); // Radius of 2-4 tiles for variation

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            int oreToPlace = SelectOreBasedOnDepth(chunkRow, level);

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
                    Vector2Int tilePosition = new(chunkX + tileX, (chunkRow - 1) * -(gridSize.y) - 5 - tileY);
                    unplacedTilemapsTileValue[tilePosition] = oreToPlace;
                }
            }
        }
    }

    // Method to select an ore based on depth
    private int SelectOreBasedOnDepth(int chunkRow, int level)
    {
        // Define the ore range for this tier
        int minOreIndex = tierThresholds[level] + 1;
        int maxOreIndex = tierThresholds[level] + oresPerTier[level];
        int oreCount = maxOreIndex - minOreIndex + 1;

        // Calculate the probability weights based on depth
        float depthFactor = Mathf.Clamp01((chunkRow - 11 * level - 1) / 10f);  // Lower 10f to make the rarity change faster, increase to change it slower
        float[] weights = new float[oreCount];
        float totalWeight = 0f;

        // Calculate dynamic weights for each ore
        for (int i = 0; i < oreCount; i++)
        {
            // Formula: (1 - depthFactor) favors low indexes, depthFactor favors high indexes
            weights[i] = (float) System.Math.Pow((1 - depthFactor) * (oreCount - i) + depthFactor * (i + 1), 2);
            totalWeight += weights[i];
        }

        // Normalize weights to create probabilities
        for (int i = 0; i < oreCount; i++)
        {
            weights[i] /= totalWeight;
        }

        // Random selection based on probabilities
        float randomValue = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < oreCount; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                return tierThresholds[level] + i + 1; // Return the selected ore index
            }
        }

        return oreCount - 1; // Fallback in case of floating-point error
    }

    public void RevealTile(Vector2Int tilePos) {

        int tilemapIndex = CalculateTileMapIndex(tilePos.y);
        int tileValue = unplacedTilemapsTileValues[tilemapIndex][tilePos];

        // Copy value to revealedTilemapsTileValues
        // Uses more memory but it's small so doesn't matter
        revealedTilemapsTileValues[tilemapIndex][tilePos] = tileValue;
        tilemaps[tilemapIndex].SetTile(new(tilePos.x, tilePos.y), tileValues[tileValue]);
    }

    public void DestroyTile(Vector3Int tileToDestroy, bool loading) {
        int tilemapIndex = CalculateTileMapIndex(tileToDestroy.y);

        Tilemap tilemap = tilemaps[tilemapIndex];
        TileBase tileMined = tilemap.GetTile(tileToDestroy);

        int tileValue = 0;
        // Move tile to destroyed
        // fails when initializing because the first row that has DestroyTile being called on it isn't actually part of the map
        // revealedTilemapsTileValues is a subset of unplacedTilemapsTileValues
        // it's just a quick way to reveal the first few tiles
        try {
            tileValue = unplacedTilemapsTileValues[tilemapIndex][new(tileToDestroy.x, tileToDestroy.y)];
            unplacedTilemapsTileValues[tilemapIndex].Remove(new(tileToDestroy.x, tileToDestroy.y));
            revealedTilemapsTileValues[tilemapIndex].Remove(new(tileToDestroy.x, tileToDestroy.y));
        } catch {
        }

        // This one fails when normally destroying or initializing
        // It only doesn't fail when loading
        // a value will only be part of destroyedTilemapsTileValues if it was already destroyed, so it must be loaded if this doesn't fail
        try {
            tileValue = destroyedTilemapsTileValues[tilemapIndex][new(tileToDestroy.x, tileToDestroy.y)];
        } catch {
        }

        destroyedTilemapsTileValues[tilemapIndex][new(tileToDestroy.x, tileToDestroy.y)] = tileValue;
        tilemap.SetTile(tileToDestroy, null);

        // Search in a radius around tileToDestroy
        for (int x = -visionRadius; x <= visionRadius; x++) {
            for (int y = -visionRadius; y <= visionRadius; y++) {
                Vector2Int checkPos = new(tileToDestroy.x + x, tileToDestroy.y + y);

                // Calculate the Manhattan distance from the center tile
                int distance = Mathf.Abs(x) + Mathf.Abs(y);

                // Make sure within the circular radius
                if (distance > visionRadius) {
                    continue;
                }

                // Get the tilemap index, shouldbe anywhere between 0 and 35
                tilemapIndex = CalculateTileMapIndex(tileToDestroy.y + y);
                
                // Check if the tile exists in unplacedTilemapsTileValues
                if (unplacedTilemapsTileValues[tilemapIndex].ContainsKey(checkPos)) {
                    RevealTile(checkPos);
                }
            }
        }
   
        // If one of the top row tiles, or the mine is being loaded from a save, don't count towards stats
        if (tileToDestroy.y == 4 || loading) {
            return;
        }
   
        bool oreMined = false;
        if (IdentifyTile(tileMined) != 0) {
            oreMined = true;
        }
        
        playerState.GetComponent<PlayerState>().NewBlockMined(oreMined);
    }

    public TileBase[] GetOres() {
        TileBase[] ores = new TileBase[tileValues.Length - tierThresholds.Length];

        int counter = 0;
        for (int i = 0; i != tileValues.Length; i++) {
            // Only add tiles that aren't rock tiles
            bool tierIndex = false;
            for (int j = 0; j != tierThresholds.Length; j++) {
                if (i == tierThresholds[j]) {
                    tierIndex = true;
                    break;
                }
            }

            if (tierIndex) {
                continue;
            }

            ores[counter] = tileValues[i];
            counter++;
        }

        // Return only the tiles of ores
        return ores;
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
        this.seed = data.seed;
        Random.InitState(this.seed);

        // If mine is already initialized, then this is not a new game
        // This doesn't necessarily mean the player is new, just that a new mine is needed
        this.mineInitialization = data.mineInitialization;

        SerializableDictionary<string, MaterialManagerData> savedMaterials = data.materials;
        this.revealedTilemapsTileValues = data.revealedTilemapsTileValues;
        this.destroyedTilemapsTileValues = data.destroyedTilemapsTileValues;
        this.highestRow = data.highestRow;
        
        foreach (string id in savedMaterials.Keys) {
            // Copy all the saved values into the loaded material
            MaterialManagerData savedMaterialManager = savedMaterials[id];
            CreateNewMaterial(savedMaterialManager.materialIndex, savedMaterialManager.count, savedMaterialManager.position);
        }

        LoadTiles();
        StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
    }

    public void SaveData(ref GameData data) {
        data.materials = materialsDelegator.uncollectedMaterials;
        data.revealedTilemapsTileValues = this.revealedTilemapsTileValues;
        data.destroyedTilemapsTileValues = this.destroyedTilemapsTileValues;
        data.seed = this.seed;
        data.highestRow = this.highestRow;
        data.mineInitialization = this.mineInitialization;
    }

    public int CalculateTileMapIndex(int tilePosY) {
        // Mine is offset by 5, and factor in the grid height too
        int tilemapIndex = Mathf.FloorToInt((tilePosY + 5) / -(gridSize.y));
        tilemapIndex = Mathf.Clamp(tilemapIndex, 0, 41);
        return tilemapIndex;
    }

    private void SaveGame() {
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
    }

    public int GetTileTier(TileBase tileToIdentify) {
        int tier = 1;

        for (int i = 0; i != tileValues.Length; i++) {
            if (tileToIdentify != tileValues[i]) {
                continue;
            }

            for (int j = 0; j != tierThresholds.Length; j++) {
                if (tierThresholds[j] <= i) {
                    tier = j + 1;
                }
            }

            break;
        }

        return tier;
    }

    // This function is also used by PlayerVehicleDelegation
    public void CreateNewMaterial(int materialIndex, int materialCount, Vector3 materialPosition) {
        GameObject newMaterial = Instantiate(materials[materialIndex]);
        materialsDelegator.AddMaterial(newMaterial, materialSprites[materialIndex], materialPosition, materialIndex, materialCount);
    }

    public void SetVisionRadius(int newRadius) {
        visionRadius = newRadius;
    }
}