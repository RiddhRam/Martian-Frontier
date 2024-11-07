using System;
using UnityEngine;

public class PlayerVehicleDelegation : MonoBehaviour
{
    private Boolean currentlyDriller = true;

    public void SwitchVehicle(Boolean driller) {

        if (currentlyDriller) {
            // Switch to drill
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
        } else {
            // Switch to haul
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
        }

        currentlyDriller = !currentlyDriller;
    }

}
