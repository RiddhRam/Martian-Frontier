using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour, IDataPersistence
{
    [SerializeField]
    private GameObject npcPrefab;

    [SerializeField]
    private GameObject testDriller;
    [SerializeField]
    private GameObject testHauler;

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

    public Vector2[] npcJoysticks;

    [SerializeField]
    private MineRenderer mineRenderer;

    System.Random random = new();
    // Reused multiple times
    NPCMovement nPCMovement;

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

    public void CreateNPC(int npcIndex) {
        int number = random.Next(0, 20);

        // 5% chance of player not spawning
        if (number < 1) {
            return;
        }

        npcs[npcIndex] = Instantiate(npcPrefab);
        npcs[npcIndex].name = "NPC " + npcIndex;

        //navMeshAgents[npcIndex] = npcs[npcIndex].GetComponent<NavMeshAgent>();
        nPCMovement = npcs[npcIndex].GetComponent<NPCMovement>();
        nPCMovement.npcIndex = npcIndex;
        nPCMovement.nPCManager = this;
        nPCMovement.rebirthLevel = random.Next(0, 10);

        npcSpawnPoints[npcIndex] = GetSpawnPoint();

        GameObject vehicle = Instantiate(testDriller);
        vehicle.transform.SetParent(npcs[npcIndex].transform, false);

        SetMapIcon(npcs[npcIndex], npcSpawnPoints[npcIndex]);

        npcs[npcIndex].transform.position = npcSpawnPoints[npcIndex];
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
        npcs = new GameObject[spawnPoints.Length - 1];
        npcSpawnPoints = new Vector3[spawnPoints.Length - 1];
        npcJoysticks = new Vector2[spawnPoints.Length - 1];

        PlacePlayer();

        for (int i = 0; i != spawnPoints.Length - 1; i++) {
            CreateNPC(i);
            ResetNPCPos(i);
        }

        StartCoroutine(ScatterNPCs());
    }

    private IEnumerator ScatterNPCs() {
        yield return new WaitUntil(() => mineRenderer.coopMineLoaded);
        Debug.Log("Scattered NPCs!");
    }

    public void SaveData(ref GameData data) {

    }

}