using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NPCManager : MonoBehaviour, IDataPersistence
{
    [SerializeField]
    private GameObject npcPrefab;

    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private bool[] spawnPointTaken;
    [SerializeField]
    private TextMeshProUGUI[] spawnPointNameTexts;

    [SerializeField]
    private Sprite[] mapIcons;
    [SerializeField]
    public GameObject mapIconPrefab;
    [SerializeField]
    private Sprite drillerIcon;
    [SerializeField]
    private Sprite haulerIcon;

    [SerializeField]
    private Transform playerVehicle;
    private Vector3 playerSpawnPoint;

    [SerializeField]
    private GameObject[] npcs;
    private NavMeshAgent[] navMeshAgents;
    private Vector3[] npcSpawnPoints;
    private NPCMovement[] nPCMovements;
    private string[] nPCNames;
    [SerializeField]
    private bool[] npcIsHauler;
    // How long the npcs play for
    [SerializeField]
    private int[] nPCTimeRemaining;
    [SerializeField]
    private int nPCEmptyTimer = 0;
    private readonly Color[] spawnColours = {new(246/255f, 4/255f, 3/255f), new(57/255f, 255/255f, 21/255f), new(2/255f, 191/255f, 255f/255f), new(255f/255, 166/255f, 2/255f)};
    // Helps prevent race conditions with NPCMovement and LiveManageSession()
    private bool[] transitioningVehicle;

    // How many npc haulers are active
    private int haulers = 0;
    private int highestDrillTier = 1;
    private int playerRebirths = 0;
    private int npcCount;
    public bool drillingNeeded = false;

    // The minimum value in the random number generator that determines whether or not NPC should spawn
    // Setting greater than 0 gauarantees NPC will spawn
    private int minNPCRNG = 0;

    public MineRenderer mineRenderer;
    [SerializeField]
    private UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    [SerializeField]
    private PlayerState playerState;
    [SerializeField]
    private GarageDelegator garageDelegator;
    [SerializeField]
    private GameObject lostInternetScreen;
    [SerializeField]
    private NavMeshAgent testAgent;
    [SerializeField]
    private MapRecordingMode mapRecordingMode;
    [SerializeField]
    private Button toggleCamera;

    private readonly int sessionUpdateTimer = 4;
    private readonly int[] drillerTierThresholds = {5, 11, 17};
    private readonly int[] haulerThresholds = {9, 13, 19};
    private readonly string[] botNames = {
        "Crimson", "Rusty", "Lunar", "Solar", "Astro", "Quantum", 
        "Nova", "Phantom", "Obsidian", "Cobalt", "Plasma", "Ironclad",
        "Zephyr", "Void", "Gritty", "Vortex", "Redshift", "Orbital",
        "Radiant", "Pyro", "Blazing", "Silent", "Nebula", "Electric",
        "Shadow", "Frozen", "Glitchy", "Titan", "Infernal", "Chrome",
        "Echo", "Warped", "Venomous", "Hazard", "Stellar", "Jaded",
        "Atomic", "Grim", "Pixelated", "Blistered", "Cyber", "Fractal",
        "Miner", "Driller", "Hauler", "Rover", "Seeker", 
        "Pioneer", "Scout", "Prospector", "Nomad", "Quaker", "Astronaut",
        "Digger", "Crawler", "Core", "Reactor", "Golem", "Harvester",
        "Excavator", "Sentinel", "Warden", "Breaker", "Scraper", "Forager",
        "Smelter", "Raider", "Assembler", "Tracer", "Forgemaster", "Grinder",
        "Operator", "Runner", "Chiseler", "Refiner", "Surveyor", "Plunderer",
        "Reclaimer", "Destructor", "Engineer", "Drifter", "Observer", "Stalker",
        "M1ner", "D1gg3r", "R0verX", "B00t404", "Dr1ll3r", "Nom@d", 
        "Qw4ker", "H4ulr", "S3eker", "Gr1nder", "Ph4ntom", "Xpl0rer",
        "AstroN0m", "Cyb3rBot", "Obsid1an", "R3dsh1ft", "C0reBrkr", "Fr4ct4l",
        "H@rv3ster", "D1v3r", "R0gueX", "W4rpdriv3", "G1itch", "V01dX",
        "Tr4cer77", "Bl1zz4rd", "Chrom3X", "Re4ct0r9", "V0rt3x99", "P1x3l", 
        "Ember", "Tectonic", "Pulsar", "Nebulark", "Solaris", "Quasar",
        "Turbine", "Maelstrom", "Flux", "Oblivion", "Rift", "Singularity",
        "Eclipse", "Pyronova", "Thunderstrike", "Sentient", "Overdrive", "Chrono",
        "Blitz", "Warpcore", "Circuit", "Voltage", "Nanite", "Zenith",
        "Helion", "Omicron", "Catalyst", "Dynamo", "Onyx", "Phazer",
        "Reverberate", "Cryo", "Mecha", "Spectron", "Monolith", "Ether",
        "Gyrator", "Vanguard", "Titanium", "Mach", "Overseer", "Resonance",
        "Serrator", "Pulsewave", "Sonic", "Forgecore", "Amplify", "Kinetix",
        "Neutron", "Plasmonic", "Metron", "Ionic", "Havoc", "Zenon",
        "Stratos", "Hyperion", "Synthetix", "Photon", "Spectra", "Fusion",
        "Aegis", "Kryptron", "Shredder", "OblivionX", "Syphon", "Hydron",
        "Nexon", "Xenotron", "Etherion", "Velocitron", "Vortexus", "Catalyx",
        "Synthar", "Axion", "Dimensia", "Polaron", "HorizonX", "Ruptor",
        "Exotron", "Silicore", "Nocturn", "Halcyon", "Excalibot", "Typhon",
        "EchoCore", "NeonEdge", "ChronoX", "Celestus", "Pyrevolt", "Stormdrift",
        "Dreadnought", "Evolvion", "Voltar", "Strikron", "Roguewave", "Cipheron",
        "Glacion", "HyperCore", "Tesseract", "Omniflare", "Infernix", "Mechara",
        "Nucleon", "Skybreaker", "Voidstorm", "Cyclonix", "Oblivix", "Fission",
        "T3rr@", "D3f1ler", "M3chX", "Cr@t3r", "Bl4st0ff", "F1ss10n", "Ast3r01d99",
        "T0rqu3", "Xen0nX", "Hydr0Ph4z3", "P3rmafrost", "L4v@fl0w", "N3bularX", 
        "Cyb3rn@ut", "Qu@ntum99", "R3nd3rX", "Z3r0P01nt", "Exc@v8r", "Thr@sh3r", 
        "T1m3warp", "S0n1cX", "W4rpg4te", "R@gnar0k", "Pyr0x", "Dr4g0nB0t", 
        "Bl@ckH0l3", "V3l0c1tr0n", "R4d10@ct1v3", "Synth3X", "Turb0C0r3", "H3llfir3",  
        "Gl4c14l", "Xpl01t3r", "M3ch@fl@re", "N1ghtSh4d3", "D3m0n1cX", "Puls4r99", 
        "C0sm0tr0n", "D4t@M1n3r", "T3kn0M4ncer", "X-t3rmin8r", "Crypt0X", "F1r3w@llX",  
        "Bl1tzkr13g", "Str4t0bl@st", "C3l3st14lX", "Ph0t0nDr1ft", "T0rment0r", "V0r@x",  
        "Ex0G3n", "Lun@rM3ch", "D3struct0rX", "H@voc99", "M0n0l1thX"

    };

    readonly System.Random random = new();

    // Cache
    HaulerController haulerController;
    Coroutine liveSessionCoroutine;
    private Camera mainCamera;
    private NavMeshPath path;

    void Awake()
    {
        mainCamera = Camera.main;

        if (mapRecordingMode.enabled) {
            minNPCRNG = 1;
        }
    }

    public void PlacePlayer() {
        int index = random.Next(0, spawnPoints.Length);

        playerSpawnPoint = spawnPoints[index].position;
        spawnPointTaken[index] = true;

        string name = PlayerPrefs.GetString("PlayerName");
        spawnPointNameTexts[index].text = name.Substring(0, name.Length - 5);

        SetMapIcon(playerVehicle.gameObject, playerSpawnPoint);

        ResetPlayerPos();
    }

    public void ResetPlayerPos() {
        playerVehicle.position = playerSpawnPoint;
        playerVehicle.eulerAngles = new(0, 0, 90);
        playerVehicle.GetChild(0).eulerAngles = new(0, 0, 270);
    }

    public void CreateNPC(int npcIndex, bool driller = true, bool newBot = true, int spawnSpecificIndex = -1) {
        int number = random.Next(minNPCRNG, 10);

        // 10% chance of player not spawning
        if (number < 1 && newBot) {
            return;
        }

        npcs[npcIndex] = Instantiate(npcPrefab);

        if (newBot) {
            nPCNames[npcIndex] = GenerateBotName();
            nPCTimeRemaining[npcIndex] = random.Next(300, 3600);
            npcSpawnPoints[npcIndex] = GetSpawnPoint();
        }

        System.Random seedRandom = new System.Random(nPCNames[npcIndex].GetHashCode());

        npcs[npcIndex].name = nPCNames[npcIndex] + " " + npcIndex;

        int spawnIndex = FindSpawnPointIndex(npcSpawnPoints[npcIndex]);
        nPCMovements[npcIndex] = npcs[npcIndex].GetComponent<NPCMovement>();
        nPCMovements[npcIndex].npcIndex = npcIndex;
        nPCMovements[npcIndex].nPCManager = this;
        nPCMovements[npcIndex].rebirthLevel = seedRandom.Next(playerRebirths - 2, playerRebirths + 5);
        nPCMovements[npcIndex].npcNameText.text = nPCNames[npcIndex];
        nPCMovements[npcIndex].npcNameText.color = spawnColours[spawnIndex];
        nPCMovements[npcIndex].worldSpaceCanvas.worldCamera = mainCamera;

        // Clamp to be higher than -1
        if (nPCMovements[npcIndex].rebirthLevel < 0) {
            nPCMovements[npcIndex].rebirthLevel = 0;
        }

        nPCMovements[npcIndex].rebirthText.text = nPCMovements[npcIndex].rebirthLevel.ToString();

        navMeshAgents[npcIndex] = nPCMovements[npcIndex].agent;

        spawnPointNameTexts[spawnIndex].text = nPCNames[npcIndex];

        GameObject vehicle;

        // Agent types and indexes: Humanoid (0), Width 3 (1), Width 4 (2), Width 5 (3), Driller (4)
        float speed;
        if (driller) {
            // Make sure player gets NPCs of the same drill tier
            int max = drillerTierThresholds[highestDrillTier - 1];
            int min = 0;

            if (highestDrillTier != 1) {
                min = drillerTierThresholds[highestDrillTier - 2];
            }

            int index;
            
            if (mapRecordingMode.enabled) {
                // Gaurantee Bore I spawns
                index = 10;
            } else {
                index = seedRandom.Next(min, max);
            }
            
            // If its a Specter, drop the index by one
            // Specters are buggy with AI Navigation, they keep getting stuck
            if (garageDelegator.drillers[index].name.Contains("SPECTER")) {
                index--;
            }

            vehicle = Instantiate(garageDelegator.drillers[index]);

            nPCMovements[npcIndex].haulerController = null;
            nPCMovements[npcIndex].drillTier = vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();

            // Indicate if changed from hauler or not
            npcIsHauler[npcIndex] = false;

            // Set agent type
            navMeshAgents[npcIndex].agentTypeID = NavMesh.GetSettingsByIndex(4).agentTypeID;

            if (mapRecordingMode.enabled) {
                // Gauarantee high speed Bore
                speed = 10;
            } else {
                speed = vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPlayerSpeed();
            }
            
            SetMapIcon(npcs[npcIndex], npcSpawnPoints[npcIndex], true);
        } 
        else { 

            // min is 4 because we don't want the base haulers
            int index;

            if (spawnSpecificIndex != -1) {
                index = spawnSpecificIndex;
            } else {
                index = seedRandom.Next(4, haulerThresholds[highestDrillTier - 1]);
            }

            if (index < 0) {
                index = 0;
            }

            if (mapRecordingMode.enabled) {
                // Gaurantee stubby
                index = 0;
            }

            // Helions are too large, too buggy, also there's no nav mesh surface for them
            if (garageDelegator.haulers[index].name.Contains("HELION")) {
                index--;
            }

            vehicle = Instantiate(garageDelegator.haulers[index]);

            nPCMovements[npcIndex].haulerController = vehicle.GetComponent<HaulerController>();
            nPCMovements[npcIndex].haulerController.SetAsNPC();
            nPCMovements[npcIndex].haulerIndex = index;

            // Indicate if changed to hauler or not
            npcIsHauler[npcIndex] = true;

            nPCMovements[npcIndex].drillTier = highestDrillTier;

            haulerController = vehicle.GetComponent<HaulerController>();

            // (width - 2) gives the right agent index for haulers
            navMeshAgents[npcIndex].agentTypeID = NavMesh.GetSettingsByIndex(haulerController.width - 2).agentTypeID;

            if (mapRecordingMode.enabled) {
                speed = 8;
                haulerController.IncreaseMaxMaterials(300);
            } else {
                speed = haulerController.GetPlayerSpeed();
            }
            
            SetMapIcon(npcs[npcIndex], npcSpawnPoints[npcIndex], false);
        }   

        // Must set speed after setting parent
        vehicle.transform.SetParent(npcs[npcIndex].transform, false);
        nPCMovements[npcIndex].SetSpeed(speed);
        
        // Need to prevent drillers from clipping each other
        nPCMovements[npcIndex].sortingGroup.sortingOrder = (npcIndex + 2) * 2 + 2;

        npcs[npcIndex].transform.position = npcSpawnPoints[npcIndex];

        ResetNPCPos(npcIndex);
    }

    public string GenerateBotName() {
        // Decide how many words to use (1 or 2)
        int wordCount = random.Next(1, 3);
        string botName = "";

        for (int i = 0; i < wordCount; i++)
        {
            string word = botNames[random.Next(botNames.Length)];
            int style = random.Next(3); // 0 = caps, 1 = lower, 2 = as is

            switch (style)
            {
                case 0: word = word.ToUpper(); 
                    break;
                case 1: word = word.ToLower(); 
                    break;
                // case 2: leave as is
            }

            botName += word;
        }

        // Determine the number of digits (0 to 3)
        int digitCount = random.Next(4);

        if (digitCount == 0) {
            return botName;
        }

        botName += random.Next((int) Math.Pow(10, digitCount)); // Random digit from 0 to 9

        return botName;
    }

    public Vector3 RequestNewMiningPosition(Vector3 pos, float rotation, int drillTier) {
        if (mapRecordingMode && mapRecordingMode.enabled) {
            // Gaurantee tier 1
            return mineRenderer.FindBestMiningPosition(3, 15, new((int) pos.x, (int) pos.y), rotation, 1);
        }
        
        return mineRenderer.FindBestMiningPosition(3, 15, new((int) pos.x, (int) pos.y), rotation, drillTier);
    }

    public Vector3 RequestNewHaulerPosition(int tier) {
        return uncollectedMaterialsDelegator.GetRandomMaterialLocation(tier);
    }

    public void SetMapIcon(GameObject vehicleParent, Vector3 spawnPoint, bool driller = true) {
        GameObject mapIcon = Instantiate(mapIconPrefab);
        mapIcon.transform.SetParent(vehicleParent.transform, false);

        SpriteRenderer spriteRenderer = mapIcon.GetComponent<SpriteRenderer>();

        // If player, use normal icon
        if (playerSpawnPoint == spawnPoint) {
            spriteRenderer.sprite = mapIcons[FindSpawnPointIndex(spawnPoint)];
        } else {
            if (driller) {
                spriteRenderer.sprite = mapIcons[FindSpawnPointIndex(spawnPoint)];
            } else {
                spriteRenderer.sprite = haulerIcon;
            }

            spriteRenderer.color = spawnColours[FindSpawnPointIndex(spawnPoint)];
        }
    }

    public void ResetNPCPos(int npcIndex) {
        if (!spawnPointTaken[npcIndex] || npcs[npcIndex] == null) {
            return;
        } 

        npcs[npcIndex].transform.position = npcSpawnPoints[npcIndex];
        npcs[npcIndex].transform.eulerAngles = new(0, 0, 90);
    }

    public void ResetAllNPCPos() {
        for (int i = 0; i != npcCount; i++) {
            ResetNPCPos(i);
        }
    }

    public Vector3 GetSpawnPoint() {
        Vector3 spawnPoint = new();

        for (int i = 0; i != spawnPoints.Length; i++) {
            if (spawnPointTaken[i]) {
                continue;
            }
        
            spawnPoint = spawnPoints[i].position;
            spawnPointTaken[i] = true;
            break;
        }

        return spawnPoint;
    }

    public int FindSpawnPointIndex(Vector3 spawnPoint) {

        for (int i = 0; i != spawnPoints.Length; i++) {
            if (spawnPoints[i].position == spawnPoint) {
                return i;
            }
        }

        return -1;
    }

    public void LoadData(GameData data) {

        spawnPointTaken = new bool[spawnPoints.Length];
        // -1 because 1 spawn point goes to the player
        npcCount = spawnPoints.Length - 1;
        npcs = new GameObject[npcCount];
        npcSpawnPoints = new Vector3[npcCount];
        npcIsHauler = new bool[npcCount];
        nPCMovements = new NPCMovement[npcCount];
        nPCNames = new string[npcCount];
        nPCTimeRemaining = new int[npcCount];
        transitioningVehicle = new bool[npcCount];

        navMeshAgents = new NavMeshAgent[npcCount];

        StartCoroutine(PrepareGame());
    }

    private IEnumerator PrepareGame() {
        yield return new WaitUntil(() => mineRenderer.coopMineLoaded);

        PlacePlayer();
        highestDrillTier = playerState.GetHighestDrillTier();

        playerRebirths = playerState.GetRebirths();

        // Uncomment when recording
        if (mapRecordingMode.enabled) {
            npcCount = 2;
            toggleCamera.onClick.Invoke();
        }

        for (int i = 0; i != npcCount; i++) {
            CreateNPC(i);
        }

        Time.timeScale = 6;
        // 5 real seconds
        // Comment when recording
        if (!mapRecordingMode.enabled) {
            yield return new WaitForSeconds(30);
            Time.timeScale = 1;
        }

        try {
            StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems(gameObject));
        } catch {
        }

        StartCoroutine(RunLiveManageSession());
    }

    private IEnumerator RunLiveManageSession() {
        while (true) {

            if (liveSessionCoroutine == null) {
                liveSessionCoroutine = StartCoroutine(LiveManageSession());
            }

            yield return new WaitForSeconds(5);
        }
    }

    private IEnumerator LiveManageSession() {
        while (true) {
            yield return new WaitForSeconds(sessionUpdateTimer);

            if (Application.internetReachability == NetworkReachability.NotReachable) {

                for (int i = 0; i != npcCount; i++) {
                    if (nPCMovements[i] != null) {
                        nPCMovements[i].stopMoving = true;
                    }
                }

                Time.timeScale = 0;

                lostInternetScreen.SetActive(true);

                yield break;
            }

            int haulerCount = 0;

            // Remove npcs if they are done playing
            for (int i = 0; i != npcCount; i++) {
                nPCTimeRemaining[i] -= sessionUpdateTimer;

                // Object somehow disappeared
                // usually happens when npc game object is destroyed when transitioning, then a new one doesnt spawn from CreateNPC because of RNG
                if (nPCTimeRemaining[i] > 0 && npcs[i] == null && !transitioningVehicle[i]) {
                    // Remove player without destroying gameobject
                    RemoveNPC(i, true);
                    Debug.Log("R: REMOVED ERROR " + i);
                }
                else if (nPCTimeRemaining[i] < 0 && npcs[i] != null && !transitioningVehicle[i]) {
                    // Remove player normally
                    RemoveNPC(i, false);
                }
                    
                if (npcIsHauler[i]) {
                    haulerCount++;
                }
            }

            haulers = haulerCount;

            if (uncollectedMaterialsDelegator.materialCount > Get1HaulerThreshold() && haulers < 1 && !drillingNeeded) {
                yield return SwitchDrillerToHauler();
            } 
            else if (uncollectedMaterialsDelegator.materialCount > Get2HaulerThreshold() && haulers < 2 && !drillingNeeded) {
                yield return SwitchDrillerToHauler();
            } 
            else if (uncollectedMaterialsDelegator.materialCount > Get3HaulerThreshold() && haulers < 3 && !drillingNeeded) {
                if (haulers < 1) {
                    drillingNeeded = true;
                } else {
                    yield return SwitchDrillerToHauler();
                }
            }

            // Fill empty NPC spot every 1 min
            nPCEmptyTimer += sessionUpdateTimer;

            if (nPCEmptyTimer > 60) {

                for (int i = 0; i != npcCount; i++) {

                    if (npcs[i] == null && !transitioningVehicle[i]) {
                        int spawnIndex = FindSpawnPointIndex(npcSpawnPoints[i]);

                        // If no spawn index, then none was ever set
                        if (spawnIndex == -1) {
                            CreateNPC(i);
                            break;
                        }

                        spawnPointTaken[spawnIndex] = false;
                        CreateNPC(i);
                        // Only bring 1 player max every time
                        break;
                    }
                }

                nPCEmptyTimer = 0;
            }

            CheckIfHaulingNeeded(-1);

            highestDrillTier = playerState.GetHighestDrillTier();
        }
    }

    private IEnumerator SwitchDrillerToHauler() {

        int npcToChange;
        bool inactiveNPC;

        do {
            npcToChange = random.Next(0, 3);

            inactiveNPC = nPCMovements[npcToChange] == null;

            // Prevents infinite loop, in case all npcs are haulers, but this coroutine was called somehow
            bool allHaulers = true;
            for (int i = 0; i != npcCount; i++) {
                if (!npcIsHauler[i]) {
                    allHaulers = false;
                    break;
                }
            }

            if (allHaulers) {
                yield break;
            }
        } 
        while(npcIsHauler[npcToChange] || inactiveNPC);

        transitioningVehicle[npcToChange] = true;

        if (nPCMovements[npcToChange] == null) {
            yield break;
        }
        
        nPCMovements[npcToChange].stopMoving = true;
        yield return new WaitForSeconds(3);

        Destroy(npcs[npcToChange]);
        CreateNPC(npcToChange, false, false);

        transitioningVehicle[npcToChange] = false;
    }

    private IEnumerator SwitchHaulerToDriller(int npcToChange) {

        transitioningVehicle[npcToChange] = true;

        nPCMovements[npcToChange].stopMoving = true;
        yield return new WaitForSeconds(3);

        DropMaterials(npcToChange);
        Destroy(npcs[npcToChange]);
        CreateNPC(npcToChange, true, false);

        transitioningVehicle[npcToChange] = false;
    }

    public IEnumerator SwitchToAnotherHauler(int npcIndex, int haulerIndex) {

        if (transitioningVehicle[npcIndex]) {
            yield break;
        }

        transitioningVehicle[npcIndex] = true;

        int index = haulerIndex - 1;

        if (index < 4) {
            index = 4;
        }

        nPCMovements[npcIndex].stopMoving = true;

        yield return new WaitForSeconds(3);

        nPCMovements[npcIndex] = null;
        
        DropMaterials(npcIndex);
        Destroy(npcs[npcIndex]);
        CreateNPC(npcIndex, false, false, index);

        transitioningVehicle[npcIndex] = false;
    }

    public int Get1HaulerThreshold() {
        return 320 + (130 * highestDrillTier);
    }

    public int Get2HaulerThreshold() {
        return 390 + (130 * highestDrillTier);
    }

    public int Get3HaulerThreshold() {
        return 500 + (150 * highestDrillTier);
    }

    public void DropMaterials(int npcIndex) {

        HaulerController haulerController = npcs[npcIndex].transform.GetChild(2).GetComponent<HaulerController>();

        // Need this because there's no check of whether its a hauler or driller in RemoveNPC
        if (!haulerController) {
            return;
        }

        int[] materialCount = haulerController.GetMaterialCount();

        for (int i = 0; i != materialCount.Length; i++) {
            // Should never be less than zero but just in case
            if (materialCount[i] <= 0) {
                continue;
            }

            mineRenderer.GetMaterialObject(i, npcs[npcIndex].transform.position, materialCount[i], haulerController.GetProfitMultiplier());
        }

    }

    public void RemoveNPC(int npcIndex, bool gameObjectDestroyed) {

        int spawnIndex = FindSpawnPointIndex(npcSpawnPoints[npcIndex]);

        if (!gameObjectDestroyed) {
            DropMaterials(npcIndex);
            Destroy(npcs[npcIndex]);
        }

        npcs[npcIndex] = null;
        
        npcIsHauler[npcIndex] = false;
        nPCTimeRemaining[npcIndex] = 0;

        spawnPointNameTexts[spawnIndex].text = "";
        spawnPointTaken[spawnIndex] = false;
    }

    public void SaveData(ref GameData data) {

    }

    public bool CheckIfHaulingNeeded(int npcIndex, bool hasCargo = false) {

        if (uncollectedMaterialsDelegator.materialCount < Get1HaulerThreshold()) {
            // -1 means it wasn't called from a npc
            if (npcIndex != -1 && !hasCargo) {
                StartCoroutine(SwitchHaulerToDriller(npcIndex));
                return false;
            } else if (hasCargo) {
                return true;
            }
        }

        // return false = become a drill
        // return true = keep hauling
        Vector3 newHaulerPosition;
        int haulPositionCount = 0;

        // If path is null, initialize it
        path ??= new NavMeshPath();

        do {
            newHaulerPosition = RequestNewHaulerPosition(highestDrillTier);

            haulPositionCount++;
        } 
        while((!testAgent.CalculatePath(newHaulerPosition, path) || path.status != NavMeshPathStatus.PathComplete) && haulPositionCount < 60);

        if (haulPositionCount >= 60 || Vector3.Distance(newHaulerPosition, new(0, -6)) < 0.2) {
            drillingNeeded = true;

            // -1 means it wasn't called from a npc
            if (npcIndex != -1 && !hasCargo) {
                StartCoroutine(SwitchHaulerToDriller(npcIndex));
                return false;
            } else if (hasCargo) {
                return true;
            }

            return false;
        } 

        drillingNeeded = false;
        return true;
    }

    public Color[] GetSpawnColors() {
        return spawnColours;
    }

    public int GetMaterialCount() {
        return uncollectedMaterialsDelegator.materialCount;
    }
}