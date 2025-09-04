using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NPCManager : MonoBehaviour, IDataPersistence
{
    private static NPCManager _instance;
    public static NPCManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<NPCManager>();
            }
            return _instance;
        }
    }

    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] public GameObject mapIconPrefab;

    public GameObject[] npcs;
    public GameObject[] upgradeNoticeIcons;
    public GameObject pointToDrillArrow;
    private NavMeshAgent[] navMeshAgents;
    private NPCMovement[] nPCMovements;
    private string[] nPCNames;

    private readonly Color[] spawnColours = { new(246 / 255f, 4 / 255f, 3 / 255f), new(57 / 255f, 255 / 255f, 21 / 255f), new(2 / 255f, 191 / 255f, 255f / 255f), new(255f / 255, 166 / 255f, 2 / 255f) };

    [Header("Scripts")]
    public MineRenderer mineRenderer;
    public PlayerState playerState;

    private bool waitingInLobby = false;

    private readonly string[] botNames = {
        /*"Crimson", "Rusty", "Lunar", "Solar", "Astro", "Quantum",
        "Nova", "Phantom", "Obsidian", "Cobalt", "Plasma", "Ironclad",
        "Zephyr", "Void", "Gritty", "Vortex", "Redshift", "Orbital",
        "Radiant", "Pyro", "Blazing", "Silent", "Nebula", "Electric",
        "Shadow", "Frozen", "Glitchy", "Titan", "Infernal", "Chrome",
        "Echo", "Warped", "Venomous", "Hazard", "Stellar", "Jaded",
        "Atomic", "Grim", "Pixelated", "Blistered", "Cyber", "Fractal",
        "Miner", "Rover", "Seeker",
        "Pioneer", "Scout", "Prospector", "Nomad", "Quaker", "Astronaut",
        "Digger", "Crawler", "Core", "Reactor", "Golem", "Harvester",
        "Excavator", "Sentinel", "Warden", "Breaker", "Scraper", "Forager",
        "Smelter", "Raider", "Assembler", "Tracer", "Forgemaster", "Grinder",
        "Operator", "Runner", "Chiseler", "Refiner", "Surveyor", "Plunderer",
        "Reclaimer", "Destructor", "Engineer", "Drifter", "Observer", "Stalker",
        "M1ner", "D1gg3r", "R0verX", "B00t404", "Dr1ll3r", "Nom4d",
        "Qw4ker", "H4ulr", "S3eker", "Gr1nder", "Ph4ntom", "Xpl0rer",
        "AstroN0m", "Obsid1an", "R3dsh1ft", "C0reBrkr", "Fr4ct4l",
        "Harv3ster", "D1v3r", "R0gueX", "W4rpdriv3", "G1itch", "V01dX",
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
        "Exotron", "Silicore", "Nocturn", "Halcyon", "Excalibur", "Typhon",
        "EchoCore", "NeonEdge", "ChronoX", "Celestus", "Pyrevolt", "Stormdrift",
        "Dreadnought", "Evolvion", "Voltar", "Strikron", "Roguewave", "Cipheron",
        "Glacion", "HyperCore", "Tesseract", "Omniflare", "Infernix", "Mechara",
        "Nucleon", "Skybreaker", "Voidstorm", "Cyclonix", "Oblivix", "Fission",
        "T3rra", "D3f1ler", "M3chX", "Crat3r", "Bl4st0ff", "F1ss10n", "Ast3r01d99",
        "T0rqu3", "Xen0nX", "Hydr0Ph4z3", "P3rmafrost", "L4vafl0w", "N3bularX",
        "Cyb3rnaut", "Quantum99", "R3nd3rX", "Z3r0P01nt", "Excav8r", "Thr4sh3r",
        "T1m3warp", "S0n1cX", "W4rpg4te", "Ragnar0k", "Pyr0x", "Dr4g0nB0t",
        "BlackH0l3", "V3l0c1tr0n", "R4d10act1v3", "Synth3X", "Turb0C0r3", "H3llfir3",
        "Gl4c14l", "Xpl01t3r", "M0chafl3re", "N1ghtSh4d3", "D3m0n1cX", "Puls4r99",
        "C0sm0tr0n", "D4t4M1n3r", "T3kn0M4ncer", "X-t3rmin8r", "Crypt0X", "F1r3wallX",
        "Bl1tzkr13g", "Str4t0blast", "C3l3st14lX", "Ph0t0nDr1ft", "T0rment0r", "V0rax",
        "Ex0G3n", "Lun4rM3ch", "D3struct0rX", "Havoc99", "M0n0l1thX", "Solara", "Nebulite",
        "Quantara", "Eclipseon", "Marsforge", "Orbitron",  "Fizzcrank",
        "Gravion", "Celestior", "Voidcore", "Exohelm", "Starforge", "Zenithal",
        "Planetrak", "Lunaris", "Orbex", "Pulsanova", "Aerion", "Astrogon",
        "Thrustar", "Galaxior", "Scrapline", "Steeljaw", "Rustclank", "Coregrind",
        "Drillbit", "Rockmaw", "Depthcrawler", "Ironbore", "Gritforge", "Shaftwalker",
        "Coremauler", "Deepgrip", "Crustpiercer", "Burrower", "Gunkrake",
        "Tunneljaw", "Maghammer", "Coregnaw", "Crushunit", "Codeburn", "SyntaxX",
        "Bytevoid", "Hackbit", "Packetstorm", "Glitchphase", "Compilr", "Databurn",
        "Cryptron", "0vercrank", "Err0rUnit", "Ramcore", "Debugga", "Fragloop",
        "S3gm3ntX", "Nullwave", "Cr4shdr1ll", "M4lfunct", "Bitm4sk", "C0d3Wr3ck",
        "M1n3R4g3", "Dr1ftX99", "Cl4wB0t", "R0ck3tM1n3", "N0D3cr4ck", "T3rr4Dr1ll",
        "R3kt0r", "S1l1c0nX", "V01dR1pp3r", "D34dB00t", "Gr1mF1nn", "Sh4d0wC0r3",
        "Sp1nDr1ll", "R4v3nM3ch", "M1n3flare", "Carb0nCr0wl", "Xcv8Roid", "D3pthC0de",
        "PlasmGr1nd", "T1nkrX", "Thudbrick", "Vantabot", "Blortok", "Greeblor",
        "Hexnut", "Plink99", "Drubbler", "Torqueleech", "Yttrion", "Ogranik",
        "Drossel", "Splinewalker", "Bronzorb", "Nokturne", "Frakspur", "Grundlebot",
        "Screevix", "Dustwhirl", "Squirmatron", "Bytequake", "CryoCore", "SynthForge",
        "Mechaweld", "Nanoflux", "Voxcircuit", "ExoShaft", "Rustpulse", "W4rpOre",
        "Zerobit", "V1rusPrime", "Pulsebyte", "Nanonite", "GhostProc", "Bitrend",
        "LagSpike", "HaxxorX", "KernelDrill", "C0d3spl1n3", "Crypt0burn", "Datawr4ck",
        "Glitchnode", "NullForge", "Shaftcore", "Quakejaw", "Tunnelgrind", "Orevex",
        "Drillquake", "Grindjaw", "Loadcore", "Tectodrill", "Ravencore", "Dredgebot",
        "Ironpick", "Corelatch", "Shovax", "Graveldent", "Deepburrow", "Substrator",
        "Excavix", "Pitburner", "Stratavore", "Chasmwalker", "Corehound", "Smeltlock",
        "Subcore", "Drillstorm", "Tremorclank", "Dustcrank", "Blastjaw", "Forgeleech",
        "Pickshard", "Hackminr", "Tunnellite", "Gravshift", "Bytecrack", "Frackbyte",
        "Nodeclank", "Burrowcore", "Voidminer", "EchoDrill", "Bitshredder", "Rubble",
        "Gritunit", "Shattercore", "Digipick", "Lodebyte", "Orebit", "Rocktide",
        "Downbit", "Clangbo", "Breachcore", "Furnix", "V1b3drill", "Fractanite",
        "SubnetX", "Blazebore", "TunnelByte", "Plasmcore", "Thumpjaw", "Ramshard",
        "Forkbit", "MagForge", "Crackrake", "Xtractron", "Shardflux", "Corefuse",
        "DrillNova", "orock", "Wrenchbit", "Datadent", "TremorX", "Krushbyte",
        "Burstrak", "Shov3lr", "Coalbolt", "Plasmweld", "Nodeflare", "Pitshift",
        "Drivax", "Drillgeist", "Voidclank", "Orecrank", "Dustprobe", "Mineweld",
        "Shatter", "Warpforge", "EchoNode", "Cryptforge", "Crackbit", "V0ltbore",
        "Dr1llwr4th", "Fract0byte", "DataSp1ke", "Br3achdrill", "Gl1tchr1ft", "Excavatr0n",
        "Sm3ltByte", "Foragron", "Weldshift", "Dr3dgex", "Coreb1t3", "Bitcr4wl", "LavaCoreX",
        "Downshard", "Sp1kegr1nd", "Ramgrind", "Tunnelbit", "Nodebreaker", "Shockcrank",
        "Ch4smX", "Cr4ck3r", "Grittr0n", "Boltjaw", "Ironcrawl", "Spl1nt3r", "Orekn1ght",
        "Zer0core", "Nodev1per", "Tunnelclaw", "SubterraX", "Obsidrax", "Nanogrind",
        "Slic3o", "Strataclaw", "Hackjaw", "Borift", "Crustmole", "GravforgeX",
        "Excavat3r77", "B1tburn", "CryoD1g", "Rockbyte", "Gnashunit", "S0ilburst",
        "Corebl1tz", "Faultbt", "D3pthshock", "B0rex", "R1ftbit", "VoidripperX",
        "Downclank", "Sh3arot", "Orew1zard", "D3epFl4re", "Smeltbyte", "Tunn3lDr0id",
        "Wrenchflux", "Gritslicer", "VoidB0t99", "F1ssot", "Axebot", "Depthbore",
        "Rockgr1nd", "B1tm4gnet", "Tect0buster", "Tr3morphase", "Furnacrusher", "OreSurge",
        "Chasmf1re", "Frackture", "Spl1nto", "Downbit3", "Mechdenter", "Loadsplit",
        "Magdriller", "Voltb0re", "Voidgrindr", "CorejawX", "B0tM1ner", "Thrashtr0n",
        "Subb0tX", "Tectonizer", "R0ckBr34k3r", "M1nerCl4nk", "NullM1ner", "Shaftb1t",
        "Dr1llburst", "Burrowb1t3", "MineR0v3", "Blastshaft", "Sk1ncr4per", "StrataSp1n",
        "OreJawz", "L0wbit", "Trenchcore", "Krankforge", "RubbleClank", "Chiselcore",
        "Fragmentor", "IroncoreX", "Digiz3r0", "Faultline", "Coreshock", "Tectogrid",
        "Draggat", "Voltrench", "Crackcore", "Cl4nkb1t", "Tunn3lJaw", "Xpl0drill",
        "Rustw0rm", "Tremorbolt", "Rubblebyte", "Voidtick", "Crushblitz", "Gritchurn",
        "Corequake", "B1tf0ssil", "Chasmflare", "Crush99", "Sh4ftsm4sh", "Xdr1ll3r",
        "Depthfury", "Gr1tcr4wl", "Frackbl4st", "Dr1llsp1n", "Cryoclaw", "Loadgr1p",
        "Tunnelcrack", "R1ftzone", "Slagot", "B0r3storm", "Oreblitz", "Magcrush",
        "Shatterminr", "Mechfract", "Digg0tron", "Pulverizer", "StrataX", "Downsplit",
        "Clunkbyte", "Subm3ch", "Gritstream", "Slicebore", "Gouget", "Blastknuckle",
        "Corepit", "BurnboX", "Shaftb1nder", "Flaregr1nd", "Fr4ckstorm", "WeldcoreX",
        "D3pthslicer", "LoadbitX", "Corecl4w", "Plungebit", "H4mmerJaw", "Moltenr1ft",
        "ExoCore", "Grimjaw", "Mineflare", "Cl4nkburst", "Nullshred", "Bytefracture",
        "Excavatik", "Tunnelcrusher", "Ironshifter", "Forgebyte", "Faultbreaker",
        "MinecrushX", "LavaChurn", "Blasterclank", "Shaftstrike", "Tect0flare",
        "Tunn3lshock", "Pickblitz", "Rustquake", "Coalstorm", "Fractunit", "Splint3rdrill",
        "Pitblast", "Shockvane", "D1ggrX", "OverburdenX", "Smashbit", "VoidScrap",
        "Oregrinder", "Deepbit", "Voltagrind", "Ironburst", "ShardbX", "Nodeblaster",
        "Bitstrike", "Rocksplit", "Drillblitz", "Nulljaw", "Fraybt", "Orelock",
        "Tunnelthrasher", "QuakeSp1n", "Depthjaw", "Datashard", "Gritpulse", "Burrowburn",
        "Oreburst", "M1n3sl4sh",*/
    };

    [Header("Camera Controls")]
    private int droneCameraIndex;
    public Image toggleCameraModeButton;
    public GameObject cameraIterateControls;
    public GameObject refineryUpgradePanel;
    public GameObject closeRefineryButton;
    public GameObject importantInfo;
    public Sprite manualControlIcon;
    public Sprite autoControlIcon;
    public Image manualControlIconImage;
    public Image manualControlButtonImage;

    [Header("Cache")]
    readonly System.Random random = new();
    private Camera mainCamera;
    // Only used to reference mineCount, because mineCount is used for random seed generation
    GameData snapshotGameData;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    public void CreateNPC(int npcIndex = -1)
    {
        // If -1, then that means to create a new one and give it the next index
        // Since VehicleUpgradeBayManager.Instance.GetDroneCount() tracks how many drones there are and is not zero-indexed, VehicleUpgradeBayManager.Instance.GetDroneCount() is equal to the index of the next drone
        if (npcIndex == -1)
        {
            npcIndex = VehicleUpgradeBayManager.Instance.GetDroneCount() - 1;
        }

        npcs[npcIndex] = Instantiate(npcPrefab);

        nPCNames[npcIndex] = GenerateBotName(npcIndex);

        // Choose a random factor to multiply by, so adjacent levels aren't too similar
        int multiplicationFactor = new System.Random(snapshotGameData.mineCount).Next(0, 61);
        System.Random seedRandom = new System.Random(multiplicationFactor * (npcIndex + snapshotGameData.mineCount));

        nPCMovements[npcIndex] = npcs[npcIndex].GetComponent<NPCMovement>();
        nPCMovements[npcIndex].npcIndex = npcIndex;
        nPCMovements[npcIndex].nPCManager = this;

        nPCMovements[npcIndex].npcNameText.text = nPCNames[npcIndex];
        // nPCMovements[npcIndex].npcNameText.color = spawnColours[spawnIndex];
        nPCMovements[npcIndex].worldSpaceCanvas.worldCamera = mainCamera;
        nPCMovements[npcIndex].button.GetComponent<Button>().onClick.AddListener(() => DroneTapped(npcIndex));
        upgradeNoticeIcons[npcIndex] = nPCMovements[npcIndex].noticeIcon.gameObject;

        navMeshAgents[npcIndex] = nPCMovements[npcIndex].agent;

        // Used for tutorial
        if (npcIndex == 0)
        {
            pointToDrillArrow = nPCMovements[npcIndex].pointToDrillArrow;
        }

        GameObject[] drillPrefabs = VehicleUpgradeBayManager.Instance.GetAllDrillPrefabs();
        GameObject vehicle;

        // Agent types and indexes: Humanoid (0), Width 3 (1), Width 4 (2), Width 5 (3), Driller (4)
        float speed;

        // Choose random drill
        //int index = seedRandom.Next(drillPrefabs.Length);
        int index = npcIndex;

        // If its a Specter, drop the index by one. I haven't retested it yet
        /*if (garageDelegator.drillers[index].name.Contains("SPECTER")) {
            index--;
        }
        // If its a grinder, increment index. They are small and slow, not good for the NPC algorithm which needs fast or wide drills (mostly wide)
        if (garageDelegator.drillers[index].name.Contains("GRINDER")) {
            index++;
        }*/

        vehicle = Instantiate(drillPrefabs[index]);

        DrillerController drillerController = vehicle.transform.GetChild(1).GetComponent<DrillerController>();

        Sprite[] bodySprites = VehicleUpgradeBayManager.Instance.GetAllDrillBodySprites(index);
        // Set sprite
        vehicle.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = bodySprites[seedRandom.Next(bodySprites.Length)];
        // If there's an animator on the drill, change that
        // Otherwise change the spriterenderer
        Animator drillAnimator = vehicle.transform.GetChild(1).GetComponent<Animator>();

        if (drillAnimator)
        {
            RuntimeAnimatorController[] runtimeAnimatorControllers = VehicleUpgradeBayManager.Instance.boreDrills;
            drillAnimator.runtimeAnimatorController = runtimeAnimatorControllers[seedRandom.Next(runtimeAnimatorControllers.Length)];
        }
        else
        {
            Sprite[] drillerSprites = VehicleUpgradeBayManager.Instance.GetAllDrillDrillerSprites(drillerController.drillTypeIndex);
            vehicle.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = drillerSprites[seedRandom.Next(drillerSprites.Length)];
        }

        // Set agent type
        navMeshAgents[npcIndex].agentTypeID = NavMesh.GetSettingsByIndex(4).agentTypeID;

        Transform droneDetailsPanel = nPCMovements[npcIndex].droneDetails;

        // Set overheat progress bar values in the driller controller
        Transform droneOverheatBar = droneDetailsPanel.GetChild(0);
        drillerController.sliderImage = droneOverheatBar.GetChild(1).GetChild(0).GetComponent<Image>();
        drillerController.slider = droneOverheatBar.GetComponent<Slider>();
        drillerController.sliderTransform = droneOverheatBar.GetComponent<RectTransform>();
        drillerController.sliderText = droneOverheatBar.GetChild(2).GetComponent<TextMeshProUGUI>();
        drillerController.initialScale = drillerController.sliderTransform.localScale;

        speed = drillerController.GetPlayerSpeed();
        drillerController.nPCMovement = nPCMovements[npcIndex];
        nPCMovements[npcIndex].drillerController = drillerController;

        // Set cash earned displays
        nPCMovements[npcIndex].cashIconSpriteRenderer = droneDetailsPanel.GetChild(1).GetComponent<SpriteRenderer>();
        nPCMovements[npcIndex].cashEarnedText = droneDetailsPanel.GetChild(2).GetComponent<TextMeshProUGUI>();

        SetMapIcon(npcIndex);

        // Must set speed after setting parent
        vehicle.transform.SetParent(npcs[npcIndex].transform, false);
        nPCMovements[npcIndex].SetSpeed(speed);

        // Need to prevent drillers from clipping each other
        nPCMovements[npcIndex].sortingGroup.sortingOrder = (npcIndex + 2) * 2 + 2;

        ResetNPCPos(npcIndex);

        npcs[npcIndex].name = nPCNames[npcIndex] + " " + drillPrefabs[index].name;
    }

    public string GenerateBotName(int droneIndex)
    {
        // Decide how many words to use (1 or 2)
        /*string botName;

        // Choose name
        while (true)
        {
            botName = botNames[random.Next(botNames.Length)];

            // Make sure name is unique
            for (int i = 0; i != VehicleUpgradeBayManager.Instance.GetDroneCount(); i++)
            {
                // Name is not unique, choose another
                if (nPCNames[i] != null && botName == nPCNames[i])
                {
                    continue;
                }
            }
        }*/

        return (droneIndex + 1).ToString();
    }

    public Vector3 RequestNewMiningPosition(Vector3 pos, float rotation, int drillTier, NPCMovement nPCMovement)
    {
        return mineRenderer.FindBestMiningPosition(0, 15, new((int)pos.x, (int)pos.y), rotation, drillTier, nPCMovement);
    }

    public void SetMapIcon(int droneIndex)
    {
        GameObject mapIcon = Instantiate(mapIconPrefab);
        mapIcon.transform.SetParent(npcs[droneIndex].transform, false);

        SpriteRenderer spriteRenderer = mapIcon.GetComponent<SpriteRenderer>();

        // Set sprite and icon
        //spriteRenderer.color = spawnColours[FindSpawnPointIndex(spawnPoint)];     
        spriteRenderer.color = Color.red;
    }

    public void ResetNPCPos(int npcIndex)
    {
        if (npcs[npcIndex] == null)
        {
            return;
        }

        npcs[npcIndex].transform.position = spawnPoint.position;
        npcs[npcIndex].transform.eulerAngles = new(0, 0, 90);
    }

    public void ResetAllNPCPos()
    {

        for (int i = 0; i != VehicleUpgradeBayManager.Instance.GetDroneCount(); i++)
        {
            ResetNPCPos(i);
        }
    }

    public void LoadData(GameData data)
    {
        snapshotGameData = data;

        const int maxDrones = 6;

        npcs = new GameObject[maxDrones];
        upgradeNoticeIcons = new GameObject[maxDrones];
        nPCMovements = new NPCMovement[maxDrones];
        nPCNames = new string[maxDrones];
        navMeshAgents = new NavMeshAgent[maxDrones];

        StartCoroutine(PrepareGame());
    }

    private IEnumerator PrepareGame()
    {
        yield return new WaitUntil(() => mineRenderer.soloMineLoaded);

        try
        {
            StartCoroutine(LoadingScreen.Instance.IncrementLoadedItems(gameObject));
        }
        catch
        {
        }
    }

    public void SaveData(ref GameData data)
    {

    }

    public IEnumerator WaitInLobby()
    {
        waitingInLobby = true;

        if (mineRenderer.mineInitialization == 0)
        {
            for (int i = 0; i != VehicleUpgradeBayManager.Instance.GetDroneCount(); i++)
            {
                if (nPCMovements[i] != null)
                {
                    StartCoroutine(nPCMovements[i].WaitInSpawnPosition(GetRandomSpawnPosition()));
                }
            }
        }

        yield return new WaitUntil(() => mineRenderer.mineInitialization != 0);
        waitingInLobby = false;
    }

    public Color[] GetSpawnColors()
    {
        return spawnColours;
    }

    // NOT A SPAWN POINT, JUST A RANDOM COORDINATE IN THE LOBBY
    public Vector3 GetRandomSpawnPosition()
    {

        if (random.NextDouble() < 0.66)
        {
            // In front of entrance
            return new(0, 0);
        }

        // x in [‑6, 6)
        float x = (float)random.NextDouble() * (6 - (-6)) + (-6);
        // y in [1, 7)
        float y = (float)random.NextDouble() * (7 - 1) + 1;

        return new(x, y);
    }

    public void DroneTapped(int droneIndex)
    {
        if (droneIndex == -1)
        {
            HideRefineryPanel();
            return;
        }

        droneCameraIndex = droneIndex;

        // If not following a drone or following another drone, then start following the one that was just tapped
        if (GameCameraController.Instance.droneToFollow != npcs[droneIndex].transform)
        {
            if (JoystickMovement.Instance.nPCMovement)
            {
                ToggleManualDroneControl();
            }

            GameCameraController.Instance.SetDroneToFollow(npcs[droneIndex].transform);
            ShowUIControls();

            // if active, disable (whether player tapped the same drone or new drone)
            if (refineryUpgradePanel.activeSelf)
            {
                HideRefineryPanel();
            }
            return;
        }

        // if active, disable (whether player tapped the same drone or new drone)
        if (refineryUpgradePanel.activeSelf)
        {
            HideRefineryPanel();
            return;
        }

        // If inactive, enable (only if player tapped same drone that they are currently following)
        if (!refineryUpgradePanel.activeSelf)
        {
            // Call this so that it zooms back in. We do this so the player isn't zoomed out too far when the panel opens
            // If they are zoomed out too far, and close to the edge of the map, the camera clamping will cause the drone
            // to be off screen, and then the player can't close the panel
            GameCameraController.Instance.SetDroneToFollow(npcs[droneIndex].transform);

            OreDelegation.Instance.PrepareGrid();
            
            UIDelegation.Instance.RevealElement(closeRefineryButton);
        }
    }

    public void ToggleManualDroneControl()
    {
        // If player is manually controlling something, then enable automatic controls
        if (JoystickMovement.Instance.nPCMovement)
        {
            JoystickMovement.Instance.joystickBG.SetActive(false);
            JoystickMovement.Instance.joystick.SetActive(false);
            JoystickMovement.Instance.nPCMovement = null;
            JoystickMovement.Instance.joystickRaycastImage.raycastTarget = false;
            manualControlIconImage.sprite = manualControlIcon;
            manualControlButtonImage.color = new(143f / 255, 20f / 255, 1);
            return;
        }

        // If not controlling anything, then enable manual controls
        if (nPCMovements[droneCameraIndex] == null)
            return;
        TutorialManager.Instance.TellPlayerToMove();
        JoystickMovement.Instance.joystickRaycastImage.raycastTarget = true;
        JoystickMovement.Instance.nPCMovement = nPCMovements[droneCameraIndex];
        manualControlIconImage.sprite = autoControlIcon;
        manualControlButtonImage.color = new(100f / 255, 179f / 255, 216f / 255);
    }

    private void HideRefineryPanel()
    {
        UIDelegation.Instance.HideElement(refineryUpgradePanel);
        closeRefineryButton.SetActive(false);
        UIDelegation.Instance.RevealAll();
    }

    public void ToggleCameraMode()
    {
        if (JoystickMovement.Instance.nPCMovement)
        {
            ToggleManualDroneControl();
        }

        // If camera is following a drone, tell it to stop
        if (GameCameraController.Instance.droneToFollow)
        {
            GameCameraController.Instance.droneToFollow = null;

            // Update UI
            toggleCameraModeButton.color = new(1, 64 / 255f, 129 / 255f);
            cameraIterateControls.SetActive(false);
            manualControlButtonImage.gameObject.SetActive(false);
            return;
        }

        // If player has no drones
        if (VehicleUpgradeBayManager.Instance.GetDroneCount() == 0)
        {
            UIDelegation.Instance.ShowError("YOU DON'T HAVE ANY DRONES!");
            return;
        }

        // Otherwise, tell it to follow a drone
        droneCameraIndex = 0;
        GameCameraController.Instance.SetDroneToFollow(npcs[droneCameraIndex].transform);

        // Update UI
        ShowUIControls();
    }

    public void SwitchDroneCamera(int direction)
    {
        if (VehicleUpgradeBayManager.Instance.GetDroneCount() == 1)
        {
            UIDelegation.Instance.ShowError("YOU ONLY HAVE 1 DRONE!");
            return;
        }

        // direction = 1, go forward. direction = -1, go backward
        int startIndex = droneCameraIndex;
        int index = droneCameraIndex;

        // Find the next available drone camera and search in the right direction
        do
        {
            // Have to subtract direction, not add
            index = (index - direction + npcs.Length) % npcs.Length;
            if (npcs[index] != null)
            {
                droneCameraIndex = index;
                GameCameraController.Instance.SetDroneToFollow(npcs[index].transform);

                if (JoystickMovement.Instance.nPCMovement)
                {
                    ToggleManualDroneControl();
                }

                return;
            }
        }
        while (index != startIndex);
    }

    private void ShowUIControls()
    {
        toggleCameraModeButton.color = new(1, 0, 0);
        cameraIterateControls.SetActive(true);
        manualControlButtonImage.gameObject.SetActive(true);
    }
}