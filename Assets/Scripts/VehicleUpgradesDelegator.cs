using UnityEngine;

public class VehicleUpgradesDelegator : MonoBehaviour, IDataPersistence
{
    public SerializableDictionary<string, int> vehicleUpgradeLevels;

    public PlayerVehicleDelegation playerVehicleDelegation;
    public GameObject playerVehicle;

    public void LoadData(GameData data)
    {
        this.vehicleUpgradeLevels = data.vehicleUpgradeLevels;

        if (playerVehicleDelegation.vehicleType == "Driller") {
            DrillerController drillerController = playerVehicle.transform.GetChild(0).GetChild(1).GetComponent<DrillerController>();
            drillerController.SetProfitMultiplier(GetVehicleProfitMultiplier(drillerController.name));
            return;
        }

        HaulerController haulerController = playerVehicle.transform.GetChild(0).GetComponent<HaulerController>();
        haulerController.SetProfitMultiplier(GetVehicleProfitMultiplier(haulerController.name));
    }

    public void SaveData(ref GameData data)
    {
        data.vehicleUpgradeLevels = this.vehicleUpgradeLevels;
    }

    public int GetVehicleLevel(string vehicleName) {
        if (vehicleUpgradeLevels.ContainsKey(vehicleName)) {
            return vehicleUpgradeLevels[vehicleName];
        }

        return 1;
    }

    public int GetVehicleProfitMultiplier(string vehicleName) {
        if (vehicleUpgradeLevels.ContainsKey(vehicleName)) {
            return (vehicleUpgradeLevels[vehicleName] / 200);
        }

        return (1 / 200);
    }
}