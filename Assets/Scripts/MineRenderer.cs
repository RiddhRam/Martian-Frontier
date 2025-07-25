using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MineRenderer : MonoBehaviour, IDataPersistence
{
    private static MineRenderer _instance;
    public static MineRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<MineRenderer>();
            }
            return _instance;
        }
    }

    // Have to change through hierarchy not through here
    [SerializeField] private int visionRadius;
    public GameObject largeFogOfWar;
    public GameObject generationTriggers;
    public GameObject mineTilemapPrefab;  // Reference to the Tilemap component
    public TileBase mineBackgroundRuleTile;
    public TileBase unknownTile;

    // These are used to reveal which tile is at a position, includes base rock tile, and ores
    public TileBase[] tileValues;
    // The colour of the particles to show when an ore is destroyed
    public Color[] tileColours;

    // Height of the map, measured in tilemaps
    private const int totalRows = 42;
    // Width of the map, measured in tilemaps, calculated by using gridSize and mapHalfLength
    private int totalColumns;
    // Half the width of the map, measured in tiles
    private const int mapHalfLength = 75;
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
    // Used for manual frustum culling
    public List<TilemapRenderer> tilemapRenderers = new();
    // Array of the tilemap Game objects, same as above, but in a 2d array rather than a dictionary with the string as the key
    public Tilemap[,] tilemaps;

    // Uppercase names
    public string[] selectedMaterialNames;

    [SerializeField] private int seed;
    public int highestRow = 0;

    // 0 = Not created
    // 1 = in the process of initializing
    // 2 = initialized
    public int mineInitialization = 0;
    public int mineCount;

    public HashSet<int> discoveredOres = new();

    // Indicates the index of new tiers in tileValues
    public int[] tierThresholds = new int[3];
    public int[] oresPerTier = new int[3];

    [Header("Scripts")]
    public OreDelegation oreDelegation;
    public UpgradesDelegator upgradesDelegator;
    public RefineryController refineryController;

    [Header("Cameras")]
    // For culling
    public List<Camera> cameras;

    private Dictionary<string, int> quantities = new();

    // Used to find the count of each ore in a mine
    // search and uncomment everything related to "oresCount"
    //public int[] oresCount;
    
    [SerializeField] ParticleSystem vacuumPrefab;
    [SerializeField] AudioClip orePickUpAudioClip;
    [SerializeField] AudioSource orePickUpSequenceAudioSource;
    private float lastOreMinedTime;

    [Header("Cache")]
    private readonly Queue<ParticleSystem> particlePool = new();
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

    public bool soloMineLoaded = false;
    int seedInUse;

    private NPCMovement causeOfWhirrAudio;

    // Not related to seed, only used for choosing ores for this mine, and drone mining positions
    System.Random rng;

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

        // Used for naming
        int columnCount = 2;
        int rowCount = -1;
        int mapCount = -1;
        
        // unplacedTilemapsTileValues will be populated as each row is created
        // These ones are done right now
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++)
        {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++)
            {
                destroyedTilemapsTileValues[i, j] = new();
                revealedTilemapsTileValues[i, j] = new();

                GameObject mineTilemapGameObject = Instantiate(mineTilemapPrefab);

                mapCount++;
                // 6 tilemaps per row
                if (mapCount % (mapHalfLength / gridSize.x * 2) == 0)
                {
                    // We reached the next row
                    columnCount = 0;
                    rowCount++;
                }
                else
                {
                    // Same row next column
                    columnCount++;
                }

                mineTilemapGameObject.transform.SetParent(transform);
                mineTilemapGameObject.name = "Column " + (totalColumns - columnCount) + ", Row " + (totalRows - rowCount);

                ReturnTilemapObject(mineTilemapGameObject, i * gridSize.x, j * -gridSize.y - 5);

                // Get the component once, then no need to do it again later
                Tilemap mineTilemap = mineTilemapGameObject.GetComponent<Tilemap>();
                tilemapsDictionary.Add(mineTilemapGameObject.name, mineTilemap);
                tilemapRenderers.Add(mineTilemapGameObject.GetComponent<TilemapRenderer>());
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
                sum += oresPerTier[i];
                break;
            }
            oresPerTier[i] = tierThresholds[i+1] - tierThresholds[i] - 1;
            sum += oresPerTier[i];
        }

        //oresCount = new int[sum];

        // Create particle object pool
        for (int i = 0; i != 40; i++) {
            var ps = Instantiate(vacuumPrefab, new(), Quaternion.identity);

            ps.gameObject.SetActive(false);
            particlePool.Enqueue(ps);
        }
    }
    
    // Cull tilemaps that are not in any active camera view
    void Update()
    {
        foreach (var camera in cameras)
        {
            if (!camera.isActiveAndEnabled)
            {
                continue;
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var tm in tilemapRenderers)
            {
                bool visible = GeometryUtility.TestPlanesAABB(planes, tm.bounds);

                if (tm.enabled != visible)
                    tm.enabled = visible;
            }
        }
    }

    // Called when game first loads, and the RefineryController calls this when it's battery reaches 0
    public void InitializeMine()
    {

        // If mineInitialization == 1 then the user already saw the first few blocks before they left the game
        // Don't make a new seed, just use the last one
        if (mineInitialization < 2)
        {
            // My birthday: Dec 8
            System.DateTime epoch = new System.DateTime(2024, 12, 8, 0, 0, 0, System.DateTimeKind.Utc);

            // If this is the first drone, use this specific seed, so there's a smooth start
            /*if (!RefineryUpgradePad.Instance.BoughtTenUpgrades())
            {
                // Limestone close to the surface
                seed = ;
            }
            else
            {
                seed = (int)(System.DateTime.UtcNow - epoch).TotalSeconds;
            }*/

            seed = (int)(System.DateTime.UtcNow - epoch).TotalSeconds;

            Random.InitState(seed);
            seedInUse = seed;
        }

        // Clear all dictionaries in reveal and destroyed array
        // unplacedTilemapsTileValues will be populated as each row is created
        for (int i = 0; i != unplacedTilemapsTileValues.GetLength(0); i++)
        {
            for (int j = 0; j != unplacedTilemapsTileValues.GetLength(1); j++)
            {

                // Try to avoid using new() to keep memory usage down
                if (destroyedTilemapsTileValues[i, j] == null)
                {
                    destroyedTilemapsTileValues[i, j] = new();
                    revealedTilemapsTileValues[i, j] = new();
                }
                else
                {
                    destroyedTilemapsTileValues[i, j].Clear();
                    revealedTilemapsTileValues[i, j].Clear();
                }
            }
        }

        /*for (int i = 0; i != oresCount.Length; i++)
        {
            oresCount[i] = 0;
        }*/

        CreateGenTriggers();
        // Change limit to 5 to create first 4 rows
        // Change limit to totalRows + 1 to create entire map
        for (int i = 1; i != 5; i++)
        {
            CreateTiles(i);
        }

        // Uncomment this too to log the quantity of each ores, copy pastable into excel
        /*string output = oresCount[0].ToString();
        for (int i = 1; i != oresCount.Length; i++)
        {
            output += "\n" + oresCount[i].ToString();
        }
        Debug.Log(output);*/

        // Reveal the entry blocks, by calling destroy the tiles above the first few surface blocks
        // Even though there's no tiles here, it uses to vision radius to reveal other tiles around it
        // This is better than calling RevealTiles it doesn't just reveal the first few surface blocks
        DestroyTiles(initializeTiles, true);
        if (notSinglePlayerScene)
        {
            // Not an npc, and is loading, but if you change it to true, false, then the surrounding tiles are not revealed
            DestroyTiles(coopInitializeTiles, false);
        }

        mineInitialization = 2;
        SaveGame();

        AnalyticsDelegator.Instance.InitializeMine(highestRow);
    }

    // Places tiles in a 25x12 rectangle, starting from (-mapHalfLength, -5) and going to the right and downward
    public void CreateTiles(int chunkRow, bool setHighestRow = true)
    {
        // If countdown hasn't started, then start it (player teleported inside and bypassed the normal entrance)
        if (refineryController.countdownCoroutine == null && refineryController.doneLoading && chunkRow >= 5)
        {
            refineryController.StartRefineryCountdown();
        }

        chunkRow = System.Math.Clamp(chunkRow, 1, generatedRows.Length);

        try
        {
            Destroy(GameObject.Find("Generate Row (" + (chunkRow) + ")"));
        }
        catch
        {
        }

        for (int i = 1; i < chunkRow; i++) {

            // Verify previous tiles were created
            if (!generatedRows[i])
            {
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
            string tileMapName = GetTilemapObject().name;
            mineTilemap = tilemapsDictionary[tileMapName];
            
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

            mineTilemap.CompressBounds();

            unplacedTilemapsTileValues[chunkColumn-1, chunkRow-1] = unplacedTilemapsTileValue;
            tilemaps[chunkColumn-1, chunkRow-1] = mineTilemap;

            chunkColumn++;
        }

        generatedRows[chunkRow - 1] = true;
    }

    public void TriggerAllGenerationTriggersAbove(float y)
    {
        // Generate all rows up to that position if not already generated (will cause a lag spike)

        // Offset to be 4 tilemaps lower because the generation triggers are offset by a little bit too
        int tilemapY = (int)y - (4 * gridSize.y);
        Vector2Int tilemapPos = CalculateTileMapPos(new(0, tilemapY));

        CreateTiles(tilemapPos.y + 1);
    }

    public void MoveFogOfWar(int rowLoaded)
    {
        // If the last row, send it very far down where it won't be seen at the edge of the map
        if (rowLoaded == totalRows || genTrigTransform.childCount == 1)
        {
            largeFogOfWar.transform.position = new Vector3(0, -3000, 0);
            return;
        }

        // If not last row, just move it down
        largeFogOfWar.transform.position = new Vector3(0, -220 - ((rowLoaded + 1) * gridSize.y), 0);
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
        //veinCount = Random.Range(1, 3);
        /*if (!RefineryUpgradePad.Instance.BoughtTenUpgrades()) // If this level is still new
        {
            //veinCount = maxVeinCount + 1; // More veins
        }*/

        for (int v = 0; v < veinCount; v++)
        {
            // Randomly choose the center position for each vein within the chunk
            centerX = Random.Range(0, gridSize.x);
            centerY = Random.Range(0, gridSize.y);
            radius = Random.Range(minVeinRadius, maxVeinRadius); // Radius of 2-4 tiles for variation
            //radius = Random.Range(2, 2);
            /*if (!RefineryUpgradePad.Instance.BoughtTenUpgrades())
            {
                //radius = maxVeinRadius - 1; // Slightly less than max
            }*/

            // Select an ore based on the depth (chunkRow) to increase the chances of higher-value ores
            oreToPlace = SelectOreBasedOnDepth(chunkRow, level);

            // In order to see quantity of each ore in the mine
            // Uncomment this, and in initialize mine generate entire map by change the for loop where it only generates first few rows
            // and also search and uncomment everything related to "oresCount"

            /*int oreIndex = 0;
            for (int i = 0; i != tileValues.Length; i++)
            {
                isBaseTile = false;

                for (int j = 0; j != tierThresholds.Length; j++)
                {
                    if (tierThresholds[j] == i)
                    {
                        isBaseTile = true;
                        break;
                    }
                }

                if (isBaseTile)
                {
                    continue;
                }

                if (oreToPlace == i)
                {
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
                    if (distanceFromCenter > radius)
                    {
                        continue;
                    }

                    tileX = centerX + x;
                    tileY = centerY + y;

                    // Ensure we stay within grid bounds
                    if (tileX < 0 || tileX >= gridSize.x || tileY < 0 || tileY >= gridSize.y)
                    {
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
        // Tutorial level
        if (mineCount == 1) {
            // Return limestone only
            if (level == 0)
            {
                return 1;
            }

            // Return anything higher than tier 1, so the AI doesn't get mixed up when close to level 2 ores
            return 4;
        }   
            
        // Define the ore range for this tier
        minOreIndex = tierThresholds[level] + 1;
        maxOreIndex = tierThresholds[level] + oresPerTier[level];
        oreCount = maxOreIndex - minOreIndex + 1;

        // Calculate the probability weights based on depth
        depthFactor = Mathf.Clamp01((chunkRow - 14 * level - 1) / 13f);  // Lower 13f to make the rarity change faster, increase to change it slower
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

    public void DestroyTiles(List<Vector2Int> tilesToDestroy, bool loading, Vector3 vehiclePos = new(), bool playAudio = false, NPCMovement nPCMovement = null) {

        oresMined = 0;

        destroyTilemapsToEdit.Clear();
        destroyTilesForTilemaps.Clear();
        tilesToReveal.Clear();
        revealTilemapsToEdit.Clear();
        revealTilesForTilemaps.Clear();

        bool generateParticles = false;
        // If its not Vector3.zero, then it was initialized because there needs to be particles generated
        if (Vector3.Distance(vehiclePos, new()) > 0.1f) {
            generateParticles = true;
        }

        foreach (Vector3Int tileToDestroy in tilesToDestroy.Select(v => (Vector3Int)v))
        {
            tilemapPos = CalculateTileMapPos(new(tileToDestroy.x, tileToDestroy.y));

            tilemap = tilemaps[tilemapPos.x, tilemapPos.y];

            if (!destroyTilemapsToEdit.Contains(tilemap))
            {
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
            try
            {
                unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
                revealedTilemapsTileValues[tilemapPos.x, tilemapPos.y].Remove(new(tileToDestroy.x, tileToDestroy.y));
            }
            catch
            {
            }

            // Destroy the tile by setting to null and saving it
            destroyedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(tileToDestroy.x, tileToDestroy.y)] = tileValue;

            // If the mine is being loaded from a save, don't reveal tiles, unless the top row
            if (loading && tileToDestroy.y != -4)
            {
                continue;
            }

            int visionBoost = 0;
            if (upgradesDelegator)
            {
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
            if (tileToDestroy.y == -4 || loading)
            {
                continue;
            }

            tileMined = tilemap.GetTile(tileToDestroy);

            // Get tile index among the array of all tile values (including non ores)
            identifiedTile = GetTileIndex(tileMined);
            oreMined = true;

            if (!oreDelegation.VerifyIfOre(identifiedTile))
            {
                oreMined = false;
            }
            else
            {
                if (generateParticles)
                {
                    SpawnVacuum(tileToDestroy, vehiclePos, tileColours[identifiedTile]);
                }
            }

            // Actually current blocks mined, not ores
            currentOresMined++;

            if (!oreMined)
            {
                continue;
            }

            oresMined++;

            // Get adjustment needed to make sure we have the ore index, not the tile index (ignore any non ore tiles when getting index)
            int adjustment = 0;

            for (int i = 0; i != tierThresholds.Length; i++)
            {
                if (identifiedTile > tierThresholds[i])
                {
                    adjustment++;
                }
            }

            int adjustedTileIndex = identifiedTile - adjustment;

            if (!quantities.ContainsKey(selectedMaterialNames[adjustedTileIndex]))
            {
                quantities[selectedMaterialNames[adjustedTileIndex]] = 1;
                
                // Determine which ores have been discovered so far
                // If less than 9, then we need to determine which ones have been found, since not all were found yet
                // Only add the ones that were found in the frame, if they were not previously found
                if (discoveredOres.Count < 9 && !discoveredOres.Contains(adjustedTileIndex))
                {
                    discoveredOres.Add(adjustedTileIndex);
                }
            }
            else
            {

                quantities[selectedMaterialNames[identifiedTile - adjustment]]++;
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

        // If not loading
        if (!loading) {
            playerStateScript.NewBlockMined(oresMined, tilesToDestroy.Count);
            DailyChallengeDelegator.Instance.MinedOres(quantities);

            if (oresMined > 0 && playAudio)
            {
                if (causeOfWhirrAudio == null)
                {
                    causeOfWhirrAudio = nPCMovement;
                }

                if (causeOfWhirrAudio == nPCMovement)
                {
                    float timeSinceLastMine = Time.time - lastOreMinedTime;
                    const float volume = 0.5f;
                    
                    if (timeSinceLastMine >= 0.4f)
                    {
                        // Play ore pick up audio, or add to the timer
                        StartCoroutine(AudioDelegator.Instance.PlayTimedAudio(orePickUpSequenceAudioSource, orePickUpAudioClip, volume, false));

                        lastOreMinedTime = Time.time;
                    }
                    // If audio is fading out or faded out already, then play from the start
                    else if (AudioDelegator.Instance.audioTimer <= 0f)
                    {
                        // Play ore pick up audio from the start
                        StartCoroutine(AudioDelegator.Instance.PlayTimedAudio(orePickUpSequenceAudioSource, orePickUpAudioClip, volume, true));

                        lastOreMinedTime = Time.time;
                    }
                }

                if (AudioDelegator.Instance.audioTimer <= 0f)
                {
                    causeOfWhirrAudio = null;
                }

                // Track which ores are being sold so the player can get paid
                int[] newMaterials = new int[9];
                foreach (string oreName in quantities.Keys)
                {
                    newMaterials[GetTileIndexByName(oreName)] = quantities[oreName];
                }

                // Finally pay player
                if (nPCMovement)
                {
                    nPCMovement.NewOreMined(refineryController.SellOres(newMaterials));
                }
            }
        }

        quantities.Clear();

        // Reveal the tiles
        RevealTiles(tilesToReveal);
    }

    public void SpawnVacuum(Vector3 from, Vector3 to, Color colour)
    {
        // Instantiate at source
        var ps = GetParticleFromPool();

        // No particle was available
        if (ps == null) {
            return;
        }

        ps.transform.position = from;
        ps.gameObject.SetActive(true);

        // Tint
        var main = ps.main;
        main.startColor = colour;

        // Compute direction and force so particles reach target ~75‑90 % of lifetime
        var dir = (to - from).normalized;
        var distance = Vector3.Distance(from, to);
        var forceMag = 2f * distance / main.startLifetime.constant;

        var fol = ps.forceOverLifetime;
        fol.enabled = true;
        fol.x = dir.x * forceMag;
        fol.y = dir.y * forceMag;
        fol.z = dir.z * forceMag;

        // orient swirl around travel axis
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(6f);

        ps.Play();

        StartCoroutine(ReturnParticleToPool(ps));
    }

    private ParticleSystem GetParticleFromPool() {
        if (particlePool.Count > 0) {
            return particlePool.Dequeue();
        }
        return null;
    }

    private IEnumerator ReturnParticleToPool(ParticleSystem particle) {
        yield return new WaitForSeconds(particle.main.startLifetime.constant);

        particlePool.Enqueue(particle);
        particle.gameObject.SetActive(false);
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
            tier1OreNames[i] = selectedMaterialNames[i];
        }

        return tier1OreNames;
    }

    public string[] GetTier2OreNames() {
        string[] tier2OreNames = new string[oresPerTier[1]];

        for (int i = 0; i != oresPerTier[1]; i++) {
            tier2OreNames[i] = selectedMaterialNames[oresPerTier[0] + i];
        }

        return tier2OreNames;
    }

    public string[] GetTier3OreNames() {
        string[] tier3OreNames = new string[oresPerTier[2]];

        for (int i = 0; i != oresPerTier[2]; i++) {
            tier3OreNames[i] = selectedMaterialNames[oresPerTier[0] + oresPerTier[1] + i];
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

    public int GetTileIndexByName(string oreName) {
        for (int i = 0; i != selectedMaterialNames.Length; i++) {
            if (selectedMaterialNames[i] == oreName) {
                return i;
            }
        }

        // Shouldnt reach here
        return 0;
    }

    public void LoadData(GameData data)
    {
        // MINE IS INITIALIZED IN REFINERY CONTROLLER
        bool currentCloudLoadState = cloudLoading;

        this.currentOresMined = data.currentOresMined;
        this.mineCount = data.mineCount;

        this.discoveredOres = data.discoveredOres;

        // Choose a random factor to multiply by, so adjacent levels aren't too similar
        int multiplicationFactor = new System.Random(this.mineCount).Next(0, 31);

        // Build a list of all original indices (0-14)
        List<int> indices = Enumerable.Range(0, oreDelegation.materialNames.Length).ToList();

        // Shuffle deterministically with the seed
        rng = new(multiplicationFactor * this.mineCount);
        for (int i = 0; i != indices.Count; i++)
        {
            int j = rng.Next(i, indices.Count);          // inclusive-exclusive
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Take the first 9 indices as the chosen ones
        List<int> chosen = indices.Take(oreDelegation.GetOriginalMaterialPrices().Length).ToList();

        // If its the first mine, use the basic ores
        if (mineCount == 1)
        {
            chosen = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        }

        // Set selected material names
        selectedMaterialNames = new string[chosen.Count];
        for (int i = 0; i != chosen.Count; i++)
        {
            selectedMaterialNames[i] = oreDelegation.materialNames[chosen[i]];
        }

        // Set tilebase values and colours
        int oreIndex = 0;
        for (int i = 0; i != tileValues.Length; i++)
        {
            // if its an ore, over write it with the selected material
            if (oreDelegation.VerifyIfOre(i))
            {
                int index = chosen[oreIndex];
                tileValues[i] = oreDelegation.oreTileValues[index];
                tileColours[i] = oreDelegation.oreTileColours[index];
                oreIndex++;
            }
        }

        // Initialize everything else
        RefineryUpgradePad.Instance.oreUpgrades = data.oreUpgrades;
        RefineryUpgradePad.Instance.SetProceedPanelRequirement(this.mineCount);
        DailyChallengeDelegator.Instance.Initialize();
        
        if (currentCloudLoadState == cloudLoading)
        {
            cloudLoading = true;
            try
            {
                StartCoroutine(LoadingScreen.Instance.IncrementLoadedItems(gameObject));
            }
            catch
            {
            }
        }

        soloMineLoaded = true;
    }

    public void SaveData(ref GameData data)
    {
        if (notSinglePlayerScene)
        {
            return;
        }

        // if mine didn't load, then its probably because player quickly opened the game, then closed it before mine loaded
        if (!soloMineLoaded)
        {
            return;
        }

        data.currentOresMined = this.currentOresMined;
        data.mineCount = this.mineCount;

        data.discoveredOres = this.discoveredOres;

        data.oreUpgrades = RefineryUpgradePad.Instance.oreUpgrades;
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
        if (!DataPersistenceManager.Instance) {
            return;
        }

        DataPersistenceManager.Instance.SaveGame();
    }

    // Get tiles tier 
    public int GetTileTier(TileBase tileToIdentify)
    {
        tileTier = 1;

        for (int i = 0; i != tileValues.Length; i++)
        {
            // Find the right tile index
            if (tileToIdentify != tileValues[i])
            {
                continue;
            }

            for (int j = 0; j != tierThresholds.Length; j++)
            {
                if (tierThresholds[j] <= i)
                {
                    tileTier = j + 1;
                }
            }

            break;
        }

        return tileTier;
    }

    public int GetOreTierByIndex(int oreIndex)
    {
        int tier = 1;

        int oreCounter = 0;

        for (int i = 0; i != oresPerTier.Length; i++)
        {
            for (int j = 0; j != oresPerTier[i]; j++)
            {
                // Find the same ore index
                if (oreCounter != oreIndex)
                {
                    oreCounter++;
                }
                // If we found it, it's in this tier
                else
                {
                    return tier;
                }
            }

            // Check the next tier
            tier++;
        }

        // Shouldn't reach here
        return 1;
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

    public Vector3 FindBestMiningPosition(int minRadius, int maxRadius, Vector2Int currentPosition, float currentRotation, int drillTier, NPCMovement nPCMovement)
    {
        // If not initialized yet
        if (mineInitialization != 2)
        {
            return nPCMovement.GetRandomPosition();
        }
        // Find all ore tiles within the search area
        List<Vector2Int> oreTiles = FindOreTilesInRange(currentPosition, currentRotation, minRadius, maxRadius, drillTier);
        
        // If no ore tiles found
        if (oreTiles.Count == 0) {
            return nPCMovement.GetRandomPosition();
        }
            
        // Find all connected veins from the ore tiles
        List<List<Vector2Int>> veins = FindConnectedVeins(oreTiles);
        
        // If no veins found
        if (veins.Count == 0) {
            return nPCMovement.GetRandomPosition();
        }

        // Find the largest vein
        //List<Vector2Int> bestVein = FindLargestVein(veins);

        // Choose a random vein
        List<Vector2Int> bestVein = veins[rng.Next(veins.Count)];

        if (bestVein.Count == 0)
        {
            return nPCMovement.GetRandomPosition();
        }
        
        Vector2Int position = CalculateBestMiningPosition(bestVein, currentRotation);

        // Calculate the best mining position based on the selected vein
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
                if (y > -6)
                {
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
                /*float angle = Mathf.Atan2(relativePos.y, relativePos.x);

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

                if (Mathf.DeltaAngle(angle * Mathf.Rad2Deg, currentRotation * Mathf.Rad2Deg) < 20)
                {
                    inAngleRange = false;
                }

                // Skip if not in angular range
                if (!inAngleRange)
                {
                    continue;
                }*/

                Vector2Int thisTilemapPos = CalculateTileMapPos(tilePos);

                // Make sure this tile exist, matches the drill tier, and is an ore
                if (unplacedTilemapsTileValues[thisTilemapPos.x, thisTilemapPos.y].TryGetValue(tilePos, out int value) && oreDelegation.VerifyIfOre(value) && (GetTileTier(tileValues[value]) == drillTier))
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

    public int GetSeed() {
        return seed;
    }

    public int GetTotalRows() {
        return totalRows;
    }

}