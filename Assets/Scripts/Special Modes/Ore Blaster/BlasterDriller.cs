using System.Collections;
using UnityEngine;

public class BlasterDriller : MonoBehaviour
{

    public Canvas sliderCanvas;
    private readonly Quaternion normalRotation = Quaternion.Euler(0, 0, 0);

    void Start() {
        // Do this so front wheels are found
        PlayerMovement playerMovement = transform.parent.GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(playerMovement.GetSpeed());
        
        StartCoroutine(HoldPlayerCardStill());
    }

    private IEnumerator HoldPlayerCardStill() {

        while (true) {
            sliderCanvas.transform.rotation = normalRotation;
            float angle = Mathf.Deg2Rad * transform.parent.eulerAngles.z; // Get the Y-axis rotation

            // Calculate new position based on rotation
            float x = Mathf.Sin(angle) * 4.2f;
            float y = Mathf.Cos(angle) * 4.2f;

            sliderCanvas.transform.localPosition = new Vector3(x, y, 0);

            yield return null;
        }
    }

}