using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerVehicleDelegation : MonoBehaviour, IDataPersistence
{
    [Header("Vehicles")]
    public GameObject[] drillers;
    public string currentVehicle;
    public string currentCoopVehicle;
    public GameObject playerVehicle;
    private bool loading = false;
    private Vector3 loadPlayerPos;
    private float loadRotate;

    [Header("Other Scripts")]
    private AdDelegator adDelegator;
    private AnalyticsDelegator analyticsDelegator;
    public NPCManager nPCManager;
    public VehicleUpgradeBayManager vehicleUpgradeBayManager;
    public RefineryUpgradePad refineryUpgradePad;
    public bool loaded = false;

    private bool notSinglePlayerScene = false;

    // For tutorial
    public bool firstTimePlaying = false;
    private float speedBoostAmount = 1.2f;

    [Header("Visual")]
    [SerializeField] private Slider slider;

    void Awake()
    {
        adDelegator = AdDelegator.Instance;
        analyticsDelegator = AnalyticsDelegator.Instance;
    }

    public void SwitchVehicle(GameObject newVehicle) {

        GameObject oldVehicle = transform.GetChild(0).gameObject;

        if (newVehicle.name == oldVehicle.name && !loading) {
            // User is already in this vehicle, do nothing
            if (!notSinglePlayerScene) {
                currentVehicle = oldVehicle.name;
            } else {
                currentCoopVehicle = oldVehicle.name;
            }
            return;
        }
        loading = false;

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
            transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(0, 0, 180));
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

        DrillerController drillerController = playerVehicle.transform.GetChild(1).GetComponent<DrillerController>();
        playerSpeed = drillerController.GetPlayerSpeed();
        playerSpeed = UpdateOriginalSpeed(playerSpeed);
        // Speed boost to new players
        if (firstTimePlaying) {
            playerSpeed *= speedBoostAmount;
        }
        gameObject.GetComponent<PlayerMovement>().SetSpeed(playerSpeed);
        
        drillerController.playerVehicleDelegation = this;

        vehicleUpgradeBayManager.drillerController = drillerController;

        // In production this loads before the upgrade bay for some reason, whichever loads second should call the function
        if (vehicleUpgradeBayManager.loaded)
        {
            vehicleUpgradeBayManager.MatchPlayerDrillToDrill();
        }
       
        analyticsDelegator.SelectVehicle(playerVehicle.name, "Driller", drillerController.GetDrillTier());
    }

    public void LoadData(GameData data)
    {

        this.currentCoopVehicle = data.currentCoopVehicle;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op"))
        {
            notSinglePlayerScene = true;
            FindVehicle(currentCoopVehicle);
            loaded = true;
            return;
        }

        if (!data.finishedTutorial)
        {
            firstTimePlaying = true;
        }

        // Load the vehicle name
        // We need the last vehicle pos and rotation too, just for now though
        this.currentVehicle = data.currentVehicle;
        this.loadPlayerPos = data.playerPos;
        this.loadRotate = data.playerRotation;

        // Bypasses first if statement in SwitchVehicle
        loading = true;
        FindVehicle(currentVehicle);
        loaded = true;

        // Set next vehicle to be the drill after the current one, or the first drill if this is the last drill
        int nextIndex = GetNextVehicleIndex(data.mineCount);
        refineryUpgradePad.SetProceedPanelVehicle(vehicleUpgradeBayManager.drillUIPositions[nextIndex]);
    }

    // ONLY USED WHEN LOADING
    // Returns the index of the vehicle, and switches vehicle automatically
    public int FindVehicle(string vehicleName, bool switchVehicle = true)
    {

        (string secondaryName, bool checkSecondaryName) = GetMergedVehicleName(vehicleName);

        // Iterate through all vehicles and find which vehicle it is

        for (int i = 0; i != drillers.Length; i++)
        {
            if (!vehicleName.Contains(drillers[i].name))
            {
                if (!(checkSecondaryName && vehicleName.Contains(secondaryName)))
                {

                    continue;
                }
            }

            if (switchVehicle)
            {
                SwitchVehicle(drillers[i]);
                if (!notSinglePlayerScene)
                {
                    playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
                }
            }
            

            return i;
        }

        // If it reaches here, no vehicle was found, so we just set the player to use the first drill
        if (switchVehicle)
        {
            SwitchVehicle(drillers[0]);
            if (!notSinglePlayerScene)
            {
                playerVehicle.transform.parent.SetPositionAndRotation(loadPlayerPos, Quaternion.Euler(0, 0, loadRotate));
            }
        }
        
        return 0;
    }

    public int GetNextVehicleIndex(int mineCount)
    {
        // No need to do mineCount + 1, because mineCount is not zero indexed
        return (mineCount) % vehicleUpgradeBayManager.drillUIPositions.Length;
    }

    // Check to see if this vehicle was merged into another in a previous update
    public (string secondaryName, bool checkSecondaryName) GetMergedVehicleName(string vehicleName)
    {

        // Neither of these 4 are in the game anymore, just here as a demonstration
        if (vehicleName.Contains("TURBO TANKER"))
        {
            return ("HEAVY", true);
        }
        else if (vehicleName.Contains("HEAVY"))
        {
            return ("TURBO TANKER", true);
        }
        else if (vehicleName.Contains("DASH"))
        {
            return ("STUBBY", true);
        }
        else if (vehicleName.Contains("STUBBY"))
        {
            return ("DASH", true);
        }

        return ("", false);
    }

    public void SaveData(ref GameData data) {

        data.currentCoopVehicle = this.currentCoopVehicle;

        if (notSinglePlayerScene) {
            return;
        }

        data.currentVehicle = this.currentVehicle;

        if (!playerVehicle) {
            return;
        }

        data.playerPos = playerVehicle.transform.parent.position;
        data.playerRotation = playerVehicle.transform.parent.rotation.eulerAngles.z;
    }

    private float UpdateOriginalSpeed(float playerSpeed) {
        if (firstTimePlaying) {
            playerSpeed *= speedBoostAmount;
        }

        if (adDelegator.speedBoostActive) {   
            adDelegator.originalSpeed = playerSpeed;
            
            playerSpeed *= 1.5f;
        }

        return playerSpeed;
    }

    public void UpdateOverheatSlider(float heatPercentage) {
        slider.value = heatPercentage;
    }

}
