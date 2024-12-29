using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DrillerController : MonoBehaviour
{
    // Not actually a radius, it's a square
    private int radius;
    private TileBase[] ores;
    private GameObject[] materials;
    private Sprite[] materialSprites;
    private MineRenderer mineRenderer;
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private int drillTier;
    // Does nothing, just for showing the user in the Garage
    public int width;
    [SerializeField]
    private long price;
    private UncollectedMaterialsDelegator materialsDelegator;
    // Every second spent atttempting to mine a higher tier block, display an error
    private int errorCounter = 50;
    private int lastErrorCounter = 50;
    private AudioSource vehicleSoundEffects;
    private AudioClip[] drillBlockSoundEffects;
    private float[] drillBlockVolumes;
    // Same thing as the error counter, but with an actual timer
    private DateTime audioTimer = DateTime.Now;
    private int lastAudioUsed = -1;
    private AudioDelegator audioDelegator;
    private UIDelegation uiDelegation;

    void Start() {
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        ores = mineRenderer.GetOres();

        materials = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().materials;
        materialSprites = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().materialSprites;

        materialsDelegator = GameObject.Find("Materials Delegator").GetComponent<UncollectedMaterialsDelegator>();
        
        radius = Mathf.RoundToInt(GetComponent<BoxCollider2D>().size.x);

        vehicleSoundEffects = GameObject.Find("Vehicle Sound Effects").GetComponent<AudioSource>();
        drillBlockSoundEffects = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockSoundEffects;
        drillBlockVolumes = GameObject.Find("Sound Holder").GetComponent<SoundHolder>().drillBlockVolumes;
        audioDelegator = GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>();
        uiDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();
    }

    void FixedUpdate() {
        // Used to reset the counters, that way when user backs up from tile then comes back, it displays the error again
        if (lastErrorCounter == errorCounter && lastErrorCounter != 60) {
            errorCounter = 60;
        }
        lastErrorCounter = errorCounter;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Mine Tag")) {
            return;
        }

        Tilemap tilemap = collision.GetComponent<Tilemap>();

        Vector3 spriteWorldPos = transform.position;
        Vector3Int spriteTilePos = tilemap.WorldToCell(spriteWorldPos);

        float closestDistance = radius + 1;
        Vector3Int nearestTilePos = Vector3Int.zero;
        Vector3 centerTilePos = Vector3.zero;

        // Iterate over nearby tiles within the radius
        // Not actually a radius, it's a square
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentTilePos = spriteTilePos + new Vector3Int(x, y, 0);

                // Check if the tile exists
                if (!tilemap.HasTile(currentTilePos)) {
                    continue;
                }

                Vector3 tileWorldPos = tilemap.GetCellCenterWorld(currentTilePos);
                float distance = Vector3.Distance(spriteWorldPos, tileWorldPos);

                if (distance >= closestDistance) {
                    continue;
                }

                // Keep track of the closest tile
                closestDistance = distance;
                nearestTilePos = currentTilePos;
                centerTilePos = tileWorldPos; 
            }
        }

        if (closestDistance >= radius) {
            return;
        }

        TileBase tileToDestroy = tilemap.GetTile(nearestTilePos);
        TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();

        // Make sure the drill is capable of destroying this tile
        int tileTier = mineRenderer.GetTileTier(tileToDestroy);
        if (drillTier < tileTier) {
            errorCounter++;
            FlickerMap(tilemapCollider);
            // Dont spam the user with errors
            if (errorCounter >= 60) {
                uiDelegation.ShowError("TIER {0} DRILL IS NEEDED!", tileTier);
                errorCounter = 0;
            }
            return;
        }

        // Destroy the tile and reveal new tiles in the vision radius
        mineRenderer.DestroyTile(nearestTilePos, false);

        PlayAudio();

        for (int i = 0; i != ores.Length; i++) {
            if (tileToDestroy != ores[i]) {
                continue;
            }

            GameObject materialToUse = materials[i];

            // If no neighbouring materials then this stays 0 and the new object will have a count of 1
            int oldCount = 0;

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                { 

                    Collider2D[] hitColliders = Physics2D.OverlapCircleAll(new Vector2(x + centerTilePos.x, y + centerTilePos.y), 0.1f);
                    
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
                
                        // If a neighbouring material was found, delete it,
                        // and keep track of that value deleted
                        // Don't set oldCount, use += in case there are more than 1;
                        // Also don't break for the same reason
                        MaterialManager newMaterialManager = hitCollider.GetComponent<MaterialManager>();
                        oldCount += newMaterialManager.count;
                        materialsDelegator.RemoveMaterial(newMaterialManager.id);
                        Destroy(hitCollider.gameObject);

                        break;
                    }
                }
            }

            GameObject material = Instantiate(materialToUse);
            materialsDelegator.AddMaterial(material, materialSprites[i], centerTilePos, i, oldCount + 1);
            break;
        }

        FlickerMap(tilemapCollider);
    }

    private void FlickerMap(TilemapCollider2D tilemap) {
        // Disable and renable quickly so the trigger event can occur again
        tilemap.GetComponent<TilemapCollider2D>().enabled = false;
        tilemap.GetComponent<TilemapCollider2D>().enabled = true;
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

    public void PlayAudio() {
        if ((DateTime.Now - audioTimer).TotalMilliseconds < 1000) {
            return;
        }

        // Make sure we are not using the same audio twice in a row
        // Theoretically, this loop can get stuck forever but very unlikely
        int randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);

        while (randomIndex == lastAudioUsed) {
            randomIndex = UnityEngine.Random.Range(0, drillBlockSoundEffects.Length);
        }

        lastAudioUsed = randomIndex;

        audioDelegator.PlayAudio(vehicleSoundEffects, drillBlockSoundEffects[randomIndex], drillBlockVolumes[randomIndex]);

        audioTimer = DateTime.Now;
    }
}
