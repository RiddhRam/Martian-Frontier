using UnityEngine;

public class GamemodePad : MonoBehaviour
{
    [SerializeField] TutorialManager tutorialManager;
    [SerializeField] GameObject gamemodeScreen;

    void OnTriggerEnter2D(Collider2D collision) {

        if (tutorialManager && !tutorialManager.finishedTutorial) {
            UIDelegation.Instance.ShowError("FINISH THE TUTORIAL FIRST!");
            return;
        }

        // Only the drill/hauler can activate this pad, not the body
        // Only the player vehicle can open the UI panel on their local game
        if (!(collision.GetComponent<DrillerController>() || collision.GetComponent<BlasterDriller>() || collision.GetComponent<CreditMagnet>()) || !collision.transform.parent.parent.name.Contains("Player Vehicle")) {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.linearVelocity.sqrMagnitude < 0.01f)
            return;

        UIDelegation.Instance.HideAll();
        UIDelegation.Instance.RevealElement(gamemodeScreen);

        // Stops player from moving
        JoystickMovement.Instance.joystickVec = new();
    }
}