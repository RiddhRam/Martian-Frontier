using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class OreBlaster : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditCounterText;
    
    [SerializeField] private OreBlasterRoundManager oreBlasterRoundManager;
    [SerializeField] private ExplosiveChargeProjectile explosiveChargeProjectile;
    [SerializeField] private MineRenderer mineRenderer;

    [SerializeField] private Slider slider;

    public int destroyRadius;

    // Cache
    Tilemap tilemap;
    Vector3Int spriteTilePos;
    readonly HashSet<Vector2Int> tilesToDestroy = new();

    private int[] creditTileValues = new int[] {0, 4, 7, 12};
    public int collectedCredits = 0;

    public float blastInterval;
    [SerializeField] private float blastTimer;

    int creditsToAdd;

    void Start()
    {
        blastTimer = blastInterval;
    }

    void FixedUpdate()
    {
        slider.value = blastTimer / blastInterval;
        // If in lobby, disable projectible
        if (!oreBlasterRoundManager.roundInProgress) {
            blastTimer = blastInterval;
            explosiveChargeProjectile.gameObject.SetActive(false);
            return;
        }

        // If projectile active, wait for it to be inactive before reloading
        if (explosiveChargeProjectile.gameObject.activeSelf) {
            blastTimer = 0;
            return;
        }

        // Reload and fire
        blastTimer += Time.deltaTime;
        if (blastTimer >= blastInterval) {
            blastTimer = 0f;                               // reset for the next interval
            explosiveChargeProjectile.gameObject.SetActive(true);
        }
    }

    public void BlastOres() {
        creditsToAdd = 0;
        Vector3 projectilePos = explosiveChargeProjectile.transform.position;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(projectilePos, new(destroyRadius * 2.5f, destroyRadius * 2.5f), 0);

        spriteTilePos = new((int) projectilePos.x, (int) projectilePos.y, (int) projectilePos.z);

        foreach (Collider2D collision in colliders) {
            if (!collision.CompareTag("Mine Tag")) {
                continue;
            }

            tilemap = mineRenderer.tilemapsDictionary[collision.name];

            tilesToDestroy.Clear();
            // Iterate over nearby tiles within the radius
            for (int x = -destroyRadius; x <= destroyRadius; x++)
            {
                for (int y = -destroyRadius; y <= destroyRadius; y++)
                {
                    if (x * x + y * y <= destroyRadius * destroyRadius) // Check if inside circle
                    {
                        CheckToDestroyTile(spriteTilePos + new Vector3Int(x, y, 0));
                    }
                }
            }

            // Not an npc, but set it the parameter true just so it doesn't count towards stats
            mineRenderer.DestroyTiles(tilesToDestroy.ToList(), false, transform.position, false);
        }

        UpdateCreditCount(creditsToAdd);
    }

    public void UpdateCreditCount(int newAmount) {
        collectedCredits += newAmount;
        creditCounterText.text = collectedCredits.ToString();
    }

    private void CheckToDestroyTile(Vector3Int currentTilePos) {

        // Check if the tile exists
        if (!tilemap.HasTile(currentTilePos)) {
            return;
        }

        // Have to get the tile index first and then using tileValues array, rather than getting the tilebase from the tilemap
        // otherwise unknown tiles will be destroyed
        Vector2Int tilemapPos = mineRenderer.CalculateTileMapPos(new(currentTilePos.x, currentTilePos.y));

        int tileIndex = mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y][new(currentTilePos.x, currentTilePos.y)];
        creditsToAdd += creditTileValues[tileIndex];

        tilesToDestroy.Add(new(currentTilePos.x, currentTilePos.y));
    }

}