using UnityEngine;
using TMPro; // Use TextMeshPro if you're using TextMeshPro

public class BlasterDriller : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditCounterText;

    public int collectedCredits;

    private AudioSource vehicleSoundEffects;
    private AudioClip orePickUpSoundEffect;
    private AudioDelegator audioDelegator;

    void Start() {
        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        orePickUpSoundEffect = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().oreSaleSoundEffect;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();

        // Do this so front wheels are found
        PlayerMovement playerMovement = transform.parent.GetComponent<PlayerMovement>();
        playerMovement.SetSpeed(playerMovement.GetSpeed());
    }

    void FixedUpdate()
    {
        
    }

    public void UpdateCreditCount(int newAmount) {
        collectedCredits += newAmount;
        creditCounterText.text = collectedCredits.ToString();
    }

}