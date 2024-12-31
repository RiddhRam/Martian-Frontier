using System.Collections.Generic;
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
    public GameObject mineBackgroundTilemapPrefab;
    public TileBase mineBackgroundRuleTile;
    public TileBase unknownTile;
    // These are used to reveal which tile is at a position, includes base rock tile, and ores
    public TileBase[] tileValues;
    // Height of the map, measured in tilemaps
    private readonly int totalRows = 42;
    // Width of the map, measured in tilemaps, calculated by using gridSize and mapHalfLength
    private int totalColumns;
    // Half the width of the map, measured in tiles
    private readonly int mapHalfLength = 75;
    private readonly Vector2Int gridSize = new(25, 12);
    // Array of tile values for each chunk in each tilemap (row)
    // [chunk row] [tile world x-coordinate] [tile world y-coordinate]
    // Tiles will start in unplaced, then are copied (but not removed) to revealed when revealed, then remove from unplaced and revealed and placed in destroyed when destroyed
    // destroyed and revealed are used to save the game
    private SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValues;
    private SerializableDictionary<Vector2Int, int>[,] revealedTilemapsTileValues;
    // This doesn't need to be a dictionary, just a list, because we already know the tile value
    // If a tile is destroyed, it will be set to null
    // It's going to stay as a list as a future anti cheat measure
    // We can see if the user is creating materials out of nowhere or has made more money than possible from this mine
    private  SerializableDictionary<Vector2Int, int>[,] destroyedTilemapsTileValues;
    // Use this to get a tilemap rather than calling GetComponent each time a tilemap is being mined
    // string = tilemap gameobject name
    // public so DrillerController can easily use it
    public Dictionary<string, Tilemap> tilemapsDictionary = new();
    // Array of the tilemap Game objects
    private Tilemap[,] tilemaps;
    // The gameobject of each ore material to be instantied onto the map when mining ores
    private GameObject[] materials;
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
    private DataPersistenceManager dataPersistenceManager;
    private AnalyticsDelegator analyticsDelegator;
    private OreDelegation oreDelegation;
    //private int[] oresCount = new int[9];
    private int[] materialPoolSizes = {23, 27, 30, 17, 24, 42, 13, 27, 50};
    private Queue<GameObject>[] materialPools;
    private PlayerState playerStateScript;

    private int tileTier;
    GameObject obj;
    private Vector2Int tilemapPos;
    private Tilemap tilemap;
    private TileBase tileMined;
    private int tileValue;
    private Vector2Int checkPos;
    private bool oreMined;
    private int distance;
    private int tilemapRow;
    private int tilemapColumn;

    // Called before Start
    void Awake()
    {
        totalColumns = mapHalfLength * 2 / gridSize.x;
        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
        unplacedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];
        revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];
        destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];

        // unplacedTilemapsTileValues will be populated as each row is created
        // These ones are done right now
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++) {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++) {
                // Avoid using new() to keep memory usage down
                destroyedTilemapsTileValues[i, j] = new();
                revealedTilemapsTileValues[i, j] = new();
            }
        }

        tilemaps = new Tilemap[totalColumns, totalRows];

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
    
        oreDelegation = GameObject.Find("Ore Prices").GetComponent<OreDelegation>();
        materials = oreDelegation.materials;

        dataPersistenceManager = GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>();
        playerStateScript = playerState.GetComponent<PlayerState>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // This doesn't necessarily mean the player is new, just that a new mine is needed
        if (mineInitialization == 0) {
            InitializeMine();
        }
        analyticsDelegator = AnalyticsDelegator.Instance;
    }

    // Called when game first loads, and the RefineryController calls this when it's battery reaches 0
    public void InitializeMine() {

        // If mineInitialization == 1 then the user already saw the first few blocks before they left the game
        // Don't make a new seed, just use the last one
        if (mineInitialization < 2) {
            // My birthday: Dec 8
            System.DateTime epoch = new System.DateTime(2024, 12, 8, 0, 0, 0, System.DateTimeKind.Utc);
            seed = (int)(System.DateTime.UtcNow - epoch).TotalSeconds;
            Random.InitState(seed);
        }
        
        mineInitialization = 1;

        // Clear all dictionaries in reveal and destroyed array
        // unplacedTilemapsTileValues will be populated as each row is created
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++) {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++) {

                // Try to avoid using new() to keep memory usage down
                if (destroyedTilemapsTileValues[i, j] == null) {
                    destroyedTilemapsTileValues[i, j] = new();
                    revealedTilemapsTileValues[i, j] = new();
                } else {
                    destroyedTilemapsTileValues[i, j].Clear();
                    revealedTilemapsTileValues[i, j].Clear();
                }
            }
        }

        // Clear all saved components
        tilemapsDictionary.Clear();
        // Remove all keys
        materialsDelegator.uncollectedMaterials.Clear();

        // Create first 4 rows
        for (int i = 1; i != 5; i++) {
            CreateTiles(i);
        }

        /* Uncomment this too to log the quantity of each ore
        for (int i = 0; i != oresCount.Length; i++) {
            Debug.Log(i + ": " + oresCount[i]);
        }*/

        // Reveal the entry blocks, by calling destroy the tiles above the first few surface blocks
        // Even though there's no tiles here, it uses to vision radius to reveal other tiles around it
        // This is better than calling RevealTile it doesn't just reveal the first few surface blocks
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

        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.InitializeMine(highestRow);
    }

    // Places tiles in a 25x12 rectangle, starting from (-mapHalfLength, -5) and going to the right and downward
    public void CreateTiles(int chunkRow)
    {
        highestRow = chunkRow;

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

        int chunkColumn = 1;
        // Generate 6 grids in each tilemap
        for (int i = -mapHalfLength; i != mapHalfLength; i += 25) {

            GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
            GameObject mineBackgroundTilemapGameObject = Instantiate(mineBackgroundTilemapPrefab);

            mineTilemapGameObject.transform.SetParent(transform);
            mineTilemapGameObject.name = "Row " + chunkRow + ", Column " + chunkColumn;

            mineBackgroundTilemapGameObject.transform.SetParent(transform);

            // Get the component once, then no need to do it again later
            Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();
            tilemapsDictionary.Add(mineTilemapGameObject.name, mineTilemap);

            Tilemap mineBackgroundTilemap = mineBackgroundTilemapGameObject.GetComponent<Tilemap>();
            
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * -(gridSize.y) - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile
            SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue = new();

            // Set the base tiles of the chunk to unknown tile
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(i + x, (chunkRow - 1) * -gridSize.y - 5 - y, 0);
                    
                    // Add this coordinate, use a base tile
                    // Level 1 base tile = 0, level 2 = 4, level 3 = 8
                    unplacedTilemapsTileValue.Add(new(tilePosition.x, tilePosition.y), tileValueIndex);
                    mineTilemap.SetTile(tilePosition, unknownTile);
                    mineBackgroundTilemap.SetTile(tilePosition, mineBackgroundRuleTile);
                }
            }

            // Now place ore veins throughout the chunk
            GenerateOreVeins(unplacedTilemapsTileValue, i, chunkRow, level);

            unplacedTilemapsTileValues[chunkColumn-1, chunkRow-1] = unplacedTilemapsTileValue;
            tilemaps[chunkColumn-1, chunkRow-1] = mineTilemap;

            chunkColumn++;
        }

        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (chunkRow == totalRows) {
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

        for (int j = 0; j != totalColumns; j++) {
            for (int i = 0; i != savedHighestRow; i++) {
            List<Vector2Int> tileKeys = new List<Vector2Int>(revealedTilemapsTileValues[j, i].Keys);
            foreach (Vector2Int tileKey in tileKeys) {
                Vector2Int tilemapPos = CalculateTileMapPos(new(tileKey.x, tileKey.y));
                // If this tile is supposed to be destroyed, destroy it
                RevealTile(new(tileKey.x, tileKey.y), tilemapPos);
            }

            // Then we go through unplacedTilemapsTileValues, reveal the placed ones and set the destroyed ones to null
            tileKeys = new List<Vector2Int>(destroyedTilemapsTileValues[j, i].Keys);
            foreach (Vector2Int tileKey in tileKeys) {

                // If this tile is supposed to be destroyed, destroy it
                DestroyTile(new(tileKey.x, tileKey.y), true);
            }
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

            // In order to see quantity of each ore in the mine
            // Uncomment this, in initialize mine generate entire map by changing for loop where it only generates first few rows
            // and also uncomment oresCount integer array above
            /*int oreIndex = 0;
            for (int i = 0; i != tileValues.Length; i++) {
                bool isBaseTile = false;

                for (int j = 0; j != tierThresholds.Length; j++) {
                    if (tierThresholds[j] == i) {
                        isBaseTile = true;
                        break;
                    }
                }

                if (isBaseTile) {
                    continue;
                }

                if (oreToPlace == i) {
                    break;
                }

                oreIndex++;
            }
            oresCount[oreIndex]++;*/

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

    public void RevealTile(Vector2Int tilePos, Vector2Int tilemapPos) {
        
        // Find out what the tile is
        tileValue = unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y][tilePos];

        //
        // Save value to revealedTilemapsTileValues
        revealedTilemapsTileValues[tilemapPos.x, tilemapPos.y][tilePos] = tileValue;

        // Finally reveal tile
        tilemaps[tilemapPos.x, tilemapPos.y].SetTile(new(tilePos.x, tilePos.y), tileValues[tileValue]);
    }

    public void DestroyTile(Vector3Int tileToDestroy, bool loading) {
        tilemapPos = CalculateTileMapPos(new(tileToDestroy.x, tileToDestroy.y));

        tilemap = tilemaps[tilemapPos.x, tilemapPos.y];
        tileMined = tilemap.GetTile(tileToDestroy);

        tileValue = 0;
        // Move tile to destroyed
        // fails when initializing because the first row that has DestroyTile being called on it isn't actually part of the map
        // revealedTilemapsTileValues is a subset of unplacedTilemapsTileValues
        // it's just a quick way to reveal the first few tiles
        try {
            tileValue = unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(tileToDestroy.x, tileToDestroy.y)];
            unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
            revealedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
        } catch {
            // This one fails when normally destroying or initializing
            // It only doesn't fail when loading
            // a value will only be part of destroyedTilemapsTileValues if it was already destroyed, so it must be loaded if this doesn't fail
            try {
                tileValue = destroyedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(tileToDestroy.x, tileToDestroy.y)];
            } catch {
            }
        }

        // Destroy the tile by setting to null and saving it
        destroyedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(tileToDestroy.x, tileToDestroy.y)] = tileValue;
        tilemap.SetTile(tileToDestroy, null);

        // Reveal new tiles
        // Search in a radius around tileToDestroy
        for (int x = -visionRadius; x <= visionRadius; x++) {
            for (int y = -visionRadius; y <= visionRadius; y++) {

                // Calculate the Manhattan distance from the center tile
                distance = Mathf.Abs(x) + Mathf.Abs(y);

                // Make sure within the circular radius
                if (distance > visionRadius) {
                    continue;
                }

                // Get the tilemap index
                tilemapPos = CalculateTileMapPos(new(tileToDestroy.x + x, tileToDestroy.y + y));
                checkPos = new(tileToDestroy.x + x, tileToDestroy.y + y);
                
                // Check if the tile exists in unplacedTilemapsTileValues
                if (unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].ContainsKey(checkPos)) {
                    RevealTile(checkPos, tilemapPos);
                }
            }
        }
        
        // If one of the top row tiles, or the mine is being loaded from a save, don't count towards stats
        if (tileToDestroy.y == -4 || loading) {
            return;
        }
   
        oreMined = false;
        if (IdentifyTile(tileMined) != 0) {
            oreMined = true;
        }
        
        playerStateScript.NewBlockMined(oreMined);
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

        // Create pools of materials before loading materials
        materialPools = new Queue<GameObject>[tileValues.Length - tierThresholds.Length];

        for (int i = 0; i != materialPools.Length; i++) {
            materialPools[i] = new Queue<GameObject>();
            // Create the right amount of each material according to each pool size
            for (int j = 0; j != materialPoolSizes[i]; j++) {
                GameObject newMaterial = Instantiate(materials[i]);
                newMaterial.SetActive(false);
                materialPools[i].Enqueue(newMaterial);
                newMaterial.transform.SetParent(materialsDelegator.transform);
            }
        }

        foreach (string id in savedMaterials.Keys) {
            // Copy all the saved values into the loaded material
            MaterialManagerData savedMaterialManager = savedMaterials[id];
            GetMaterialObject(savedMaterialManager.materialIndex, savedMaterialManager.position, savedMaterialManager.count);
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

    public Vector2Int CalculateTileMapPos(Vector2Int tilePos) {
        // Mine is offset by 5, and factor in the grid height too
        tilemapRow = Mathf.FloorToInt((tilePos.y + 5) / -gridSize.y);
        tilemapRow = Mathf.Clamp(tilemapRow, 0, totalRows - 1);

        // Offset by half the width, since some x coords are negative, and some are positive
        tilemapColumn = Mathf.FloorToInt((tilePos.x + mapHalfLength) / gridSize.x);
        tilemapColumn = Mathf.Clamp(tilemapColumn, 0, totalColumns - 1);

        return new(tilemapColumn, tilemapRow);
    }

    private void SaveGame() {
        if (!dataPersistenceManager) {
            return;
        }

        dataPersistenceManager.SaveGame();
    }

    public int GetTileTier(TileBase tileToIdentify) {
        tileTier = 1;

        for (int i = 0; i != tileValues.Length; i++) {
            if (tileToIdentify != tileValues[i]) {
                continue;
            }

            for (int j = 0; j != tierThresholds.Length; j++) {
                if (tierThresholds[j] <= i) {
                    tileTier = j + 1;
                }
            }

            break;
        }

        return tileTier;
    }

    public void GetMaterialObject(int materialIndex, Vector3 materialPosition, int materialCount)
    {

        if (materialPools[materialIndex].Count > 0)
        {
            obj = materialPools[materialIndex].Dequeue();
        }
        else
        {
            // Expand the pool if empty
            obj = Instantiate(materials[materialIndex]);
        }

        obj.transform.position = materialPosition;
        obj.SetActive(true);
        materialsDelegator.AddMaterial(obj, materialPosition, materialIndex, materialCount);
        //return obj;
    }

    // Method to return an object to the pool
    public void ReturnObject(GameObject obj, int materialIndex, string materialID)
    {
        materialsDelegator.RemoveMaterial(materialID);
        obj.SetActive(false);
        materialPools[materialIndex].Enqueue(obj);
    }

    public void SetVisionRadius(int newRadius) {
        visionRadius = newRadius;
    }
}