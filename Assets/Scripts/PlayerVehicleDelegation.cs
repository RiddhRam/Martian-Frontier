using UnityEngine;

public class PlayerVehicleDelegation : MonoBehaviour, IDataPersistence
{
    public GameObject cargoButton;
    public GameObject UI;
    public string currentVehicle;
    public GameObject garageDelegator;
    public GameObject playerVehicle;
    private bool loading = false;
    private Vector3 loadPlayerPos;
    private float loadRotate;

    public void SwitchVehicle(GameObject newVehicle) {

        if (newVehicle.name == transform.GetChild(0).gameObject.name && !loading) {
            // User is already in this vehicle, do nothing
            return;
        }
        loading = false;

        // Reset PlayerVehicle by removing the current vehicle, and resetting the vehicle position and rotation
        Destroy(transform.GetChild(0).gameObject);
        transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));

        // Create the new vehicle using the prefab and set it's parent to PlayerVehicle (the gameobjet of this script)
        playerVehicle = Instantiate(newVehicle);
        playerVehicle.transform.SetParent(transform);
        playerVehicle.transform.localPosition = new(0, 0, 0);
        // The z rotation initially starts at 180, but when we switch we use 0
        playerVehicle.transform.rotation = Quaternion.Euler(0, 0, 0);
        // Remove (Clone) from the name
        playerVehicle.name = playerVehicle.name[..^7];
        currentVehicle = playerVehicle.name;

        float playerSpeed;
        
        // All haulers will have this script, if the vehicle doesn't have this, it's not a hauler
        if (playerVehicle.GetComponent<HaulerController>()) {
            // Display the hauler cargo button
            cargoButton.SetActive(true);
            UI.GetComponent<UIDelegation>().ToggleCargoButton(true);
            playerSpeed = playerVehicle.GetComponent<HaulerController>().GetPlayerSpeed();
            gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);
            return;
        }

        // If not a hauler, hide the hauler cargo button
        cargoButton.SetActive(false);
        UI.GetComponent<UIDelegation>().ToggleCargoButton(false);
        playerSpeed = playerVehicle.transform.GetChild(1).GetComponent<DrillerController>().GetPlayerSpeed();
        gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);
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
            playerVehicle.GetComponent<HaulerController>().SetMaterialCount(tempHaulerCargo);
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
            data.haulerCargo = new int[3];

            return;
        }

        data.playerPos = playerVehicle.transform.parent.position;
        data.playerRotation = playerVehicle.transform.parent.rotation.eulerAngles.z;

        if (playerVehicle.GetComponent<HaulerController>()) {
            data.haulerCargo = playerVehicle.GetComponent<HaulerController>().GetMaterialCount();
        } else {
            data.haulerCargo = new int[3];
        }
        
    }

}
