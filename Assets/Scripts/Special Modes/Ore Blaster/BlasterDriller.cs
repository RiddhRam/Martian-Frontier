using System.Collections;
using UnityEngine;

public class BlasterDriller : MonoBehaviour
{

    void Start() {
        // Do this so front wheels are found
        PlayerMovement playerMovement = transform.parent.GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(playerMovement.GetSpeed());
    }

}