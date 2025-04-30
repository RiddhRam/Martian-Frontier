using UnityEngine;

public class ExplosiveChargeProjectile : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private OreBlaster oreBlaster;
    [SerializeField] private ExplosionController explosionController;


    [SerializeField] private AudioDelegator audioDelegator;
    [SerializeField] private AudioSource powerUpAudioSource;
    [SerializeField] private AudioClip powerUpAudioClip;

    private Rigidbody2D rb;

    void OnEnable()
    {
        transform.position = playerMovement.transform.position;

        if (!rb) {
            rb = GetComponent<Rigidbody2D>();
        }

        float angleRad = (playerMovement.transform.eulerAngles.z + 90f) * Mathf.Deg2Rad;

        Vector2 rotation =  new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
        rb.velocity = rotation * 20f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Visual and audio
        explosionController.transform.position = transform.position;
        explosionController.SetupAndTrigger(oreBlaster.destroyRadius);

        audioDelegator.PlayAudio(powerUpAudioSource, powerUpAudioClip, 0.8f);
        
        // Destroy ores
        oreBlaster.BlastOres();

        gameObject.SetActive(false);
    }

}