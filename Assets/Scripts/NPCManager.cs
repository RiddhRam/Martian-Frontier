using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

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
    private Transform playerVehicle;
    private Vector3 playerSpawnPoint;

    [SerializeField]
    private GameObject[] npcs;
    private NavMeshAgent[] navMeshAgents;
    private Vector3[] npcSpawnPoints;
    private NPCMovement[] nPCMovements;
    private string[] nPCNames;
    private bool[] npcIsHauler;
    // How long the npcs play for
    [SerializeField]
    private int[] nPCTimeRemaining;
    [SerializeField]
    private int nPCEmptyTimer = 0;

    // How many npc haulers are active
    private int haulers = 0;
    private int highestDrillTier = 1;
    private int playerRebirths = 0;
    private int npcCount;


    [SerializeField]
    private MineRenderer mineRenderer;
    [SerializeField]
    private UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    [SerializeField]
    private PlayerState playerState;
    [SerializeField]
    private GarageDelegator garageDelegator;

    private readonly int sessionUpdateTimer = 2;
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
        "Tr4cer77", "Bl1zz4rd", "Chrom3X", "Re4ct0r9", "V0rt3x99", "P1x3l"
    };

    readonly System.Random random = new();

    bool switchingVehicle = false;

    // Cache
    HaulerController haulerController;
    Coroutine liveSessionCoroutine;

    public void PlacePlayer() {
        int index = random.Next(0, spawnPoints.Length);

        playerSpawnPoint = spawnPoints[index].position;
        spawnPointTaken[index] = true;
        spawnPointNameTexts[index].text = PlayerPrefs.GetString("PlayerName");

        SetMapIcon(playerVehicle.gameObject, playerSpawnPoint);

        ResetPlayerPos();
    }

    public void ResetPlayerPos() {
        playerVehicle.position = playerSpawnPoint;
        playerVehicle.eulerAngles = new(0, 0, 90);
        playerVehicle.GetChild(0).eulerAngles = new(0, 0, 270);
    }

    public void CreateNPC(int npcIndex, bool driller = true, bool newBot = true, int spawnSpecificIndex = -1) {
        int number = random.Next(0, 10);

        // 10% chance of player not spawning
        if (number < 1) {
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

        nPCMovements[npcIndex] = npcs[npcIndex].GetComponent<NPCMovement>();
        nPCMovements[npcIndex].npcIndex = npcIndex;
        nPCMovements[npcIndex].nPCManager = this;
        nPCMovements[npcIndex].rebirthLevel = seedRandom.Next(playerRebirths - 2, playerRebirths + 5);

        // Clamp to be higher than -1
        if (nPCMovements[npcIndex].rebirthLevel < 0) {
            nPCMovements[npcIndex].rebirthLevel = 0;
        }

        navMeshAgents[npcIndex] = nPCMovements[npcIndex].agent;

        spawnPointNameTexts[FindSpawnPointIndex(npcSpawnPoints[npcIndex])].text = nPCNames[npcIndex];

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

            int index = seedRandom.Next(min, max);

            // If its a Specter, drop the index by one
            // Specters are buggy with AI Navigation, they keep getting stuck
            if (garageDelegator.drillers[index].name.Contains("SPECTER")) {
                index--;
            }

            vehicle = Instantiate(garageDelegator.drillers[index]);

            nPCMovements[npcIndex].haulerController = null;
            nPCMovements[npcIndex].drillTier = vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetDrillTier();

            // Indicate if changed from hauler or not
            if (npcIsHauler[npcIndex]) {
                haulers--;
                npcIsHauler[npcIndex] = false;
            }

            // Set agent type
            int agentTypeID = NavMesh.GetSettingsByIndex(4).agentTypeID;
            navMeshAgents[npcIndex].agentTypeID = agentTypeID;

            speed = vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPlayerSpeed();
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

            // Helions are too large, too buggy
            if (garageDelegator.haulers[index].name.Contains("HELION")) {
                index--;
            }

            vehicle = Instantiate(garageDelegator.haulers[index]);

            nPCMovements[npcIndex].haulerController = vehicle.GetComponent<HaulerController>();
            nPCMovements[npcIndex].haulerController.SetAsNPC();
            nPCMovements[npcIndex].haulerIndex = index;

            // Indicate if changed to hauler or not
            if (!npcIsHauler[npcIndex]) {
                haulers++;
                npcIsHauler[npcIndex] = true;
            }

            haulerController = vehicle.GetComponent<HaulerController>();

            // Set agent type
            // (width - 2) gives the right agent index for haulers
            int agentTypeID = NavMesh.GetSettingsByIndex(haulerController.width - 2).agentTypeID;
            navMeshAgents[npcIndex].agentTypeID = agentTypeID;

            speed = haulerController.GetPlayerSpeed();
        }   

        // Must set speed after setting parent
        vehicle.transform.SetParent(npcs[npcIndex].transform, false);
        nPCMovements[npcIndex].SetSpeed(speed);
        SetMapIcon(npcs[npcIndex], npcSpawnPoints[npcIndex]);

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
        return mineRenderer.FindBestMiningPosition(5, 20, new((int) pos.x, (int) pos.y), rotation, drillTier);
    }

    public Vector3 RequestNewHaulerPosition() {
        return uncollectedMaterialsDelegator.GetRandomMaterialLocation();
    }

    public void SetMapIcon(GameObject vehicleParent, Vector3 spawnPoint) {
        GameObject mapIcon = Instantiate(mapIconPrefab);
        mapIcon.transform.SetParent(vehicleParent.transform, false);
        mapIcon.GetComponent<SpriteRenderer>().sprite = mapIcons[FindSpawnPointIndex(spawnPoint)];
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

        navMeshAgents = new NavMeshAgent[npcCount];

        StartCoroutine(PrepareGame());
    }

    private IEnumerator PrepareGame() {
        yield return new WaitUntil(() => mineRenderer.coopMineLoaded);

        PlacePlayer();
        highestDrillTier = playerState.GetHighestDrillTier();

        playerRebirths = playerState.GetRebirths();

        for (int i = 0; i != npcCount; i++) {
            CreateNPC(i);
        }

        Time.timeScale = 5;
        // 7 real seconds
        yield return new WaitForSeconds(35);

        Time.timeScale = 1;

        Debug.Log("Scattered NPCs!");

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

            if (uncollectedMaterialsDelegator.materialCount > Get1HaulerThreshold() && haulers < 1) {
                yield return SwitchDrillerToHauler();
            } 
            else if (uncollectedMaterialsDelegator.materialCount > Get2HaulerThreshold() && haulers < 2) {
                yield return SwitchDrillerToHauler();
            }

            // Remove npcs if they are done playing
            for (int i = 0; i != npcCount; i++) {
                nPCTimeRemaining[i] -= sessionUpdateTimer;

                if (nPCTimeRemaining[i] < 0 && npcs[i] != null) {
                    RemoveNPC(i);
                }
            }

            // Fill empty NPC spot every 1.5 mins
            nPCEmptyTimer += sessionUpdateTimer;

            if (nPCEmptyTimer > 90) {

                for (int i = 0; i != npcCount; i++) {

                    if (npcs[i] == null) {
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

            highestDrillTier = playerState.GetHighestDrillTier();
        }
    }

    private IEnumerator SwitchDrillerToHauler() {

        int npcToChange;
        bool activeNPC;

        do {
            npcToChange = random.Next(0, 3);

            activeNPC = nPCMovements[npcToChange] != null;

        } 
        while(npcIsHauler[npcToChange] && !activeNPC);

        nPCMovements[npcToChange].stopMoving = true;
        yield return new WaitForSeconds(2);

        Destroy(npcs[npcToChange]);
        CreateNPC(npcToChange, false, false);
    }

    private IEnumerator SwitchHaulerToDriller(int npcToChange) {

        nPCMovements[npcToChange].stopMoving = true;
        yield return new WaitForSeconds(2);

        Destroy(npcs[npcToChange]);
        CreateNPC(npcToChange, true, false);
    }

    public IEnumerator SwitchToAnotherHauler(int npcIndex, int haulerIndex) {

        if (switchingVehicle) {
            yield break;
        }

        switchingVehicle = true;

        int index = haulerIndex - 1;

        if (index < 4) {
            index = 4;
        }

        nPCMovements[npcIndex].stopMoving = true;

        yield return new WaitForSeconds(2);

        nPCMovements[npcIndex] = null;
        Destroy(npcs[npcIndex]);

        CreateNPC(npcIndex, false, false, index);

        switchingVehicle = false;

        yield break;
    }

    public int Get1HaulerThreshold() {
        return 300 + (100 * highestDrillTier);
    }

    public int Get2HaulerThreshold() {
        return 400 + (100 * highestDrillTier);
    }

    public void RemoveNPC(int npcIndex) {
        int spawnIndex = FindSpawnPointIndex(npcSpawnPoints[npcIndex]);

        Destroy(npcs[npcIndex]);
        npcs[npcIndex] = null;

        spawnPointNameTexts[spawnIndex].text = "";
        spawnPointTaken[spawnIndex] = false;
    }

    public void SaveData(ref GameData data) {

    }

    public void CheckIfHaulingNeeded(int npcIndex) {
        bool needed = false;

        if (uncollectedMaterialsDelegator.materialCount > Get2HaulerThreshold()) {
            needed = true;
        } 
        else if (uncollectedMaterialsDelegator.materialCount > Get1HaulerThreshold()) {
            // This should be 2, but setting to 1 lets another npc potentially haul instead
            if (haulers >= 1) {
                needed = false;
            } 
            else {
                needed = true;
            }
        }

        if (needed) {
            return;
        }

        StartCoroutine(SwitchHaulerToDriller(npcIndex));
    }
}