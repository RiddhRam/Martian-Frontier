using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerVehicleDelegation : MonoBehaviour, IDataPersistence
{
    public GameObject cargoInfo;
    public GameObject UI;
    public string currentVehicle;
    public string currentCoopVehicle;
    public GameObject playerVehicle;
    public string vehicleType;
    private bool loading = false;
    private Vector3 loadPlayerPos;
    private float loadRotate;
    public MineRenderer mineRenderer;
    public AdDelegator adDelegator;
    public AnalyticsDelegator analyticsDelegator;
    public NPCManager nPCManager;
    public GarageDelegator garageDelegator;
    private bool notSinglePlayerScene = false;

    public void SwitchVehicle(GameObject newVehicle) {

        GameObject oldVehicle = transform.GetChild(0).gameObject;

        if (newVehicle.name == oldVehicle.name && !loading) {
            // User is already in this vehicle, do nothing
            return;
        }
        loading = false;

        HaulerController haulerController1 = oldVehicle.GetComponent<HaulerController>();

        if (haulerController1) {
            int[] materialCount = haulerController1.GetMaterialCount();

            for (int i = 0; i != materialCount.Length; i++) {
                // Should never be less than zero but just in case
                if (materialCount[i] <= 0) {
                    continue;
                }

                mineRenderer.GetMaterialObject(i, transform.position, materialCount[i], haulerController1.GetProfitMultiplier());
            }
        }

        // Reset PlayerVehicle by removing the current vehicle, and resetting the vehicle position and rotation
        Destroy(oldVehicle);

        playerVehicle = Instantiate(newVehicle);
        playerVehicle.transform.SetParent(transform);
        playerVehicle.transform.SetAsFirstSibling();
        // Create the new vehicle using the prefab and set it's parent to PlayerVehicle (the gameobjet of this script)
        playerVehicle.transform.localPosition = new(0, 0, 0);

        // Remove (Clone) from the name
        playerVehicle.name = playerVehicle.name[..^7];

        if (!notSinglePlayerScene) {
            transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
            // The z rotation initially starts at 180, but when we switch we use 0
            playerVehicle.transform.rotation = Quaternion.Euler(0, 0, 0);
            currentVehicle = playerVehicle.name;
        } else {
            playerVehicle.transform.rotation = Quaternion.Euler(0, 0, 270);
            currentCoopVehicle = playerVehicle.name;
        }

        if (nPCManager) {
            nPCManager.ResetPlayerPos();
        }
        
        float playerSpeed;
        
        HaulerController haulerController2 = playerVehicle.GetComponent<HaulerController>();
        
        // All haulers will have this script, if the vehicle doesn't have this, it's not a hauler
        if (haulerController2) {
            // Display the hauler cargo button
            cargoInfo.SetActive(true);
            UI.GetComponent<UIDelegation>().ToggleCargoInfo(true);
            playerSpeed = haulerController2.GetPlayerSpeed();
            playerSpeed = UpdateOriginalSpeed(playerSpeed);
            gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);

            vehicleType = "Hauler";

            haulerController2.SetProfitMultiplier(garageDelegator.GetVehicleProfitMultiplier(haulerController2.name));
            

            analyticsDelegator.SelectVehicle(playerVehicle.name, "Hauler", 0);
            return;
        }

        // If not a hauler, hide the hauler cargo button
        cargoInfo.SetActive(false);
        UI.GetComponent<UIDelegation>().ToggleCargoInfo(false);
        DrillerController drillerController = playerVehicle.transform.GetChild(1).GetComponent<DrillerController>();
        playerSpeed = drillerController.GetPlayerSpeed();
        playerSpeed = UpdateOriginalSpeed(playerSpeed);
        gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);

        vehicleType = "Driller";
        
        drillerController.SetProfitMultiplier(garageDelegator.GetVehicleProfitMultiplier(drillerController.transform.parent.gameObject.name));
       
       analyticsDelegator.SelectVehicle(playerVehicle.name, "Driller", drillerController.GetDrillTier());
    }

    public void LoadData(GameData data) {
        this.currentCoopVehicle = data.currentCoopVehicle;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
            FindVehicle(currentCoopVehicle, null, null);
            return;
        }
        
        // Load the vehicle name
        // We need the last vehicle pos and rotation too, just for now though
        this.currentVehicle = data.currentVehicle;
        this.loadPlayerPos = data.playerPos;
        this.loadRotate = data.playerRotation;
        // Hauler cargo is loaded lower down
        // It's saved to this temp variable because otherwise it magically gets wiped I don't know how
        int[] tempHaulerCargo = data.haulerCargo;
        float[] tempMaterialProfitMultipliers = data.materialProfitMultipliers;

        // Bypasses first if statement in SwitchVehicle
        loading = true;
        FindVehicle(currentVehicle, tempHaulerCargo, tempMaterialProfitMultipliers);
    }

    // ONLY USED WHEN LOADING
    public void FindVehicle(string vehicleName, int[] tempHaulerCargo, float[] tempMaterialProfitMultipliers) {
        // Iterate through all vehicles and find which vehicle it is
        // First check if user used a hauler
        // Most likely did since a user would probably leave after making some money
        GameObject[] haulers = garageDelegator.GetHaulers();
        for (int i = 0; i != haulers.Length; i++) {
            if (vehicleName != haulers[i].name) {
                continue;
            }

            // Switch to that vehicle
            SwitchVehicle(haulers[i]);
            
            if (!notSinglePlayerScene) {
                HaulerController haulerController = playerVehicle.GetComponent<HaulerController>();
                haulerController.SetMaterialProfitMultipliers(tempMaterialProfitMultipliers);
                haulerController.SetMaterialCount(tempHaulerCargo);
                playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
            }
            
            return;
        }

        // If wasn't a hauler then it's a driller
        GameObject[] drillers = garageDelegator.GetDrillers();
        for (int i = 0; i != drillers.Length; i++) {
            if (vehicleName != drillers[i].name) {
                continue;
            }

            SwitchVehicle(drillers[i]);
            if (!notSinglePlayerScene) {
                playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
            }

            break;
        }
    }

    public void SaveData(ref GameData data) {

        data.currentCoopVehicle = this.currentCoopVehicle;

        if (notSinglePlayerScene) {
            return;
        }

        data.currentVehicle = this.currentVehicle;

        if (!playerVehicle) {
            data.haulerCargo = new int[9];
            return;
        }

        data.playerPos = playerVehicle.transform.parent.position;
        data.playerRotation = playerVehicle.transform.parent.rotation.eulerAngles.z;

        HaulerController haulerController = playerVehicle.GetComponent<HaulerController>();
        if (haulerController) {
            data.haulerCargo = haulerController.GetMaterialCount();
            data.materialProfitMultipliers = haulerController.GetMaterialProfitMultipliers();
        } else {
            data.haulerCargo = new int[9];
            data.materialProfitMultipliers = new float[9];
        }
        
    }

    private float UpdateOriginalSpeed(float playerSpeed) {
        if (adDelegator.speedBoostActive) {
            adDelegator.originalSpeed = playerSpeed;
            playerSpeed *= 1.5f;
        }

        return playerSpeed;
    }

}
