using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    private int radius;
    private MineRenderer mineRenderer;
    private JoystickMovement joystickMovement;
    public PlayerVehicleDelegation playerVehicleDelegation;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private int drillTier;
    // Does nothing, just for showing the user in the Garage
    public int width;
    public int endurance;
    private float coolRate = 0.5f; // 0.5f * 50fps = 25/second
    [SerializeField]
    private long price;
    [SerializeField]
    private float profitMultiplier;

    [Header("Upgrade Bay Info")]
    public int drillerIndex; // Position in the garage
    public int drillTypeIndex; // Corresponds to sprite's position in allNormalDrills in VehicleUpgradeBayManager
    // Every second spent atttempting to mine a higher tier block, display an error
    private int errorCounter = 400;
    private int lastErrorCounter = 400;
    private AudioSource vehicleSoundEffects;
    private AudioClip[] drillBlockSoundEffects;
    private float[] drillBlockVolumes;
    // Same thing as the error counter, but with an actual timer
    private DateTime audioTimer = DateTime.Now;
    private int lastAudioUsed = -1;
    private AudioDelegator audioDelegator;
    private UIDelegation uiDelegation;
    private BoxCollider2D boxCollider2D;
    private Vector2 size;
    Vector2 rotatedOffset;

    // Endurance
    private float drillHeat = 0;
    private float lastMineTime = -Mathf.Infinity;
    private const float heatCooldownDelay = 0.6f;
    private int highestTierDrilled = 0;

    // Cache
    // 40 should be more enough for drilling
    private readonly Collider2D[] colliders = new Collider2D[40];
    private Tilemap tilemap;
    private Vector3Int spriteTilePos;
    private TileBase tileToDestroy;
    private int randomIndex;
    readonly List<Vector2Int> currentTilePositions = new();
    readonly List<Vector3> tileWorldPositions = new();
    readonly List<TileBase> tileBasesToDestroy = new();

    float time = 0;
    int count = 0;
    bool minedSomething;
    public bool isNPC = false;

    void Start() {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();

        boxCollider2D = GetComponent<BoxCollider2D>();
        // Get the bounds of the BoxCollider2D
        rotatedOffset = boxCollider2D.offset;
        
        radius = Mathf.RoundToInt(GetComponent<BoxCollider2D>().size.x);

        // DO NOT CHANGE THIS UNLESS DRILLER PREFAB STRUCTURE OR PLAYER STRUCTURE CHANGES
        try {
            joystickMovement = transform.parent.parent.GetComponent<PlayerMovement>().joystickMovement;
        } catch {
            // if failed, then its not the local main player
            isNPC = true;
            return;
        }

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        drillBlockSoundEffects = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockSoundEffects;
        drillBlockVolumes = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockVolumes;
        audioDelegator = AudioDelegator.Instance;
        uiDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();
    }

    void FixedUpdate() {

        // Used to reset the counters, that way when user backs up from tile then comes back, it displays the error again
        if (lastErrorCounter == errorCounter && lastErrorCounter != 60) {
            errorCounter = 400;
        }
        lastErrorCounter = errorCounter;

        if (!isNPC) {
            playerVehicleDelegation.UpdateOverheatSlider(drillHeat / endurance, drillHeat);
        }

        if (drillHeat < endurance) {
            size = boxCollider2D.bounds.size;

            // Calculate the corrected offset
            Vector3 correctedOffset = transform.rotation * rotatedOffset;

            // Check if the game object's collider is touching a tilemap with "Mine Tag"
            int colliderCount = Physics2D.OverlapBoxNonAlloc(transform.position + correctedOffset, size, 0, colliders);

            // Destroy tiles
            for (int i = 0; i < colliderCount; i++) {
                if (!colliders[i].CompareTag("Mine Tag")) {
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

                if (minedSomething) {
                    mineRenderer.DestroyTiles(currentTilePositions, false, isNPC, (transform.position + transform.parent.position)/2, true);

                    if (!isNPC && joystickMovement && joystickMovement.joystickVec != Vector2.zero) {
                        PlayAudio();
                    }
                }
                
            }

            if (tileWorldPositions.Count > 0) {
                minedSomething = true;
            } else {
                minedSomething = false;
            }

            tileWorldPositions.Clear();
            tileBasesToDestroy.Clear();
        }
        else {
            minedSomething = false;
        }

        if (isNPC) {
            return;
        }

        // Drill overheat
        float timeSinceLastMine = Time.time - lastMineTime;
        
        if (minedSomething)
        {
            // if within chain window, add heat, irregardless of amount of blocks mined
            if (timeSinceLastMine <= heatCooldownDelay)
            {
                // 0.34f = fixed coefficency
                // 0.09f = average time since last mine for small drills. 
                // Helps nerf larger drillers, which have more time in between mining (about double, so they use half the endurance)
                // 0.17f / 0.09f = 1.88f which helps balance the larger drills
                float heatToAdd = (int)Mathf.Pow(highestTierDrilled, 5) * 0.34f * (timeSinceLastMine / 0.09f);

                drillHeat = Mathf.Min(endurance, drillHeat + heatToAdd);

                // Gives average time since last mine
                /*time += timeSinceLastMine;
                count++;
                Debug.Log(time/count);*/
            }

            lastMineTime = Time.time;
        }
        else
        {
            // if player stopped mining and drill has some heat, cool it down
            if (timeSinceLastMine > heatCooldownDelay && drillHeat > 0f)
            {
                drillHeat = Mathf.Max(0, (int)(drillHeat - coolRate));
            }
        }

        highestTierDrilled = 0;
    }

    public void ErrorWhenDrilling(string error, params object[] args) {
        if (!isNPC) {
            errorCounter++;
            
            // Dont spam the user with errors
            if (errorCounter >= 400) {
                uiDelegation.ShowError(error, args);
                errorCounter = 0;
            }
        }
    }

    public void CheckToDestroyTile(Vector3Int currentTilePos) {

        // Check if the tile exists
        if (!tilemap.HasTile(currentTilePos)) {
            return;
        }

        tileToDestroy = tilemap.GetTile(currentTilePos);
        
        // Make sure the drill is capable of destroying this tile
        int tileTier = mineRenderer.GetTileTier(tileToDestroy);
        if (highestTierDrilled < tileTier) {
            highestTierDrilled = tileTier;
        }

        currentTilePositions.Add(new(currentTilePos.x, currentTilePos.y));
        tileWorldPositions.Add(tilemap.GetCellCenterWorld(currentTilePos));
        tileBasesToDestroy.Add(tileToDestroy);
    }

    public float GetPlayerSpeed() {
        return playerSpeed;
    }

    public int GetDrillTier() {
        return drillTier;
    }

    public long GetPrice() {
        return price;
    }

    public void SetProfitMultiplier(float newProfitMultiplier) {
        this.profitMultiplier = newProfitMultiplier;
    }

    public float GetCoolRate() {
        return coolRate;
    }

    public void SetCoolRate(float newRate) {
        coolRate = newRate;
    }  

    public void PlayAudio() {
        if ((DateTime.Now - audioTimer).TotalMilliseconds < 1000) {
            return;
        }

        // Make sure we are not using the same audio twice in a row
        // Theoretically, this loop can get stuck forever but very unlikely
        randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);

        while (randomIndex == lastAudioUsed) {
            randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);
        }

        lastAudioUsed = randomIndex;

        audioDelegator.PlayAudio(vehicleSoundEffects, drillBlockSoundEffects[randomIndex], drillBlockVolumes[randomIndex]);

        audioTimer = DateTime.Now;
    }
}
