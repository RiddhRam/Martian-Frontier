using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class DrillerController : MonoBehaviour
{
    [Header("Scripts")]
    private MineRenderer mineRenderer;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public NPCMovement nPCMovement;

    [SerializeField] private float playerSpeed;
    [SerializeField] private int drillTier;
    // Does nothing, just for showing the user in the Garage
    public int width;
    public int endurance;
    private float coolRate = 0.5f; // 0.5f * 50fps = 25/second
    [SerializeField] private long price;
    [SerializeField] private float profitMultiplier;

    [Header("Upgrade Bay Info")]
    public int drillerIndex; // Position in the garage
    public int drillTypeIndex; // Corresponds to sprite's position in allNormalDrills in VehicleUpgradeBayManager

    [Header("Audio")]
    private AudioSource vehicleSoundEffects;
    private AudioClip[] drillBlockSoundEffects;
    private float[] drillBlockVolumes;
    // Same thing as the error counter, but with an actual timer
    private DateTime audioTimer = DateTime.Now;
    private int lastAudioUsed = -1;
    
    [Header("Drilling")]
    private BoxCollider2D boxCollider2D;
    private Vector2 size;
    Vector2 rotatedOffset;
    private int radius;

    [Header("Endurance")]
    public float drillHeat = 0;
    private int highestTierDrilled = 0;

    [Header("Overheat progress bar")]
    public Image sliderImage;
    public Slider slider;
    public RectTransform sliderTransform;
    public TextMeshProUGUI sliderText;
    public Vector3 initialScale;
    static readonly Color hotColor = new(217f / 255f, 0f / 255f, 0f / 255f);
    static readonly Color coldColor = new(0f / 255f, 235f / 255f, 0f / 255f);
    static readonly Color mid = new Color(1f, 146f/255f, 0f);
    static readonly Color pulseColor = new Color(150f / 255f, 0f, 0f, 1f);
    Color baseColor;

    [Header("Cache")]
    // 40 should be more enough for drilling
    private readonly Collider2D[] colliders = new Collider2D[40];
    private Tilemap tilemap;
    private Vector3Int spriteTilePos;
    private TileBase tileToDestroy;
    private int randomIndex;
    readonly List<Vector2Int> currentTilePositions = new();
    System.Random rng = new();

    bool minedSomething;

    void Start()
    {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();

        boxCollider2D = GetComponent<BoxCollider2D>();
        // Get the bounds of the BoxCollider2D
        rotatedOffset = boxCollider2D.offset;

        radius = Mathf.RoundToInt(GetComponent<BoxCollider2D>().size.x);

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        drillBlockSoundEffects = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockSoundEffects;
        drillBlockVolumes = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockVolumes;
    }

    // Can't use OverlapBoxNonAlloc anymore in Unity 6 and higher
    void FixedUpdate()
    {
        // Update endurance each iteration in case it updates. This way there's no need to set a listener
        endurance = VehicleUpgradeBayManager.Instance.GetHeatLimit("");

        UpdateOverheatSlider(drillHeat / endurance, drillHeat);

        if (drillHeat < endurance)
        {
            size = boxCollider2D.bounds.size;

            // Calculate the corrected offset
            Vector3 correctedOffset = transform.rotation * rotatedOffset;

            // Check if the game object's collider is touching a tilemap with "Mine Tag"
            int colliderCount = Physics2D.OverlapBoxNonAlloc(transform.position + correctedOffset, size, 0, colliders);

            // Destroy tiles
            for (int i = 0; i < colliderCount; i++)
            {
                if (!colliders[i].CompareTag("Mine Tag"))
                {
                    continue;
                }

                tilemap = mineRenderer.tilemapsDictionary[colliders[i].name];

                spriteTilePos = tilemap.WorldToCell(transform.position);

                currentTilePositions.Clear();
                // Iterate over nearby tiles within the radius
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (x * x + y * y <= radius * radius) // Check if inside circle
                        {
                            CheckToDestroyTile(spriteTilePos + new Vector3Int(x, y, 0));
                        }
                    }
                }

                if (minedSomething)
                {
                    mineRenderer.DestroyTiles(currentTilePositions, false, (transform.position + transform.parent.position) / 2, true, nPCMovement);

                    PlayAudio();
                }
            }

            if (currentTilePositions.Count > 0)
            {
                minedSomething = true;
            }
            else
            {
                minedSomething = false;
            }
        }
        else
        {
            minedSomething = false;
        }

        if (minedSomething)
        {

            float heatToAdd = (int)Mathf.Pow(highestTierDrilled, 5) * 0.34f;
            drillHeat = Mathf.Min(endurance, drillHeat + heatToAdd);
        }

        if (drillHeat >= endurance)
        {
            nPCMovement.StartCooldownDrill();
        }

        highestTierDrilled = 0;
    }

    public void CheckToDestroyTile(Vector3Int currentTilePos)
    {

        // Check if the tile exists
        if (!tilemap.HasTile(currentTilePos))
        {
            return;
        }

        tileToDestroy = tilemap.GetTile(currentTilePos);

        // Make sure the drill is capable of destroying this tile
        int tileTier = mineRenderer.GetTileTier(tileToDestroy);

        if (highestTierDrilled < tileTier)
        {
            highestTierDrilled = tileTier;
        }

        currentTilePositions.Add(new(currentTilePos.x, currentTilePos.y));
    }

    public float GetPlayerSpeed()
    {
        return playerSpeed;
    }

    public int GetDrillTier()
    {
        return drillTier;
    }

    public long GetPrice()
    {
        return price;
    }

    public void SetProfitMultiplier(float newProfitMultiplier)
    {
        this.profitMultiplier = newProfitMultiplier;
    }

    public float GetCoolRate()
    {
        return coolRate;
    }

    public void SetCoolRate(float newRate)
    {
        coolRate = newRate;
    }

    public void PlayAudio()
    {
        // Wait at least 1000 miliseconds before playing audio again
        if ((DateTime.Now - audioTimer).TotalMilliseconds < 1000)
        {
            return;
        }

        audioTimer = DateTime.Now;

        // 50% chance of not playing audio
        if (rng.NextDouble() < 0.66)
        {
            return;
        }

        // Make sure we are not using the same audio twice in a row
        // Theoretically, this loop can get stuck forever but very unlikely
        randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);

        while (randomIndex == lastAudioUsed)
        {
            randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);
        }

        lastAudioUsed = randomIndex;

        AudioDelegator.Instance.PlayAudio(vehicleSoundEffects, drillBlockSoundEffects[randomIndex], drillBlockVolumes[randomIndex]);
    }
    
    public void UpdateOverheatSlider(float heatPercentage, float drillHeat)
    {
        // Progress
        slider.value = heatPercentage;
        // Text
        sliderText.text = ((int)drillHeat).ToString();

        // Calculate base colour
        if (heatPercentage < 0.5f)
        {
            // 0 → 0.5 : blue → yellow
            baseColor = Color.Lerp(coldColor, mid, heatPercentage * 2f);
        }
        else
        {
            // 0.5 → 1 : yellow → red
            baseColor = Color.Lerp(mid, hotColor, (heatPercentage - 0.5f) * 2f);
        }
        
        // Pulse the progress bar if heat percentage is above threshold
        const float pulseThreshold = 0.80f;
        const float pulseSpeed = 10f;
        const float pulseScaleAmount = 0.1f;

        if (heatPercentage > pulseThreshold)
        {
            // Calculate a 0-1 value representing how far we are past the pulseThreshold
            float rampUpFactor = (heatPercentage - pulseThreshold) / (1f - pulseThreshold);

            // Oscillation
            float pulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            // Factor the 2 together
            float pulseValueToUse = pulseValue * rampUpFactor;

            // Lerp between the current base color and the bright pulse color
            sliderImage.color = Color.Lerp(baseColor, pulseColor, pulseValueToUse);

            // Pulse the size of the progress bar too
            float scaleMultiplier = 1f + (pulseValueToUse * pulseScaleAmount);
            sliderTransform.localScale = initialScale * scaleMultiplier;
        }
        // If heat is below threshold, make progress look normal
        else
        {
            // Don't modify colour or scale
            sliderImage.color = baseColor;
            sliderTransform.localScale = initialScale;
        }
    }
}
