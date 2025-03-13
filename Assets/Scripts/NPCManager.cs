using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour, IDataPersistence
{
    [SerializeField]
    private GameObject npcPrefab;

    [SerializeField]
    private Transform[] spawnPoints;
    private bool[] spawnPointTaken;
    [SerializeField]
    private Sprite[] mapIcons;
    [SerializeField]
    public GameObject mapIconPrefab;

    [SerializeField]
    private Transform playerVehicle;
    private Vector3 playerSpawnPoint;

    private GameObject[] npcs;
    private NavMeshAgent[] navMeshAgents;
    private Vector3[] npcSpawnPoints;
    private NPCMovement[] nPCMovements;

    private int highestDrillTier = 0;
    private int haulers = 0;

    private bool[] npcIsHauler;


    [SerializeField]
    private MineRenderer mineRenderer;
    [SerializeField]
    private UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    [SerializeField]
    private PlayerState playerState;
    [SerializeField]
    private GarageDelegator garageDelegator;

    private readonly int[] drillerTierThresholds = {4, 10, 16};
    private readonly int[] haulerThresholds = {8, 12, 18};

    System.Random random = new();

    // Cache
    HaulerController haulerController;

    public void PlacePlayer() {
        int index = random.Next(0, spawnPoints.Length);

        playerSpawnPoint = spawnPoints[index].position;
        spawnPointTaken[index] = true;

        SetMapIcon(playerVehicle.gameObject, playerSpawnPoint);

        ResetPlayerPos();
    }

    public void ResetPlayerPos() {
        playerVehicle.position = playerSpawnPoint;
        playerVehicle.eulerAngles = new(0, 0, 90);
        playerVehicle.GetChild(0).eulerAngles = new(0, 0, 270);
    }

    public void CreateNPC(int npcIndex, bool driller = true) {
        int number = random.Next(0, 20);

        // 5% chance of player not spawning
        if (number < 1) {
            return;
        }

        npcs[npcIndex] = Instantiate(npcPrefab);
        npcs[npcIndex].name = "NPC " + npcIndex;

        nPCMovements[npcIndex] = npcs[npcIndex].GetComponent<NPCMovement>();
        nPCMovements[npcIndex].npcIndex = npcIndex;
        nPCMovements[npcIndex].nPCManager = this;
        nPCMovements[npcIndex].rebirthLevel = random.Next(0, 10);

        navMeshAgents[npcIndex] = nPCMovements[npcIndex].agent;

        npcSpawnPoints[npcIndex] = GetSpawnPoint();

        GameObject vehicle;

        // Agent types and indexes: Humanoid (0), Width 3 (1), Width 4 (2), Width 5 (3), Driller (4)
        float speed;
        if (driller) {

            int index = random.Next(0, drillerTierThresholds[highestDrillTier - 1]);
            vehicle = Instantiate(garageDelegator.drillers[index]);

            if (npcIsHauler[npcIndex]) {
                haulers--;
                npcIsHauler[npcIndex] = false;
            }

            int agentTypeID = NavMesh.GetSettingsByIndex(4).agentTypeID;
            navMeshAgents[npcIndex].agentTypeID = agentTypeID;

            speed = vehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPlayerSpeed();
        } else {

            int index = random.Next(4, haulerThresholds[highestDrillTier - 1]);
            vehicle = Instantiate(garageDelegator.haulers[index]);

            if (!npcIsHauler[npcIndex]) {
                haulers++;
                npcIsHauler[npcIndex] = true;
            }

            haulerController = vehicle.GetComponent<HaulerController>();

            // Width - 2 gives the right agent index
            int agentTypeID = NavMesh.GetSettingsByIndex(haulerController.width-2).agentTypeID;
            navMeshAgents[npcIndex].agentTypeID = agentTypeID;

            speed = haulerController.GetPlayerSpeed();
        }   

        // Must set speed after setting parent
        vehicle.transform.SetParent(npcs[npcIndex].transform, false);
        nPCMovements[npcIndex].SetSpeed(speed);
        SetMapIcon(npcs[npcIndex], npcSpawnPoints[npcIndex]);
        npcs[npcIndex].transform.position = npcSpawnPoints[npcIndex];

        Vector3 position = RequestNewMiningPosition(npcSpawnPoints[npcIndex], 180);
        navMeshAgents[npcIndex].SetDestination(position);
    }

    public Vector3 RequestNewMiningPosition(Vector3 pos, float rotation) {
        return mineRenderer.FindBestMiningPosition(5, random.Next(20, 41), new((int) pos.x, (int) pos.y), rotation);
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

        return 0;
    }

    public void LoadData(GameData data) {
        spawnPointTaken = new bool[spawnPoints.Length];
        // -1 because 1 spawn point goes to the player
        npcs = new GameObject[spawnPoints.Length - 1];
        npcSpawnPoints = new Vector3[spawnPoints.Length - 1];
        npcIsHauler = new bool[spawnPoints.Length - 1];
        nPCMovements = new NPCMovement[spawnPoints.Length - 1];

        navMeshAgents = new NavMeshAgent[spawnPoints.Length - 1];

        StartCoroutine(PrepareGame());
    }

    private IEnumerator PrepareGame() {
        yield return new WaitUntil(() => mineRenderer.coopMineLoaded);

        PlacePlayer();
        highestDrillTier = playerState.GetHighestDrillTier();

        for (int i = 0; i != spawnPoints.Length - 1; i++) {
            CreateNPC(i);
            ResetNPCPos(i);
        }

        Time.timeScale = 5;
        // 7 real seconds
        yield return new WaitForSeconds(35);
        Time.timeScale = 1;

        Debug.Log("Scattered NPCs!");

        StartCoroutine(LiveManageSession());
    }

    private IEnumerator LiveManageSession() {
        while (true) {
            yield return new WaitForSeconds(1);

            if (uncollectedMaterialsDelegator.materialCount > 300 && haulers == 0) {
                yield return SwitchDrillerToHauler();
            } else if (uncollectedMaterialsDelegator.materialCount > 400 && haulers < 2) {
                while (haulers < 2) {
                    yield return SwitchDrillerToHauler();
                }
                
            }

            highestDrillTier = playerState.GetHighestDrillTier();
        }
    }

    private IEnumerator SwitchDrillerToHauler() {
        int npcToChange;
        do {
            npcToChange = random.Next(0, 3);
        } while(npcIsHauler[npcToChange]);

        nPCMovements[npcToChange].stopMoving = true;
        yield return new WaitForSeconds(2);

        Destroy(npcs[npcToChange]);
        spawnPointTaken[FindSpawnPointIndex(npcSpawnPoints[npcToChange])] = false;
        CreateNPC(npcToChange, false);

        // Should already be false again but do it just in case
        nPCMovements[npcToChange].stopMoving = false;
    }

    public void SaveData(ref GameData data) {

    }

}