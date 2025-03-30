using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    public ParticleSystem mainExplosionParticles;

    readonly float destroyDelay = 2.0f;     // Time before destroying the effect GO

    // Call this method to set scale and trigger the explosion
    public void SetupAndTrigger(float scale)
    {
        ParticleSystem particleSystem = GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainPS = particleSystem.main;

        // Match particle speed to the radius, that way it doesnt go too short or too far
        mainPS.startSpeed = new ParticleSystem.MinMaxCurve(scale * 2.25f);
        mainPS.startSizeMultiplier = scale/10;

        // Trigger Effects
        if (mainExplosionParticles != null)
        {
            mainExplosionParticles.Play();
        }

        // Schedule Destruction
        Destroy(gameObject, destroyDelay);
    }
}