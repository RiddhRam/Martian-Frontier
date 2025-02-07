using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour, IDataPersistence
{
    // Have to change through hierarchy not through here
    [SerializeField]
    private int visionRadius;
    public GameObject largeFogOfWar;
    public GameObject generationTriggers;
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public GameObject mineBackgroundTilemapPrefab;
    public TileBase mineBackgroundRuleTile;
    public TileBase unknownTile;
    // These are used to reveal which tile is at a position, includes base rock tile, and ores
    public TileBase[] tileValues;
    public TextMeshProUGUI oresMinedText;
    public TextMeshProUGUI mineValueText;
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
    // Lowercase
    private string[] oreNames;
    // Uppercase
    private string[] materialNames;
    private int[] materialPrices;
    public UncollectedMaterialsDelegator materialsDelegator;
    [SerializeField]
    private int seed;
    public int highestRow = 0;

    // 0 = Not created
    // 1 = in the process of initializing
    // 2 = initialized
    public int mineInitialization = 0;
    // Indicates the index of new tiers in tileValues
    public int[] tierThresholds = new int[3];
    public int[] oresPerTier = new int[3];
    public DataPersistenceManager dataPersistenceManager;
    public AnalyticsDelegator analyticsDelegator;
    public OreDelegation oreDelegation;
    public DailyChallengeDelegator dailyChallengeDelegator;
    private Dictionary<string, int> quantities = new();
    public int[] oresCount;
    private readonly int[] materialPoolSizes = {23, 27, 30, 17, 24, 42, 13, 27, 50};
    private Queue<GameObject>[] materialPools;
    private List<GameObject> mineTilemaps;
    private List<GameObject> mineBackgroundTilemaps;
    private readonly List<Vector2Int> initializeTiles = new() { new(-4, -4), new(-3, -4), new(-2, -4), new(-1, -4), new(0, -4), new(1, -4), new(2, -4), new(3, -4)};
    public PlayerState playerStateScript;
    // Precompute reusable values
    float invGridHeight; // Precompute inverse for division
    float invGridWidth;  // Precompute inverse for division
    int totalRowsForFunc;
    int totalColumnsForFunc;


    private int tileTier;
    GameObject obj;
    private Vector2Int tilemapPos;
    private Tilemap tilemap;
    private TileBase tileMined;
    private int tileValue;
    private bool oreMined;
    private int tilemapRow;
    private int tilemapColumn;
    readonly List<Tilemap> destroyTilemapsToEdit = new();
    readonly List<List<Vector3Int>> destroyTilesForTilemaps = new();
    readonly HashSet<Vector2Int> tilesToReveal = new();
    readonly List<Tilemap> revealTilemapsToEdit = new();
    readonly List<List<Vector3Int>> revealTilesForTilemaps = new();
    int tilemapIndex;
    int identifiedTile;
    SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValueDictionary;
    int oresMined;
    int size;
    Vector3Int[] tilesToSet;
    TileBase[] tilesBeingRevealed;
    Vector3Int vectorValue;
    GameObject mineBackgroundTilemapGameObject;
    Tilemap mineTilemap;
    Tilemap mineBackgroundTilemap;
    SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue;
    int veinCount;
    int centerX;
    int centerY;
    int radius;
    int oreToPlace;
    int oreIndex;
    int minOreIndex;
    int maxOreIndex;
    int oreCount;
    float depthFactor;
    float[] weights;
    float totalWeight;
    float randomValue;
    float cumulative;
    bool isBaseTile;
    float distanceFromCenter;
    int tileX;
    int tileY;
    Vector2Int tilePosition;
    string childName;
    int y;
    int x;
    private Coroutine _loadDataCoroutine;
    private bool cloudLoading = false;
    public int currentOresMined = 0;
    public System.Numerics.BigInteger currentMineValue = 0;

    // Called before Start
    void Awake()
    {
        totalColumns = mapHalfLength * 2 / gridSize.x;
        totalRowsForFunc = totalRows - 1;
        totalColumnsForFunc = totalColumns - 1;
        invGridHeight = 1f / -gridSize.y; // Precompute inverse for division
        invGridWidth = 1f / gridSize.x;  // Precompute inverse for division

        unplacedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];
        revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];
        destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[totalColumns, totalRows];

        mineTilemaps = new List<GameObject>();
        mineBackgroundTilemaps = new List<GameObject>();

        // unplacedTilemapsTileValues will be populated as each row is created
        // These ones are done right now
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++) {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++) {
                // Avoid using new() to keep memory usage down
                destroyedTilemapsTileValues[i, j] = new();
                revealedTilemapsTileValues[i, j] = new();

                GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
                mineTilemapGameObject.transform.SetParent(transform);
                mineTilemapGameObject.name = "Row " + (i+1) + ", Column " + (j+1);
                ReturnTilemapObject(mineTilemapGameObject, i * 25, j * -gridSize.y - 5);

                // Get the component once, then no need to do it again later
                Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();
                tilemapsDictionary.Add(mineTilemapGameObject.name, mineTilemap);

                GameObject mineBackgroundTilemapGameObject = Instantiate(mineBackgroundTilemapPrefab);
                mineBackgroundTilemapGameObject.transform.SetParent(transform);
                ReturnBackgroundTilemapObject(mineBackgroundTilemapGameObject);                
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

        int sum = 0 ;

        for (int i = 0; i != tierThresholds.Length; i++) {
            if (i == tierThresholds.Length - 1) {
                oresPerTier[i] = tileValues.Length - tierThresholds[i] - 1;
                break;
            }
            oresPerTier[i] = tierThresholds[i+1] - tierThresholds[i] - 1;
            sum += oresPerTier[i];
        }

        oresCount = new int[sum];
    
        materials = oreDelegation.materials;
        oreNames = oreDelegation.GetOreNames();
        materialNames = oreDelegation.materialNames;
        materialPrices = oreDelegation.GetMaterialPrices();
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

        // Remove all keys
        materialsDelegator.uncollectedMaterials.Clear();

        // Create first 4 rows
        // Change to totalRows + 1 to create entire map
        for (int i = 1; i != 5; i++) {
            CreateTiles(i);
        }

        /* Uncomment this too to log the quantity of each ore
        for (int i = 0; i != oresCount.Length; i++) {
            Debug.Log(i + ": " + oresCount[i]);
        } */

        // Reveal the entry blocks, by calling destroy the tiles above the first few surface blocks
        // Even though there's no tiles here, it uses to vision radius to reveal other tiles around it
        // This is better than calling RevealTiles it doesn't just reveal the first few surface blocks
        DestroyTiles(initializeTiles, true);
        CreateGenTriggers();
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

            mineBackgroundTilemapGameObject = GetBackgroundTilemapObject();

            mineTilemap = tilemapsDictionary[GetTilemapObject().name];
            mineBackgroundTilemap = mineBackgroundTilemapGameObject.GetComponent<Tilemap>();
            
            // i = the x coordinate of the chunk;
            // (chunkRow - 1) * -(gridSize.y) - 5 = the y coordinate of the chunk

            // y = y coordinate of tile
            // x = x coordinate of tile
            unplacedTilemapsTileValue = new();

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

        MoveFogOfWar(chunkRow);
    }

    public void MoveFogOfWar(int rowLoaded) {
        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (rowLoaded == totalRows) {
            largeFogOfWar.transform.position = new Vector3(0, -3000, 0);
            return;
        }

        // If not last row, just move it down
        largeFogOfWar.transform.position = new Vector3(0, -220 - ((rowLoaded+ 1) * gridSize.y), 0);
    }

    public void LoadTiles() {
        int savedHighestRow = highestRow;
        // highestRow is going to get reassigned in CreateTiles, so save it's value
        // We create all tiles first, that way there's no error when revealing tiles when we run DestroyTiles
        
        // Destroy Generation Trigger

        try{
            Destroy(GameObject.Find("GenerationTriggers"));
        } catch {
        }

        CreateGenTriggers();

        for (int i = 0; i != savedHighestRow; i++) {
            Destroy(GameObject.Find("Generate Row (" + (i + 1) + ")"));
            // Create tiles for this row which populates unplacedTilemapsTileValues
            CreateTiles(i + 1);
        }

        List<Vector2Int> tilesToDestroy = new();
        HashSet<Vector2Int> tilesToReveal = new();

        for (int j = 0; j != totalColumns; j++) {
            for (int i = 0; i != savedHighestRow; i++) {
                List<Vector2Int> tileKeys = new List<Vector2Int>(revealedTilemapsTileValues[j, i].Keys);

                foreach (Vector2Int tileKey in tileKeys) {
                    // If this tile is supposed to be destroyed, destroy it
                    tilesToReveal.Add(new(tileKey.x, tileKey.y));
                }

                // Then we go through unplacedTilemapsTileValues, reveal the placed ones and set the destroyed ones to null
                tileKeys = new List<Vector2Int>(destroyedTilemapsTileValues[j, i].Keys);
                
                foreach (Vector2Int tileKey in tileKeys) {
                    // If this tile is supposed to be destroyed, destroy it
                    tilesToDestroy.Add(new(tileKey.x, tileKey.y));
                }
            }
        }

        RevealTiles(tilesToReveal);
        DestroyTiles(tilesToDestroy, true);
    }

    public void CreateGenTriggers() {
        // Create the new mine
        GameObject genTrigGameObject = Instantiate(generationTriggers);
        genTrigGameObject.transform.SetParent(transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigGameObject.name = genTrigGameObject.name.Substring(0, genTrigGameObject.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigGameObject.transform.childCount; i++) {
            genTrigGameObject.transform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(gameObject);
        }
    }

    private void GenerateOreVeins(SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue, int chunkX, int chunkRow, int level)
    {
        veinCount = Random.Range(1, 2);

        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            centerX = Random.Range(0, gridSize.x);
            centerY = Random.Range(0, gridSize.y);
            radius = Random.Range(1, 4); // Radius of 1-4 tiles for variation

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            oreToPlace = SelectOreBasedOnDepth(chunkRow, level);

            // In order to see quantity of each ore in the mine
            // Uncomment this, and in initialize mine generate entire map by change the for loop where it only generates first few rows
            // and also uncomment oresCount integer array above
            
            /*oreIndex = 0;
            for (int i = 0; i != tileValues.Length; i++) {
               isBaseTile = false;

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
                    distanceFromCenter = Mathf.Sqrt(x * x + y * y) + Random.Range(-0.5f, 0.5f);

                    // Only place tiles within the defined radius and randomness threshold
                    if (distanceFromCenter > radius) {
                        continue;
                    }

                    tileX = centerX + x;
                    tileY = centerY + y;

                    // Ensure we stay within grid bounds
                    if (tileX < 0 || tileX >= gridSize.x || tileY < 0 || tileY >= gridSize.y) {
                        continue;
                    }

                    // Get the position and place it in the SerializableDictionary
                    tilePosition = new(chunkX + tileX, (chunkRow - 1) * -gridSize.y - 5 - tileY);
                    unplacedTilemapsTileValue[tilePosition] = oreToPlace;
                }
            }
        }
    }

    // Method to select an ore based on depth
    private int SelectOreBasedOnDepth(int chunkRow, int level)
    {
        // Define the ore range for this tier
        minOreIndex = tierThresholds[level] + 1;
        maxOreIndex = tierThresholds[level] + oresPerTier[level];
        oreCount = maxOreIndex - minOreIndex + 1;

        // Calculate the probability weights based on depth
        depthFactor = Mathf.Clamp01((chunkRow - 11 * level - 1) / 10f);  // Lower 10f to make the rarity change faster, increase to change it slower
        weights = new float[oreCount];
        totalWeight = 0f;

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
        randomValue = Random.value;
        cumulative = 0f;

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

    public void RevealTiles(HashSet<Vector2Int> tilesToReveal) {

        revealTilemapsToEdit.Clear();
        revealTilesForTilemaps.Clear();

        foreach (Vector2Int tileToReveal in tilesToReveal) {
            tilemapPos = CalculateTileMapPos(tileToReveal);

            unplacedTilemapsTileValueDictionary = unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y];

            if (!unplacedTilemapsTileValueDictionary.ContainsKey(tileToReveal)) {
                continue;
            }

            tilemap = tilemaps[tilemapPos.x, tilemapPos.y];

            if (!revealTilemapsToEdit.Contains(tilemap)) {
                revealTilemapsToEdit.Add(tilemap);
                revealTilesForTilemaps.Add(new());
            }

            tilemapIndex = revealTilemapsToEdit.IndexOf(tilemap);
            // Find out what the tile is
            tileValue = unplacedTilemapsTileValueDictionary[tileToReveal];
            revealTilesForTilemaps[tilemapIndex].Add(new(tileToReveal.x, tileToReveal.y, tileValue));

            // Save value to revealedTilemapsTileValues
            revealedTilemapsTileValues[tilemapPos.x, tilemapPos.y][tileToReveal] = tileValue;
        }

        // Finally delete the tiles
        for (int i = 0; i != revealTilemapsToEdit.Count; i++) {
            size = revealTilesForTilemaps[i].Count;

            tilesToSet = new Vector3Int[size];
            tilesBeingRevealed = new TileBase[size];

            for (int j = 0; j != size; j++) {
                vectorValue = revealTilesForTilemaps[i][j];
                tilesToSet[j] = new(vectorValue.x, vectorValue.y);
                tilesBeingRevealed[j] = tileValues[vectorValue.z];
            }

            revealTilemapsToEdit[i].SetTiles(tilesToSet, tilesBeingRevealed);
        }
        
    }

    public void DestroyTiles(List<Vector2Int> tilesToDestroy, bool loading) {

        oresMined = 0;

        destroyTilemapsToEdit.Clear();
        destroyTilesForTilemaps.Clear();
        tilesToReveal.Clear();

        foreach (Vector3Int tileToDestroy in tilesToDestroy.Select(v => (Vector3Int)v))
        {
            tilemapPos = CalculateTileMapPos(new(tileToDestroy.x, tileToDestroy.y));
            
            tilemap = tilemaps[tilemapPos.x, tilemapPos.y];

            if (!destroyTilemapsToEdit.Contains(tilemap)) {
                destroyTilemapsToEdit.Add(tilemap);
                destroyTilesForTilemaps.Add(new());
            }

            tilemapIndex = destroyTilemapsToEdit.IndexOf(tilemap);
            destroyTilesForTilemaps[tilemapIndex].Add(tileToDestroy);

            tileValue = 0;
            // Move tile to destroyed
            // fails when initializing because the first row that has DestroyTiles being called on it isn't actually part of the map
            // revealedTilemapsTileValues is a subset of unplacedTilemapsTileValues
            // it's just a quick way to reveal the first few tiles
            try {
                unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
                revealedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
            } catch {
            }

            // Destroy the tile by setting to null and saving it
            destroyedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(tileToDestroy.x, tileToDestroy.y)] = tileValue;

            // If the mine is being loaded from a save, don't reveal tiles, unless the top row
            if (loading && tileToDestroy.y != -4) {
                continue;
            }

            // Reveal new tiles
            // Search in a radius around tileToDestroy
            for (int x = -visionRadius; x <= visionRadius; x++) {
                // Determine the y bounds for the current x to stay within the radius
                int yLimit = visionRadius - Mathf.Abs(x);

                for (int y = -yLimit; y <= yLimit; y++) {
                    // Get the tilemap index
                    tilemapPos = CalculateTileMapPos(new(tileToDestroy.x + x, tileToDestroy.y + y));
                    // Check if the tile exists in unplacedTilemapsTileValues
                    tilesToReveal.Add(new(tileToDestroy.x + x, tileToDestroy.y + y));
                }
            }
            
            // If one of the top row tiles, don't count towards stats
            if (tileToDestroy.y == -4 || loading) {
                continue;
            }

            tileMined = tilemap.GetTile(tileToDestroy);
    
            identifiedTile = IdentifyTile(tileMined);
            oreMined = true;

            for (int i = 0; i != tierThresholds.Length; i++) {
                if (identifiedTile == tierThresholds[i]) {
                    oreMined = false;
                    break;
                }
            }

            currentOresMined++;
            if (oreMined) {
                oresMined++;
                int adjustment = 0;

                for (int i = 0; i != tierThresholds.Length; i++) {
                    if (identifiedTile > tierThresholds[i]) {
                        adjustment++;
                    }
                }

                if (!quantities.ContainsKey(materialNames[identifiedTile - adjustment])) {
                    quantities[materialNames[identifiedTile - adjustment]] = 1;
                } else {
                    
                    quantities[materialNames[identifiedTile - adjustment]]++;
                }
            }
        }

        // Finally delete the tiles
        for (int i = 0; i != destroyTilemapsToEdit.Count; i++) {
            size = destroyTilesForTilemaps[i].Count;

            Vector3Int[] tilesToSet = new Vector3Int[size];
            TileBase[] nullTiles = new TileBase[size];

            for (int j = 0; j != size; j++) {
                tilesToSet[j] = destroyTilesForTilemaps[i][j];
            }

            destroyTilemapsToEdit[i].SetTiles(tilesToSet, nullTiles);
        }

        oresMinedText.text = currentOresMined.ToString();
        playerStateScript.NewBlockMined(oresMined, tilesToDestroy.Count);

        dailyChallengeDelegator.MinedOres(quantities);
        quantities.Clear();

        // Reveal the tiles
        RevealTiles(tilesToReveal);
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

    public string[] GetTier1OreNames() {
        string[] tier1OreNames = new string[oresPerTier[0]];

        for (int i = 0; i != oresPerTier[0]; i++) {
            tier1OreNames[i] = materialNames[i];
        }

        return tier1OreNames;
    }

    public string[] GetTier2OreNames() {
        string[] tier2OreNames = new string[oresPerTier[1]];

        for (int i = 0; i != oresPerTier[1]; i++) {
            tier2OreNames[i] = materialNames[oresPerTier[0] + i];
        }

        return tier2OreNames;
    }

    public string[] GetTier3OreNames() {
        string[] tier3OreNames = new string[oresPerTier[2]];

        for (int i = 0; i != oresPerTier[2]; i++) {
            tier3OreNames[i] = materialNames[oresPerTier[0] + oresPerTier[1] + i];
        }

        return tier3OreNames;
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
        // If there's already a coroutine running, stop it
        if (_loadDataCoroutine != null) {
            StopCoroutine(_loadDataCoroutine);
        }

        // This has to be done async so that we can return all objects to the pool when loading a cloud save
        // Return objects happens over several frames to reduce lag
        // Start the new coroutine and store its reference
        _loadDataCoroutine = StartCoroutine(AsyncLoadData(data));
    }

    private IEnumerator AsyncLoadData(GameData data) {

        bool currentCloudLoadState = cloudLoading;

        // RETURN ALL MATERIALS AND TILEMAPS TO OBJECT POOL
        yield return StartCoroutine(ReturnAllObjectsToPool());

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
        this.currentOresMined = data.currentOresMined;

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

        if (mineInitialization == 2) {
            LoadTiles();
        }
        
        if (currentCloudLoadState == cloudLoading) {
            cloudLoading = true;
            try {
                StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems(gameObject));
            } catch {
            }
        }

        yield break;
    }

    public void SaveData(ref GameData data) {
        data.materials = materialsDelegator.uncollectedMaterials;
        data.revealedTilemapsTileValues = this.revealedTilemapsTileValues;
        data.destroyedTilemapsTileValues = this.destroyedTilemapsTileValues;
        data.seed = this.seed;
        data.highestRow = this.highestRow;
        data.mineInitialization = this.mineInitialization;
        data.currentOresMined = this.currentOresMined;
    }

    public Vector2Int CalculateTileMapPos(Vector2Int tilePos) {
        // Mine is offset by 5, and factor in the grid height too
        // Calculate row and clamp

        tilemapRow = Mathf.Clamp(
            Mathf.FloorToInt((tilePos.y + 5) * invGridHeight),
            0, totalRowsForFunc
        );

        // Offset by half the width, since some x coords are negative, and some are positive
        // Calculate column and clamp
        tilemapColumn = Mathf.Clamp(
        Mathf.FloorToInt((tilePos.x + mapHalfLength) * invGridWidth),
        0, totalColumnsForFunc);

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

        currentMineValue += materialPrices[materialIndex] * materialCount;
        mineValueText.text = FormatPrice(currentMineValue);
        //return obj;
    }

    // Method to return an object to the pool
    public void ReturnMaterialObject(GameObject obj, int materialIndex, string materialID)
    {
        currentMineValue -= materialPrices[materialIndex] * materialsDelegator.uncollectedMaterials[materialID].count;
        mineValueText.text = FormatPrice(currentMineValue);

        materialsDelegator.RemoveMaterial(materialID);
        obj.SetActive(false);
        materialPools[materialIndex].Enqueue(obj);
    }

    public GameObject GetTilemapObject()
    {
        obj = mineTilemaps[0];
        mineTilemaps.RemoveAt(0);

        return obj;
    }

    public void ReturnTilemapObject(GameObject obj, int yChunk, int xChunk)
    {
        // Get the Tilemap component from the GameObject
        Tilemap tilemap = obj.GetComponent<Tilemap>();

        int positionsCount = tilemap.cellBounds.size.x * tilemap.cellBounds.size.y;;

        int tileIndex = 0;
        Vector3Int[] tilesForReturning = new Vector3Int[positionsCount];
        TileBase[] tilesBeingUsed = new TileBase[positionsCount];

        // Loop through all positions in the tilemap's bounds
        foreach (var position in tilemap.cellBounds.allPositionsWithin)
        {
            tilesForReturning[tileIndex] = position;
            tilesBeingUsed[tileIndex] = null;

            tileIndex++;
        }

        tilemap.SetTiles(tilesToSet, tilesBeingUsed);
        mineTilemaps.Insert(0, obj);
    }

    public GameObject GetBackgroundTilemapObject()
    {
        obj = mineBackgroundTilemaps[0];
        mineBackgroundTilemaps.RemoveAt(0);
        
        return obj;
    }

    public void ReturnBackgroundTilemapObject(GameObject obj)
    {
        // Get the Tilemap component from the GameObject
        Tilemap tilemap = obj.GetComponent<Tilemap>();

        int positionsCount = tilemap.cellBounds.size.x * tilemap.cellBounds.size.y;;

        int tileIndex = 0;
        Vector3Int[] tilesForReturning = new Vector3Int[positionsCount];
        TileBase[] tilesBeingUsed = new TileBase[positionsCount];

        // Loop through all positions in the tilemap's bounds
        foreach (var position in tilemap.cellBounds.allPositionsWithin)
        {
            tilesForReturning[tileIndex] = position;
            tilesBeingUsed[tileIndex] = null;

            tileIndex++;
        }

        tilemap.SetTiles(tilesToSet, tilesBeingUsed);
        mineBackgroundTilemaps.Insert(0, obj);
    }

    public IEnumerator ReturnAllObjectsToPool() {
        // Return materials
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("Material Tag");
        MaterialManager[] materials = new MaterialManager[taggedObjects.Length];

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            materials[i] = taggedObjects[i].GetComponent<MaterialManager>();
        }

        foreach (var material in materials) {
            ReturnMaterialObject(material.gameObject, material.materialIndex, material.id);
        }

        // Reset the mine        
        int counter = 0;

        // Split the mine reset work into intervals
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            // Skip null objects
            if (!child)
                continue;

            childName = child.name;

            // If a tilemap row, row generation trigger, or GenerationTriggers parent, or mine background tilemap
            if ((childName.Contains("Row") || childName.Contains("Generation") || childName.Contains("Background")) && child.activeSelf)
            {
                // Repool or destroy
                if (childName.Contains("Row")) {
                    // Define a regex to capture Y and X values
                    var match = Regex.Match(childName, @"Row (\d+), Column (\d+)");

                    y = int.Parse(match.Groups[1].Value);
                    x = int.Parse(match.Groups[2].Value);

                    ReturnTilemapObject(child, x * 25, y * -12 - 5);

                } else if (childName.Contains("Background")) {

                    ReturnBackgroundTilemapObject(child);
                } else {

                    Destroy(child);
                    i--;
                }
            

                // Only delete 42 per frame
                if (counter >= 84) {
                    yield return new WaitForSecondsRealtime(0.1f);
                    counter = 0;
                }
                counter++;
            }
        }

    }

    public void SetVisionRadius(int newRadius) {
        visionRadius = newRadius;
    }

    public int GetVisionRadius() {
        return visionRadius;
    }

    public SerializableDictionary<Vector2Int, int>[,] GetUnplacedTilemapsTileValues() {
        return unplacedTilemapsTileValues;
    }

    public SerializableDictionary<Vector2Int, int>[,] GetRevealedTilemapsTileValues() {
        return revealedTilemapsTileValues;
    }

    public SerializableDictionary<Vector2Int, int>[,] GetDestroyedTilemapsTileValues() {
        return destroyedTilemapsTileValues;
    }

    public int GetSeed() {
        return seed;
    }

    public string FormatPrice(System.Numerics.BigInteger price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor((float) price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("$0.#") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor((float) price / 1_000_000_000_000_000f * 1000) / 1000).ToString("$0.#") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor((float) price / 1_000_000_000_000f * 1000) / 1000).ToString("$0.#") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor((float) price / 1_000_000_000f * 1000) / 1000).ToString("$0.#") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor((float) price / 1_000_000f * 1000) / 1000).ToString("$0.#") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float) price / 1_000f * 1000) / 1000).ToString("$0.#") + "K";
        }

        // Return the original price as a string for smaller numbers
        return "$" + price.ToString();
    }

}