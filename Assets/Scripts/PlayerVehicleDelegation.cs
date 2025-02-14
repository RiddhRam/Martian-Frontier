using UnityEngine;

public class PlayerVehicleDelegation : MonoBehaviour, IDataPersistence
{
    public GameObject cargoInfo;
    public GameObject UI;
    public string currentVehicle;
    public GameObject garageDelegator;
    public GameObject playerVehicle;
    public string vehicleType;
    private bool loading = false;
    private Vector3 loadPlayerPos;
    private float loadRotate;
    public MineRenderer mineRenderer;
    public AdDelegator adDelegator;
    public AnalyticsDelegator analyticsDelegator;
    public VehicleUpgradesDelegator vehicleUpgradesDelegator;

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

        transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));

        // Create the new vehicle using the prefab and set it's parent to PlayerVehicle (the gameobjet of this script)
        playerVehicle = Instantiate(newVehicle);
        playerVehicle.transform.SetParent(transform);
        playerVehicle.transform.SetAsFirstSibling();
        playerVehicle.transform.localPosition = new(0, 0, 0);
        // The z rotation initially starts at 180, but when we switch we use 0
        playerVehicle.transform.rotation = Quaternion.Euler(0, 0, 0);
        // Remove (Clone) from the name
        playerVehicle.name = playerVehicle.name[..^7];
        currentVehicle = playerVehicle.name;

        float playerSpeed;
        
        HaulerController haulerController2 = playerVehicle.GetComponent<HaulerController>();
        
        // All haulers will have this script, if the vehicle doesn't have this, it's not a hauler
        if (haulerController2) {
            // Disable rewarded ad vision boost button
            adDelegator.SetUsingDriller(false);
            // Display the hauler cargo button
            cargoInfo.SetActive(true);
            UI.GetComponent<UIDelegation>().ToggleCargoInfo(true);
            playerSpeed = haulerController2.GetPlayerSpeed();
            playerSpeed = UpdateOriginalSpeed(playerSpeed);
            gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);
            gameObject.GetComponent<AIMovement>().vehicleType = "Hauler";
            haulerController2.SetProfitMultiplier(vehicleUpgradesDelegator.GetVehicleProfitMultiplier(haulerController2.name));
            vehicleType = "Hauler";

            analyticsDelegator.SelectVehicle(playerVehicle.name, "Hauler", 0);
            return;
        }

        // Enable rewarded ad vision boost button
        adDelegator.SetUsingDriller(true);
        // If not a hauler, hide the hauler cargo button
        cargoInfo.SetActive(false);
        UI.GetComponent<UIDelegation>().ToggleCargoInfo(false);
        DrillerController drillerController = playerVehicle.transform.GetChild(1).GetComponent<DrillerController>();
        playerSpeed = drillerController.GetPlayerSpeed();
        playerSpeed = UpdateOriginalSpeed(playerSpeed);
        gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);
        drillerController.SetProfitMultiplier(vehicleUpgradesDelegator.GetVehicleProfitMultiplier(drillerController.transform.parent.gameObject.name));

        int tier = drillerController.GetDrillTier();
        gameObject.GetComponent<AIMovement>().vehicleType = "Driller";
        gameObject.GetComponent<AIMovement>().drillTier = tier;
        vehicleType = "Driller";

        analyticsDelegator.SelectVehicle(playerVehicle.name, "Driller", tier);
    }

    public void LoadData(GameData data) {
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
        
        // Iterate through all vehicles and find which vehicle it is
        GarageDelegator garageDelegatorScript = garageDelegator.GetComponent<GarageDelegator>();

        // First check if user used a hauler
        // Most likely did since a user would porbably leave after making some money
        GameObject[] haulers = garageDelegatorScript.GetHaulers();
        for (int i = 0; i != haulers.Length; i++) {
            if (currentVehicle != haulers[i].name) {
                continue;
            }

            // Switch to that vehicle
            SwitchVehicle(haulers[i]);
            HaulerController haulerController = playerVehicle.GetComponent<HaulerController>();
            haulerController.SetMaterialProfitMultipliers(tempMaterialProfitMultipliers);
            haulerController.SetMaterialCount(tempHaulerCargo);
            playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
            return;
        }

        // If wasn't a hauler then it's a driller
        GameObject[] drillers = garageDelegatorScript.GetDrillers();
        for (int i = 0; i != drillers.Length; i++) {
            if (currentVehicle != drillers[i].name) {
                continue;
            }

            SwitchVehicle(drillers[i]);
            playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
            break;
        }
    }

    public void SaveData(ref GameData data) {
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
