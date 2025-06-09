using UnityEngine;

public class GamemodePad : MonoBehaviour
{
    [SerializeField] UIDelegation uIDelegation;
    [SerializeField] TutorialManager tutorialManager;
    JoystickMovement joystickMovement;
    [SerializeField] GameObject gamemodeScreen;

    void Awake()
    {
        joystickMovement = JoystickMovement.Instance;
    }

    void OnTriggerEnter2D(Collider2D collision) {

        if (tutorialManager && !tutorialManager.finishedTutorial) {
            uIDelegation.ShowError("FINISH THE TUTORIAL FIRST!");
            return;
        }

        // Only the drill/hauler can activate this pad, not the body
        // Only the player vehicle can open the UI panel on their local game
        if (!(collision.GetComponent<DrillerController>() || collision.GetComponent<BlasterDriller>() || collision.GetComponent<CreditMagnet>()) || !collision.transform.parent.parent.name.Contains("Player Vehicle")) {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;

        uIDelegation.HideAll();
        uIDelegation.RevealElement(gamemodeScreen);

        // Stops player from moving
        joystickMovement.joystickVec = new();
    }
}