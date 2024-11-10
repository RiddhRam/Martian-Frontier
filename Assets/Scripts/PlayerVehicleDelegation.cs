using UnityEngine;

public class PlayerVehicleDelegation : MonoBehaviour
{
    public GameObject cargoButton;
    public GameObject UI;

    public void SwitchVehicle(GameObject newVehicle) {

        if (newVehicle.name == transform.GetChild(0).gameObject.name || newVehicle.name + "(Clone)" == transform.GetChild(0).gameObject.name) {
            // User is already in this vehicle, do nothing
            return;
        }

        // Reset PlayerVehicle by removing the current vehicle, and resetting the vehicle position and rotation
        Destroy(transform.GetChild(0).gameObject);
        transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));

        // Create the new vehicle using the prefab and set it's parent to PlayerVehicle (the gameobjet of this script)
        GameObject newPlayerVehicle = Instantiate(newVehicle);
        newPlayerVehicle.transform.SetParent(transform);
        newPlayerVehicle.transform.localPosition = new(0, 0, 0);
        // The z rotation initially starts at 180, but when we switch we use 0
        newPlayerVehicle.transform.rotation = Quaternion.Euler(0, 0, 0);

        // All haulers will have this script, if the vehicle doesn't have this, it's not a hauler
        if (newPlayerVehicle.GetComponent<HaulerController>()) {
            // Display the hauler cargo button
            cargoButton.SetActive(true);
            UI.GetComponent<UIDelegation>().UpdatePrimaryElements();
            return;
        }

        // If not a hauler, hide the hauler cargo button
        cargoButton.SetActive(false);
        UI.GetComponent<UIDelegation>().UpdatePrimaryElements();
    }

}
