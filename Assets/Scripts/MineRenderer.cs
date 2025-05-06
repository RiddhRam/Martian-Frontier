using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour, IDataPersistence
{
    // Have to change through hierarchy not through here
    [SerializeField]
    private int visionRadius;
    public GameObject largeFogOfWar;
    public GameObject generationTriggers;
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public TileBase mineBackgroundRuleTile;
    public TileBase unknownTile;
    // These are used to reveal which tile is at a position, includes base rock tile, and ores
    public TileBase[] tileValues;
    public TextMeshProUGUI oresMinedText;

    // Height of the map, measured in tilemaps
    [SerializeField] private int totalRows = 42;
    // Width of the map, measured in tilemaps, calculated by using gridSize and mapHalfLength
    private int totalColumns;
    // Half the width of the map, measured in tiles
    private readonly int mapHalfLength = 75;
    private readonly Vector2Int gridSize = new(25, 12);
    // Array of tile values for each chunk in each tilemap (row)
    // [chunk row] [tile world x-coordinate] [tile world y-coordinate]
    // Tiles will start in unplaced, then are copied (but not removed) to revealed when revealed, then remove from unplaced and revealed and placed in destroyed when destroyed
    // destroyed and revealed are used to save the game
    public SerializableDictionary<Vector2Int, int>[,] unplacedTilemapsTileValues;
    private SerializableDictionary<Vector2Int, int>[,] revealedTilemapsTileValues;
    // This doesn't need to be a dictionary, just a list, because we already know the tile value
    // If a tile is destroyed, it will be set to null
    // It's going to stay as a list as a future anti cheat measure
    // We can see if the user is creating materials out of nowhere or has made more money than possible from this mine
    private SerializableDictionary<Vector2Int, int>[,] destroyedTilemapsTileValues;
    public bool[] generatedRows;
    // Use this to get a tilemap rather than calling GetComponent each time a tilemap is being mined
    // string = tilemap gameobject name
    // public so DrillerController can easily use it
    public Dictionary<string, Tilemap> tilemapsDictionary = new();
    // Array of the tilemap Game objects, same as above, but in a 2d array rather than a dictionary with the string as the key
    public Tilemap[,] tilemaps;

    // Uppercase names
    private string[] materialNames;

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
    public UpgradesDelegator upgradesDelegator;
    public RefineryController refineryController;

    private Dictionary<string, int> quantities = new();
    public int[] oresCount;

    private Queue<GameObject> materialPools;
    private List<GameObject> mineTilemaps;
    private readonly List<Vector2Int> initializeTiles = new() { new(-4, -4), new(-3, -4), new(-2, -4), new(-1, -4), new(0, -4), new(1, -4), new(2, -4), new(3, -4)};
    // Destroy these so haulers dont get stuck
    private readonly List<Vector2Int> coopInitializeTiles = new() { new(-3, -5), new(-2, -5), new(-1, -5), new(0, -5), new(1, -5), new(2, -5), new(3, -5), new(-3, -6), new(-2, -6), new(-1, -6), new(0, -6), new(1, -6), new(2, -6), new(3, -6), new(-3, -7), new(-2, -7), new(-1, -7), new(0, -7), new(1, -7), new(2, -7), new(3, -7), new(-3, -8), new(-2, -8), new(-1, -8), new(0, -8), new(1, -8), new(2, -8), new(3, -8), new(-3, -9), new(-2, -9), new(-1, -9), new(0, -9), new(1, -9), new(2, -9), new(3, -9)};
    public PlayerState playerStateScript;
    // Precompute reusable values
    float invGridHeight; // Precompute inverse for division
    float invGridWidth;  // Precompute inverse for division
    int totalRowsForFunc;
    int totalColumnsForFunc;
    public Transform genTrigTransform;


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
    Tilemap mineTilemap;
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
    // Actually current blocks mined, not ores
    public int currentOresMined = 0;
    public System.Numerics.BigInteger currentMineValue = 0;
    public int minVeinRadius;
    public int maxVeinRadius;
    public int minVeinCount;
    public int maxVeinCount;
    private GameObject child;
    private Tilemap tilemapToReturn;
    private TileBase[] tilesBeingUsed;
    private bool alreadyBeingReturned = false;
    private bool notSinglePlayerScene = false;

    public bool coopMineLoaded = false;
    private bool soloMineLoaded = false;

    int seedInUse;

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

        // unplacedTilemapsTileValues will be populated as each row is created
        // These ones are done right now
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++) {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++) {
                // Avoid using new() to keep memory usage down
                destroyedTilemapsTileValues[i, j] = new();
                revealedTilemapsTileValues[i, j] = new();

                GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);
                mineTilemapGameObject.transform.SetParent(transform);
                mineTilemapGameObject.name = "Column " + (i+1) + ", Row " + (j+1);
                ReturnTilemapObject(mineTilemapGameObject, i * 25, j * -gridSize.y - 5);

                // Get the component once, then no need to do it again later
                Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();
                tilemapsDictionary.Add(mineTilemapGameObject.name, mineTilemap);               
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

        materialNames = oreDelegation.materialNames;
    }

    // Called when game first loads, and the RefineryController calls this when it's battery reaches 0
    public void InitializeMine() {

        // If mineInitialization == 1 then the user already saw the first few blocks before they left the game
        // Don't make a new seed, just use the last one
        if (mineInitialization < 2) {
            // My birthday: Dec 8
            System.DateTime epoch = new System.DateTime(2024, 12, 8, 0, 0, 0, System.DateTimeKind.Utc);

            if (seed == 0) {
                // Tutorial map, limestone close to the surface
                seed = 11036764;
            } else {
                seed = (int)(System.DateTime.UtcNow - epoch).TotalSeconds;
            }
            
            Random.InitState(seed);
            seedInUse = seed;
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

        CreateGenTriggers();
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
        DestroyTiles(initializeTiles, true, false);
        if (notSinglePlayerScene) {
            // Not an npc, and is loading, but if you change it to true, false, then the surrounding tiles are not revealed
            DestroyTiles(coopInitializeTiles, false, true);
        }

        mineInitialization = 2;
        SaveGame();

        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator.InitializeMine(highestRow);
    }

    // Places tiles in a 25x12 rectangle, starting from (-mapHalfLength, -5) and going to the right and downward
    public void CreateTiles(int chunkRow, bool setHighestRow = true)
    {
        try {
            Destroy(GameObject.Find("Generate Row (" + (chunkRow) + ")"));
        } catch {
        }

        for (int i = 1; i < chunkRow; i++) {
            // Verify previous tiles were created
            if (!generatedRows[i]) {
                CreateTiles(i, false);
            }
        }

        if (generatedRows[chunkRow - 1]) {
            return;
        }

        if (setHighestRow) {
            highestRow = chunkRow;
            MoveFogOfWar(highestRow);
        }

        // Find the level of the rocks
        int level = 0;
        int tileValueIndex = 0;
        // 14 is the height of all tilemaps of 1 tier
        if (chunkRow < 2 * 14 && chunkRow >= 14) {
            level = 1;
            tileValueIndex = 4;
        } else if (chunkRow >= 2 * 14) {
            level = 2;
            tileValueIndex = 8;
        }

        if (level >= tierThresholds.Length) {
            return;
        }

        int chunkColumn = 1;
        // Generate 6 grids in each tilemap
        for (int i = -mapHalfLength; i != mapHalfLength; i += 25) {
            string name = GetTilemapObject().name;
            mineTilemap = tilemapsDictionary[name];
            
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
                }
            }

            // Now place ore veins throughout the chunk
            GenerateOreVeins(unplacedTilemapsTileValue, i, chunkRow, level);

            unplacedTilemapsTileValues[chunkColumn-1, chunkRow-1] = unplacedTilemapsTileValue;
            tilemaps[chunkColumn-1, chunkRow-1] = mineTilemap;

            chunkColumn++;
        }

        generatedRows[chunkRow - 1] = true;
    }

    public void MoveFogOfWar(int rowLoaded) {
        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (rowLoaded == totalRows || genTrigTransform.childCount == 1) {
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
        DestroyTiles(tilesToDestroy, true, false);
    }

    public void CreateGenTriggers() {
        generatedRows = new bool[totalRows];
        // Create the new mine triggers
        genTrigTransform = Instantiate(generationTriggers).transform;
        genTrigTransform.SetParent(transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigTransform.name = genTrigTransform.name.Substring(0, genTrigTransform.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigTransform.childCount; i++) {
            genTrigTransform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(this);
        }
    }

    private void GenerateOreVeins(SerializableDictionary<Vector2Int, int> unplacedTilemapsTileValue, int chunkX, int chunkRow, int level)
    {
        Random.InitState(seedInUse + chunkRow + chunkX + level);
        veinCount = Random.Range(minVeinCount, maxVeinCount);

        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            centerX = Random.Range(0, gridSize.x);
            centerY = Random.Range(0, gridSize.y);
            radius = Random.Range(minVeinRadius, maxVeinRadius); // Radius of 1-4 tiles for variation

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
            // Get tilemap pos index from dictionary
            tilemapPos = CalculateTileMapPos(tileToReveal);

            unplacedTilemapsTileValueDictionary = unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y];

            if (unplacedTilemapsTileValueDictionary == null || !unplacedTilemapsTileValueDictionary.ContainsKey(tileToReveal)) {
                continue;
            }

            // Save tilemap
            tilemap = tilemaps[tilemapPos.x, tilemapPos.y];

            // Make sure that we know this tilemap will be edited later
            if (!revealTilemapsToEdit.Contains(tilemap)) {
                revealTilemapsToEdit.Add(tilemap);
                revealTilesForTilemaps.Add(new());
            }

            // Get index of tilemap from list
            tilemapIndex = revealTilemapsToEdit.IndexOf(tilemap);

            // Find out what the tile is and set it as the z value to the vector 3
            tileValue = unplacedTilemapsTileValueDictionary[tileToReveal];
            revealTilesForTilemaps[tilemapIndex].Add(new(tileToReveal.x, tileToReveal.y, tileValue));

            // Save to revealedTilemapsTileValues
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

    public void DestroyTiles(List<Vector2Int> tilesToDestroy, bool loading, bool isNPC) {

        oresMined = 0;

        destroyTilemapsToEdit.Clear();
        destroyTilesForTilemaps.Clear();
        tilesToReveal.Clear();
        revealTilemapsToEdit.Clear();
        revealTilesForTilemaps.Clear();

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

            int visionBoost = 0;
            if (upgradesDelegator) {
                // Value is offset by 3
                visionBoost = upgradesDelegator.visionBoost - 3;
            }

            // Reveal new tiles
            // Search in a radius around tileToDestroy
            for (int x = 0; x <= visionRadius + visionBoost; x++)
            {
                int yLimit = visionRadius - x + visionBoost;
                for (int y = 0; y <= yLimit; y++)
                {
                    // Add all 4 quadrants
                    tilesToReveal.Add(new(tileToDestroy.x + x, tileToDestroy.y + y));
                    tilesToReveal.Add(new(tileToDestroy.x - x, tileToDestroy.y + y));
                    tilesToReveal.Add(new(tileToDestroy.x - x, tileToDestroy.y - y));
                    tilesToReveal.Add(new(tileToDestroy.x + x, tileToDestroy.y - y));
                }
            }
            
            // If one of the top row tiles, don't count towards stats
            if (tileToDestroy.y == -4 || loading) {
                continue;
            }

            tileMined = tilemap.GetTile(tileToDestroy);
    
            // Get tile index
            identifiedTile = GetTileIndex(tileMined);
            oreMined = true;

            if (!oreDelegation.VerifyIfOre(identifiedTile)) {
                oreMined = false;
            }

            // Actually current blocks mined, not ores
            currentOresMined++;

            if (!oreMined) {
                continue;
            }

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

        // Finally delete the tiles
        for (int i = 0; i != destroyTilemapsToEdit.Count; i++) {

            size = destroyTilesForTilemaps[i].Count;

            Vector3Int[] tilesToSet = new Vector3Int[size];
            TileBase[] tilesBeingChanged = new TileBase[size];

            // Set tiles being destroyed
            for (int j = 0; j != size; j++) {
                tilesToSet[j] = destroyTilesForTilemaps[i][j];
                // Leave tilesBeingChanged[j] as null since we are destroying it
            }

            destroyTilemapsToEdit[i].SetTiles(tilesToSet, tilesBeingChanged);
        }
        
        oresMinedText.text = currentOresMined.ToString();

        try {
            
            // If not loading
            if (!loading) {
                // If not from an npc
                if (!isNPC) {
                    playerStateScript.NewBlockMined(oresMined, tilesToDestroy.Count);
                    dailyChallengeDelegator.MinedOres(quantities);
                }

                // Track which ores are being sold so the player can get paid
                int[] newMaterials = new int[9];
                foreach (string name in quantities.Keys) {
                    newMaterials[oreDelegation.GetTileIndexByName(name)] = quantities[name];
                }
                
                // Finally pay player
                refineryController.SellOres(newMaterials, isNPC);
            }
            
        } catch (System.Exception ex) {
            Debug.Log(ex.Message);
        }

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
    private int GetTileIndex(TileBase tileToIdentify) {

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

        if (SceneManager.GetActiveScene().name.ToLower().Contains("singleplayer")) {
            notSinglePlayerScene = false;
            // This has to be done async so that we can return all objects to the pool when loading a cloud save
            // Return objects happens over several frames to reduce lag
            // Start the new coroutine and store its reference
            _loadDataCoroutine = StartCoroutine(AsyncLoadData(data));
            return;
        }

        notSinglePlayerScene = true;
        StartCoroutine(LoadCoopLocal());
    }

    private IEnumerator LoadCoopLocal() {
        
        yield return StartCoroutine(ReturnAllObjectsToPool());

        this.seed = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;;
        Random.InitState(this.seed);
        seedInUse = this.seed;

        InitializeMine();

        coopMineLoaded = true;

        try {
            StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems(gameObject));
        } catch {
        }
    }

    private IEnumerator AsyncLoadData(GameData data) {

        bool currentCloudLoadState = cloudLoading;

        // RETURN ALL MATERIALS AND TILEMAPS TO OBJECT POOL
        yield return StartCoroutine(ReturnAllObjectsToPool());
        
        // this.materials = array of game objects for the materials
        // data.materials = dictionary of MaterialManager values at string keys, where the strings are the ids
        this.seed = data.seed;
        Random.InitState(this.seed);
        seedInUse = this.seed;

        // If mine is already initialized, then this is not a new game
        // This doesn't necessarily mean the player is new, just that a new mine is needed
        this.mineInitialization = data.mineInitialization;

        this.revealedTilemapsTileValues = data.revealedTilemapsTileValues;

        this.destroyedTilemapsTileValues = data.destroyedTilemapsTileValues;
        this.highestRow = data.highestRow;
        this.currentOresMined = data.currentOresMined;

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

        soloMineLoaded = true;
    }

    public void SaveData(ref GameData data) {
        if (notSinglePlayerScene) {
            return;
        }

        // if mine didn't load, then its probably because player quickly opened the game, then closed it before mine loaded
        if (!soloMineLoaded) {
            return;
        }

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

    public GameObject GetTilemapObject()
    {
        obj = mineTilemaps[0];
        mineTilemaps.RemoveAt(0);

        return obj;
    }

    public void ReturnTilemapObject(GameObject obj, int yChunk, int xChunk)
    {
        // Get the Tilemap component from the GameObject
        tilemapToReturn = obj.GetComponent<Tilemap>();

        int positionsCount = tilemapToReturn.cellBounds.size.x * tilemapToReturn.cellBounds.size.y;;

        int tileIndex = 0;
        Vector3Int[] tilesForReturning = new Vector3Int[positionsCount];
        TileBase[] tilesBeingUsed = new TileBase[positionsCount];

        // Loop through all positions in the tilemap's bounds
        foreach (var position in tilemapToReturn.cellBounds.allPositionsWithin)
        {
            tilesForReturning[tileIndex] = position;
            tilesBeingUsed[tileIndex] = null;

            tileIndex++;
        }

        tilemapToReturn.SetTiles(tilesToSet, tilesBeingUsed);
        mineTilemaps.Insert(0, obj);
    }

    public IEnumerator ReturnAllObjectsToPool() {

        if (alreadyBeingReturned) {
            // In case this gets called multiple times at once (happens upon reopening game while mine is resetting)
            yield return new WaitUntil(() => !alreadyBeingReturned);
            yield break;
        } else {
            alreadyBeingReturned = true;
        }

        // Reset the mine        
        int counter = 0;

        // Split the mine reset work into intervals
        for (int i = 0; i < transform.childCount; i++)
        {
            child = transform.GetChild(i).gameObject;

            // Skip null objects
            if (!child)
                continue;

            childName = child.name;

            // If a tilemap row, row generation trigger, or GenerationTriggers parent, or mine background tilemap
            if ((childName.Contains("Row") || childName.Contains("Generation")) && child.activeSelf)
            {
                // Repool or destroy
                if (childName.Contains("Row")) {
                    // Define a regex to capture Y and X values
                    var match = Regex.Match(childName, @"Column (\d+), Row (\d+)");

                    y = int.Parse(match.Groups[1].Value);
                    x = int.Parse(match.Groups[2].Value);

                    ReturnTilemapObject(child, x * 25, y * -12 - 5);

                } else {

                    Destroy(child);
                    i--;
                }

                // Only delete 84 background tilemap, main tilemap row or random stuff per 0.1s
                if (counter >= 84) {
                    yield return new WaitForSecondsRealtime(0.1f);
                    counter = 0;
                }
                counter++;
            }
        }
        alreadyBeingReturned = false;
    }

    public void SetVisionRadius(int newRadius) {
        visionRadius = newRadius;
    }

    public int GetVisionRadius() {
        return visionRadius;
    }

    public Vector3 FindBestMiningPosition(int minRadius, int maxRadius, Vector2Int currentPosition, float currentRotation, int drillTier)
    {
        // Find all ore tiles within the search area
        List<Vector2Int> oreTiles = FindOreTilesInRange(currentPosition, currentRotation, minRadius, maxRadius, drillTier);
        
        // If no ore tiles found
        if (oreTiles.Count == 0) {
            return new(0, -6);
        }
            
        // Find all connected veins from the ore tiles
        List<List<Vector2Int>> veins = FindConnectedVeins(oreTiles);
        
        // If no veins found
        if (veins.Count == 0) {
            return new(0, -6);
        }
            
        // Find the largest vein
        List<Vector2Int> largestVein = FindLargestVein(veins);
        
        Vector2Int position = CalculateBestMiningPosition(largestVein, currentRotation);
        // Calculate the best mining position based on the largest vein
        return new(position.x, position.y);
    }

    private const float SEARCH_ANGLE = 60f;

    private List<Vector2Int> FindOreTilesInRange(Vector2Int currentPosition, float currentRotation, int minRadius, int maxRadius, int drillTier)
    {
        List<Vector2Int> oreTiles = new List<Vector2Int>();

        // Convert rotation to radians and calculate the angular range
        float rotationRad = (currentRotation - 90) * Mathf.Deg2Rad;
        float minAngle = rotationRad - SEARCH_ANGLE * Mathf.Deg2Rad;
        float maxAngle = rotationRad + SEARCH_ANGLE * Mathf.Deg2Rad;
        
        // Search all tiles within the max radius
        for (int x = currentPosition.x - maxRadius; x <= currentPosition.x + maxRadius; x++)
        {
            for (int y = currentPosition.y - maxRadius; y <= currentPosition.y + maxRadius; y++)
            {
                // Map starts below this
                if (y > -6) {
                    continue;
                }

                Vector2Int tilePos = new Vector2Int(x, y);
                Vector2Int relativePos = currentPosition - tilePos;
                
                // Calculate distance from current position
                float distance = relativePos.magnitude;
                
                // Skip if outside the radius bounds
                if (distance < minRadius || distance > maxRadius)
                    continue;        

                // Calculate angle to this tile
                float angle = Mathf.Atan2(relativePos.y, relativePos.x);
                
                // Normalize angle to [0, 2π] for proper comparison
                while (angle < 0) angle += 2 * Mathf.PI;
                while (minAngle < 0) minAngle += 2 * Mathf.PI;
                while (maxAngle < 0) maxAngle += 2 * Mathf.PI;
                
                // Handle angle wrap-around
                bool inAngleRange;
                if (minAngle > maxAngle) // Crossing 0/360 degrees
                {
                    inAngleRange = angle >= minAngle || angle <= maxAngle;
                }
                else
                {
                    inAngleRange = angle >= minAngle && angle <= maxAngle;
                }

                if (Mathf.DeltaAngle(angle * Mathf.Rad2Deg, currentRotation * Mathf.Rad2Deg) < 20) {
                    inAngleRange = false;
                }
                
                // Skip if not in angular range
                if (!inAngleRange) {
                    continue;
                }

                Vector2Int thisTilemapPos = CalculateTileMapPos(tilePos);
                
                // Check if this tile has ore
                if (unplacedTilemapsTileValues[thisTilemapPos.x, thisTilemapPos.y].TryGetValue(tilePos, out int value) && oreDelegation.VerifyIfOre(value))
                {
                    oreTiles.Add(tilePos);
                }
            }
        }
        
        return oreTiles;
    }

    private List<List<Vector2Int>> FindConnectedVeins(List<Vector2Int> oreTiles)
    {
        List<List<Vector2Int>> veins = new();
        HashSet<Vector2Int> visitedTiles = new();
        
        foreach (Vector2Int oreTile in oreTiles)
        {
            // Skip if this tile has already been processed
            if (visitedTiles.Contains(oreTile))
                continue;
                
            // Start a new vein
            List<Vector2Int> currentVein = new();
            Queue<Vector2Int> tilesToProcess = new();
            
            tilesToProcess.Enqueue(oreTile);
            visitedTiles.Add(oreTile);
            
            // Process all connected tiles
            while (tilesToProcess.Count > 0)
            {
                Vector2Int currentTile = tilesToProcess.Dequeue();
                currentVein.Add(currentTile);
                
                // Check all adjacent tiles (4-way connectivity currently, no need to check diagonal)
                Vector2Int[] adjacentOffsets = new Vector2Int[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1)
                };
                
                foreach (Vector2Int offset in adjacentOffsets)
                {
                    Vector2Int adjacentTile = currentTile + offset;
                    
                    // Skip if already visited
                    if (visitedTiles.Contains(adjacentTile))
                        continue;
                        
                    // Check if this adjacent tile is in our list of ore tiles
                    if (oreTiles.Contains(adjacentTile))
                    {
                        tilesToProcess.Enqueue(adjacentTile);
                        visitedTiles.Add(adjacentTile);
                    }
                }
            }
            
            // Add this vein to our list of veins
            veins.Add(currentVein);
        }
        
        return veins;
    }

    private List<Vector2Int> FindLargestVein(List<List<Vector2Int>> veins)
    {
        int largestSize = 0;
        List<Vector2Int> largestVein = new List<Vector2Int>();
        
        foreach (List<Vector2Int> vein in veins)
        {
            if (vein.Count > largestSize)
            {
                largestSize = vein.Count;
                largestVein = vein;
            }
        }
        
        return largestVein;
    }

    private Vector2Int CalculateBestMiningPosition(List<Vector2Int> vein, float currentRotation)
    {
        // If the vein is just one tile, return it
        if (vein.Count == 1)
            return vein[0];
            
        // For a straight-line mining approach, we need to find the best orientation
        // that intersects with as many ore tiles as possible
        
        // Convert rotation to a direction vector
        float rotationRad = currentRotation * Mathf.Deg2Rad;
        Vector2 directionVector = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad));
        
        // Get the perpendicular direction (for line sweeping)
        Vector2 perpendicularVector = new Vector2(-directionVector.y, directionVector.x);
        
        // Calculate all possible line paths through the vein
        Dictionary<float, List<Vector2Int>> linePaths = new Dictionary<float, List<Vector2Int>>();
        
        foreach (Vector2Int oreTile in vein)
        {
            // Project each tile onto the perpendicular line
            float projection = Vector2.Dot(new Vector2(oreTile.x, oreTile.y), perpendicularVector);
            
            // Round to nearest integer to group nearby tiles on the same line
            float roundedProjection = Mathf.Round(projection);
            
            if (!linePaths.ContainsKey(roundedProjection))
            {
                linePaths[roundedProjection] = new List<Vector2Int>();
            }
            
            linePaths[roundedProjection].Add(oreTile);
        }
        
        // Find the line with the most ore tiles
        float bestLine = 0;
        int maxOreCount = 0;
        
        foreach (var path in linePaths)
        {
            if (path.Value.Count > maxOreCount)
            {
                maxOreCount = path.Value.Count;
                bestLine = path.Key;
            }
        }
        
        // From the tiles on this best line, find the one closest to the player's direction
        List<Vector2Int> bestLineTiles = linePaths[bestLine];
        
        // Calculate the center of the best line tiles
        Vector2 center = Vector2.zero;
        foreach (Vector2Int tile in bestLineTiles)
        {
            center += new Vector2(tile.x, tile.y);
        }
        center /= bestLineTiles.Count;
        
        // The best mining position is approximately at the center of the vein's best line
        return new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
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

    public int GetTotalRows() {
        return totalRows;
    }

}