using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class UpgradesDelegator : MonoBehaviour, IDataPersistence
{
    [SerializeField] private Transform playerVehicle;
    [SerializeField] private MineRenderer mineRenderer;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private UIDelegation uIDelegation;
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private Image powerIconImage;
    [SerializeField] private Button powerButton;
    [SerializeField] private TextMeshProUGUI powerCooldownTimer;
    [SerializeField] private GameObject[] powerPanels;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Color surveyRadarColor;
    [SerializeField] private Sprite[] powerIcons;
    [SerializeField] private Sprite[] powerIconsWhite;

    private TileBase[] ores;
    private GameObject[] materials;
    private OreDelegation oreDelegation;
    
    bool notSinglePlayerScene = false;
    
    // Survey Radar
    public int visionRadius; // Base: 10
    // Explosive Charge
    public int destroyRadius; // Base: 6
    public float refineryProfitMultiplier; // Base: 1

    // For ad boost
    public int visionBoost; // Base: 3
    public float refineryProfitMultiplierBoost; // Base: 2

    readonly HashSet<Vector2Int> tilesToDestroy = new();
    readonly HashSet<Vector2Int> tilesToReveal = new();

    // Cache
    Tilemap tilemap;
    Vector3Int spriteTilePos;
    readonly List<Vector3> tileWorldPositions = new();
    readonly List<TileBase> tileBasesToDestroy = new();
    GameObject materialToUse;
    private MaterialManager newMaterialManager;
    private Collider2D[] hitColliders;
    ExplosionController explosionController;

    private List<string> equippedPowers = new();
    private List<Powers> powers = new();

    private int cooldownTimer = 0;

    void Start()
    {
        ores = mineRenderer.GetOres();
        oreDelegation = mineRenderer.oreDelegation;
        materials = oreDelegation.materials;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
        }
    }

    public void UsePower() {
        foreach (var power in powers) {
            if (power.Name == equippedPowers[0]) {
                power.PowerFunction.Invoke();
                break;
            }
        }

    }

    // Reveal surrounding ores, no rocks, just ores
    [ContextMenu("Survey Radar")]
    private void SurveyRadar() {
        StartCoroutine(SurveyAnimation());
        tilesToReveal.Clear();

        Vector2Int playerPos = new((int) playerVehicle.position.x, (int) playerVehicle.position.y);

        for (int x = -visionRadius; x <= visionRadius; x++)
        {
            for (int y = -visionRadius; y <= visionRadius; y++)
            {
                if (x * x + y * y <= visionRadius * visionRadius) // Check if inside circle
                {
                    AddTileIfOre(new(playerPos.x + x, playerPos.y - y));
                }
            }
        }

        mineRenderer.RevealTiles(tilesToReveal);

        StartCoroutine(StartCooldownTimer(90));
    }

    private void AddTileIfOre(Vector2Int newTileToReveal) {
        Vector2Int tilemapPos = mineRenderer.CalculateTileMapPos(newTileToReveal);
        
        if (!mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y].ContainsKey(newTileToReveal)) {
            return;
        }

        if (oreDelegation.VerifyIfOre(mineRenderer.unplacedTilemapsTileValues[tilemapPos.x, tilemapPos.y][newTileToReveal])) {
            tilesToReveal.Add(newTileToReveal);
        }
    }

    private GameObject CreateCircle(float radius, Color circleColor, int positionCount)
    {
        GameObject circleObject = new GameObject("Circle");
        LineRenderer circle = circleObject.AddComponent<LineRenderer>();
        float radiusToUse = radius;

        if (circleColor == surveyRadarColor) {
            circle.startWidth = 1f;
            circle.endWidth = 1f;
        } else {
            circle.startWidth = radius;
            circle.endWidth = radius;
            radiusToUse /= 2;
        }
        
        circle.loop = true;
        circle.positionCount = positionCount;
        circle.material = defaultMaterial;
        circle.startColor = circleColor;
        circle.endColor = circleColor;
        circle.sortingOrder = 3;

        float multplier = 360f / positionCount;

        for (int i = 0; i < positionCount; i++)
        {
            float angle = i * multplier * Mathf.Deg2Rad;

            float x = Mathf.Cos(angle) * radiusToUse;
            float y = Mathf.Sin(angle) * radiusToUse;

            circle.SetPosition(i, new Vector3(x, y, 0) + playerVehicle.position);
        }
        return circleObject;
    }

    private IEnumerator SurveyAnimation() {

        GameObject outerCircle = CreateCircle(visionRadius, surveyRadarColor, 40);
        GameObject innerCircle = CreateCircle(visionRadius, new(surveyRadarColor.r, surveyRadarColor.g, surveyRadarColor.b, 0.4f), 1000);

        float angle = 180;
        GameObject scanner = new GameObject("ScannerLine");
        LineRenderer scannerLine = scanner.AddComponent<LineRenderer>();
        scannerLine.startWidth = 1f;
        scannerLine.endWidth = 1f;
        scannerLine.material = defaultMaterial;
        scannerLine.startColor = Color.white;
        scannerLine.endColor = Color.white;
        scannerLine.positionCount = 2;
        scannerLine.sortingOrder = 3;

        Vector3 start = playerVehicle.position;

        while (angle > -270f)
        {
            angle -= 200f * Time.deltaTime; // 200f = degrees per second
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 end = start + new Vector3(Mathf.Cos(rad) * visionRadius, Mathf.Sin(rad) * visionRadius, 0);
            scannerLine.SetPosition(0, start);
            scannerLine.SetPosition(1, end);
            yield return null;
        }
        
        Destroy(scanner);
        Destroy(outerCircle);
        Destroy(innerCircle);
    }

    // Destroy surrounding ores
    [ContextMenu("Explosive Charge")]
    private void ExplosiveCharge() {
        // Set off any generation triggers first
        // Create a new GameObject for the collider
        GameObject colliderObject = new("GroundCollider");
        Rigidbody2D rb = colliderObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        // Add BoxCollider2D to the new GameObject
        BoxCollider2D boxCollider = colliderObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        boxCollider.size = new(1, destroyRadius);
        boxCollider.transform.position = new(playerVehicle.position.x, playerVehicle.position.y - destroyRadius/2);
        boxCollider.transform.rotation = new();

        // Very similar to what is used in Driller Controller
        // Have to multiply size by 2.5f, because for some reason it misses tilemaps sometimes
        Collider2D[] colliders = Physics2D.OverlapBoxAll(playerVehicle.position, new(destroyRadius * 2.5f, destroyRadius * 2.5f), 0);

        spriteTilePos = new((int) playerVehicle.position.x, (int) playerVehicle.position.y, (int) playerVehicle.position.z);

        // Show explosion
        explosionController.transform.position = playerVehicle.position;
        explosionController.SetupAndTrigger(destroyRadius);

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

            mineRenderer.DestroyTiles(tilesToDestroy.ToList(), false, true);
        }

        for (int j = 0; j != tileWorldPositions.Count; j++) {
            for (int i = 0; i != ores.Length; i++) {
                if (tileBasesToDestroy[j] != ores[i]) {
                    continue;
                }

                materialToUse = materials[i];

                // If no neighbouring materials then this stays 0 and the new object will have a count of 1
                int oldCount = 0;
                hitColliders = Physics2D.OverlapCircleAll(tileWorldPositions[j], 3);

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

                mineRenderer.GetMaterialObject(i, tileWorldPositions[j], oldCount + 1, 0);
                break;
            }
        }

        tileWorldPositions.Clear();
        tileBasesToDestroy.Clear();

        StartCoroutine(StartCooldownTimer(90));
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

        // Make sure the drill is capable of destroying this tile
        int tileTier = mineRenderer.GetTileTier(mineRenderer.tileValues[tileIndex]);
        if (playerState.GetHighestDrillTier() < tileTier) {            
            return;
        }

        tilesToDestroy.Add(new(currentTilePos.x, currentTilePos.y));
        tileBasesToDestroy.Add(tilemap.GetTile(currentTilePos));
        tileWorldPositions.Add(tilemap.GetCellCenterWorld(currentTilePos));
    }

    [ContextMenu("Show Teleporter")]
    private void ShowTeleporter() {
        uIDelegation.HideAll();
        uIDelegation.ToggleCamera();
        uIDelegation.RevealElement(teleportPanel);
    }

    public void Teleport(Vector3 newPosition) {
        playerVehicle.position = new(newPosition.x, newPosition.y);

        uIDelegation.HideElement(teleportPanel);
        uIDelegation.RevealAll();
        uIDelegation.ToggleCamera();

        StartCoroutine(StartCooldownTimer(90));
    }

    public void InvalidTeleportLocation() {
        uIDelegation.ShowError("INVALID LOCATION!");
    }

    public void SwapPower(int powerIndex) {

        foreach (var power in powers) {
            power.IsEquipped = false;
            powerPanels[power.Index].transform.GetChild(5).gameObject.SetActive(true);
        }

        powers[powerIndex].IsEquipped = true;
        powerIconImage.sprite = powers[powerIndex].PowerIconWhite;
        powerPanels[powerIndex].transform.GetChild(5).gameObject.SetActive(false);

        equippedPowers.Clear();
        equippedPowers.Add(powers[powerIndex].Name);
    }

    private IEnumerator StartCooldownTimer(int time) {
        cooldownTimer = time;

        int minutes;
        int seconds;

        powerIconImage.gameObject.SetActive(false);
        powerButton.interactable = false;
        powerCooldownTimer.gameObject.SetActive(true);

        while (cooldownTimer > 0) {
            minutes = cooldownTimer / 60;
            seconds = cooldownTimer % 60;
            powerCooldownTimer.text = $"{minutes}:{seconds:D2}";

            yield return new WaitForSeconds(1);            

            cooldownTimer--;
        }

        powerCooldownTimer.gameObject.SetActive(false);
        powerButton.interactable = true;
        powerIconImage.gameObject.SetActive(true);
    }

    public void LoadData(GameData data)
    {
        this.visionRadius = data.visionRadius;
        this.visionBoost = data.visionBoost;
        this.refineryProfitMultiplier = data.refineryProfitMultiplier;
        this.refineryProfitMultiplierBoost = data.refineryProfitMultiplierBoost;
        this.destroyRadius = data.destroyRadius;
        this.cooldownTimer = data.cooldownTimer;
        this.equippedPowers = data.equippedPowers;

        powers.Add(new(() => SurveyRadar(), "SURVEY RADAR", "REVEALS NEARBY ORES", 0, new int[0], powerIcons[0], powerIconsWhite[0], 0, false));
        powers.Add(new(() => ExplosiveCharge(), "EXPLOSIVE CHARGE", "DESTROYS NEARBY ORES", 1, new int[0], powerIcons[1], powerIconsWhite[1], 1, false));
        powers.Add(new(() => ShowTeleporter(), "TELEPORTER", "INSTANTLY RELOCATES VEHICLE", 2, new int[0], powerIcons[2], powerIconsWhite[2], 3, false));

        int powerIndex = 0;

        foreach (var power in powers) {
            if (equippedPowers[0] == power.Name) {
                powerIndex = power.Index;
            }
        }

        SwapPower(powerIndex);

        GameObject explosionEffectGO = Instantiate(explosionEffect, playerVehicle.position, new());
        explosionController = explosionEffectGO.GetComponent<ExplosionController>();

        StartCoroutine(StartCooldownTimer(cooldownTimer));
    }

    public void SaveData(ref GameData data)
    {
        data.visionRadius = this.visionRadius;
        data.visionBoost = this.visionBoost;
        data.refineryProfitMultiplier = this.refineryProfitMultiplier;
        data.refineryProfitMultiplierBoost = this.refineryProfitMultiplierBoost;
        data.destroyRadius = this.destroyRadius;
        data.cooldownTimer = this.cooldownTimer;
        data.equippedPowers = this.equippedPowers;
    }
}