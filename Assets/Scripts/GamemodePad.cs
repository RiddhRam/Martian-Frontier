using UnityEngine;

public class GamemodePad : MonoBehaviour
{
    [SerializeField] UIDelegation uIDelegation;
    [SerializeField] JoystickMovement joystickMovement;
    [SerializeField] GameObject gamemodeScreen;

    void OnTriggerEnter2D(Collider2D collision) {
        // Only the player vehicle can open the UI panel on their local game
        // Also only the drill can activate this pad, not the body
        if (!collision.transform.parent.parent.name.Contains("Player Vehicle") || !collision.GetComponent<DrillerController>()) {
            return;
        }

        uIDelegation.HideAll();
        uIDelegation.RevealElement(gamemodeScreen);

        // Stops player from moving
        joystickMovement.joystickVec = new();
    }
}