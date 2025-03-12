using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    private int radius;
    private TileBase[] ores;
    private GameObject[] materials;
    private MineRenderer mineRenderer;
    private JoystickMovement joystickMovement;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private int drillTier;
    // Does nothing, just for showing the user in the Garage
    public int width;
    [SerializeField]
    private long price;
    [SerializeField]
    private float profitMultiplier;
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

    private Collider2D[] colliders;
    private Tilemap tilemap;
    private Vector3 spriteWorldPos;
    private Vector3Int spriteTilePos;
    private Vector3 tileWorldPos;
    private TileBase tileToDestroy;
    private int tileTier;
    private GameObject materialToUse;
    private int oldCount;
    private Collider2D[] hitColliders;
    private MaterialManager newMaterialManager;
    private int randomIndex;
    readonly List<Vector2Int> currentTilePositions = new();
    readonly List<Vector3> tileWorldPositions = new();
    readonly List<TileBase> tileBasesToDestroy = new();
    bool dontPlayAudio;
    public bool isNPC = false;

    void Start() {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        ores = mineRenderer.GetOres();
        materials = mineRenderer.oreDelegation.materials;

        boxCollider2D = GetComponent<BoxCollider2D>();
        // Get the bounds of the BoxCollider2D
        rotatedOffset = boxCollider2D.offset;
        
        radius = Mathf.RoundToInt(GetComponent<BoxCollider2D>().size.x);

        try {
            joystickMovement = transform.parent.parent.GetComponent<PlayerMovement>().joystickMovement;
        } catch {
            isNPC = true;
            return;
        }

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        drillBlockSoundEffects = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockSoundEffects;
        drillBlockVolumes = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockVolumes;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();
        uiDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();

    }

    void Update() {
       // Used to reset the counters, that way when user backs up from tile then comes back, it displays the error again
        if (lastErrorCounter == errorCounter && lastErrorCounter != 60) {
            errorCounter = 400;
        }
        lastErrorCounter = errorCounter;

        size = boxCollider2D.bounds.size;

        // Calculate the corrected offset
        Vector3 correctedOffset = transform.rotation * rotatedOffset;

        // Check if the game object's collider is touching a tilemap with "Mine Tag"
        colliders = Physics2D.OverlapBoxAll(transform.position + correctedOffset, size, 0);
        
        dontPlayAudio = false;

        foreach (Collider2D collision in colliders) {
            if (!collision.gameObject.CompareTag("Mine Tag")) {
                continue;
            }

            tilemap = mineRenderer.tilemapsDictionary[collision.name];

            spriteWorldPos = transform.position;
            spriteTilePos = tilemap.WorldToCell(spriteWorldPos);

            currentTilePositions.Clear();
            tileWorldPositions.Clear();
            tileBasesToDestroy.Clear();
            // Iterate over nearby tiles within the radius
            for (int x = 0; x <= radius; x++)
            {
                int yLimit = radius - x;
                for (int y = 0; y <= yLimit; y++)
                {
                    // Check all 4 quadrants
                    CheckToDestroyTile(spriteTilePos + new Vector3Int(x, y, 0));
                    CheckToDestroyTile(spriteTilePos + new Vector3Int(-x, y, 0));
                    CheckToDestroyTile(spriteTilePos + new Vector3Int(x, -y, 0));
                    CheckToDestroyTile(spriteTilePos + new Vector3Int(-x, -y, 0));
                }
            }

            mineRenderer.DestroyTiles(currentTilePositions, false, isNPC);

            if (!dontPlayAudio && joystickMovement && joystickMovement.joystickVec != Vector2.zero && !isNPC) {
                PlayAudio();
            }

            for (int j = 0; j != tileWorldPositions.Count; j++) {
                for (int i = 0; i != ores.Length; i++) {
                    if (tileBasesToDestroy[j] != ores[i]) {
                        continue;
                    }

                    materialToUse = materials[i];

                    // If no neighbouring materials then this stays 0 and the new object will have a count of 1
                    oldCount = 0;
                    hitColliders = Physics2D.OverlapCircleAll(tileWorldPositions[j], radius);

                    foreach (var hitCollider in hitColliders)
                    {      
                        // Make sure a gameobject was hit
                        if (hitCollider == null) {
                            continue;
                        }
                        
                        // Make sure they are the same materials
                        if (hitCollider.name != materialToUse.name + "(Clone)") {
                            continue;
                        }
                
                        // If a neighbouring material was found, return to object pool,
                        // and keep track of the count of the object
                        // Don't set oldCount, use += in case there are more than 1;
                        // Also don't break for the same reason
                        newMaterialManager = hitCollider.GetComponent<MaterialManager>();
                        oldCount += newMaterialManager.count;

                        mineRenderer.ReturnMaterialObject(hitCollider.gameObject, i, newMaterialManager.id);
                    }

                    mineRenderer.GetMaterialObject(i, tileWorldPositions[j], oldCount + 1, profitMultiplier);
                    break;
                }
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
        tileTier = mineRenderer.GetTileTier(tileToDestroy);
        if (drillTier < tileTier) {
            if (!isNPC) {
                errorCounter++;
                
                // Dont spam the user with errors
                if (errorCounter >= 400) {
                    uiDelegation.ShowError("TIER {0} DRILL IS NEEDED!", tileTier);
                    errorCounter = 0;
                }
                dontPlayAudio = true;
            }
            
            return;
        }

        tileWorldPos = tilemap.GetCellCenterWorld(currentTilePos);
        currentTilePositions.Add(new(currentTilePos.x, currentTilePos.y));
        tileWorldPositions.Add(tileWorldPos);
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
