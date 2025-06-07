using UnityEngine;

// Is also the controller for the upgrade panel
public class AutomaticMinerBay : MonoBehaviour, IDataPersistence
{
    [Header("Scripts")]
    [SerializeField] UIDelegation uIDelegation;
    public PlayerState playerState;
    JoystickMovement joystickMovement;
    public MineRenderer mineRenderer;

    [Header("Audio")]
    [SerializeField] AudioClip oreUpgradeSound;
    [SerializeField] AudioSource oreSoundEffectsSource;

    [Header("UI")]
    public GameObject autoMinerScreen;

    void OnTriggerEnter2D(Collider2D collision)
    {

        // Only the Player Trigger trigger can activate the pad, not the body or drill
        // Only the player vehicle can open the UI panel on their local game
        if (collision.name != "Player Trigger" || !collision.transform.parent.parent.name.Contains("Player Vehicle"))
        {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;


        uIDelegation.HideAll();
        uIDelegation.RevealElement(autoMinerScreen);

        // Stops player from moving
        joystickMovement.joystickVec = new();
    }

    public void LoadData(GameData data)
    {
        if (data.mineCount < 2)
        {
            // Only enabled after the first level
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(false);
    }

    public void SaveData(ref GameData data)
    {
        if (data.mineCount < 2)
        {
            // Only enabled after the first level
            return;
        }
    }
}