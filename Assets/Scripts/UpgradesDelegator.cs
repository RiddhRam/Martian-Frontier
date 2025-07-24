using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class UpgradesDelegator : MonoBehaviour, IDataPersistence
{
    [SerializeField] private Transform playerVehicle;
    [SerializeField] private MineRenderer mineRenderer;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private Image powerIconImage;
    [SerializeField] private Button powerButton;
    [SerializeField] private TextMeshProUGUI powerCooldownTimer;
    [SerializeField] private GameObject[] powerPanels;
    [SerializeField] private GameObject[] powerLockedPanels;

    [SerializeField] private Button unlockPowerButton;


    [SerializeField] private AudioSource powerUpAudioSource;
    [SerializeField] private AudioClip[] powerUpAudioClips;
    [SerializeField] private float[] audioVolumes;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Color surveyRadarColor;
    [SerializeField] private Sprite[] powerIconsWhite;

    private OreDelegation oreDelegation;

    readonly HashSet<Vector2Int> tilesToDestroy = new();
    readonly HashSet<Vector2Int> tilesToReveal = new();

    // Cache
    Tilemap tilemap;
    Vector3Int spriteTilePos;
    readonly List<Vector3> tileWorldPositions = new();
    readonly List<TileBase> tileBasesToDestroy = new();

    ExplosionController explosionController;

    private List<string> equippedPowers = new();
    private List<Powers> powers = new();

    public int cooldownTimer = 0;
    [SerializeField] private SerializableDictionary<string, int> powerUpgradeLevels;
    private int powersUnlocked = 1;
    // Reduce Cooldown
    public int cooldown = 90;
    // Increase Rewards
    public float crateMultiplier;

    // Survey Radar
    public int surveyVisionRadius;
    // Explosive Charge
    public int destroyRadius;
    // Increase Profits
    public float profitMultiplier;

    public bool scannedForOres = false;

    // Length: 20
    // Sum: 578,800
    private readonly int[] upgradeCashPrices = {
        2000,
        2500,
        3100,
        3800,
        4700,
        5800,
        7100,
        8800,
        11000,
        13000,
        17000,
        21000,
        25000,
        32000,
        39000,
        48000,
        60000,
        74000,
        91000,
        110000
    };

    void Start()
    {
        oreDelegation = mineRenderer.oreDelegation;
    }
    // Increase Vision
    public int visionBoost;

    public void UsePower() {
        // Just for tutorial usage
        scannedForOres = true;
        
        foreach (var power in powers) {
            if (power.Name == equippedPowers[0]) {
                power.ActivatePower();
                AnalyticsDelegator.Instance.UsePower(power.Name);
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

        for (int x = -surveyVisionRadius; x <= surveyVisionRadius; x++)
        {
            for (int y = -surveyVisionRadius; y <= surveyVisionRadius; y++)
            {
                if (x * x + y * y <= surveyVisionRadius * surveyVisionRadius) // Check if inside circle
                {
                    AddTileIfOre(new(playerPos.x + x, playerPos.y - y));
                }
            }
        }

        mineRenderer.RevealTiles(tilesToReveal);
        
        AudioDelegator.Instance.PlayAudio(powerUpAudioSource, powerUpAudioClips[0], audioVolumes[0]);
        
        StartCoroutine(StartCooldownTimer(cooldown));
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

        // Outer ring
        if (circleColor == surveyRadarColor) {
            circle.startWidth = 1f;
            circle.endWidth = 1f;
        } 
        // Inner ring
        else {
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

        GameObject outerCircle = CreateCircle(surveyVisionRadius, surveyRadarColor, 40);
        GameObject innerCircle = CreateCircle(surveyVisionRadius, new(surveyRadarColor.r, surveyRadarColor.g, surveyRadarColor.b, 0.4f), 1000);

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
        scannerLine.SetPosition(0, start);

        while (angle > -270f)
        {
            angle -= 200f * Time.deltaTime; // 200f = degrees per second
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 end = start + new Vector3(Mathf.Cos(rad) * surveyVisionRadius, Mathf.Sin(rad) * surveyVisionRadius, 0);
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
        // If the radius is very large, there's a chance that some ungenerated tiles will be destroyed
        // This gameobject activates any generation triggers if needed

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
        
        StartCoroutine(DestroyGroundCollider(colliderObject));

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

            mineRenderer.DestroyTiles(tilesToDestroy.ToList(), false, playerVehicle.position, true);
        }

        tileWorldPositions.Clear();
        tileBasesToDestroy.Clear();

        AudioDelegator.Instance.PlayAudio(powerUpAudioSource, powerUpAudioClips[1], audioVolumes[1]);
        StartCoroutine(StartCooldownTimer(cooldown));
    }

    private IEnumerator DestroyGroundCollider(GameObject colliderGameObject) {
        yield return new WaitForSeconds(4);

        Destroy(colliderGameObject);
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

        tilesToDestroy.Add(new(currentTilePos.x, currentTilePos.y));
        tileBasesToDestroy.Add(tilemap.GetTile(currentTilePos));
        tileWorldPositions.Add(tilemap.GetCellCenterWorld(currentTilePos));
    }

    [ContextMenu("Show Teleporter")]
    private void ShowTeleporter() {
        UIDelegation.Instance.HideAll();
        UIDelegation.Instance.ToggleCamera();
        UIDelegation.Instance.RevealElement(teleportPanel);
    }

    public void Teleport(Vector3 newPosition) {
        // Make sure rows are generated
        mineRenderer.TriggerAllGenerationTriggersAbove(newPosition.y);

        // Move player
        playerVehicle.position = new(newPosition.x, newPosition.y);

        // Back to game
        UIDelegation.Instance.HideElement(teleportPanel);
        UIDelegation.Instance.RevealAll();
        UIDelegation.Instance.ToggleCamera();

        AudioDelegator.Instance.PlayAudio(powerUpAudioSource, powerUpAudioClips[2], audioVolumes[2]);
        StartCoroutine(StartCooldownTimer(cooldown));
    }

    public void InvalidTeleportLocation() {
        UIDelegation.Instance.ShowError("INVALID LOCATION!");
    }

    public void UpgradePower(string powerName) {
        int powerIndex = 0;

        foreach (var power in powers) {
            if (powerName == power.Name) {
                powerIndex = power.Index;
                break;
            }
        }

        int powerLevel = GetPowerLevel(powerName);

        if (!playerState.VerifyEnoughGems(powers[powerIndex].Prices[powerLevel])) {
            UIDelegation.Instance.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.SubtractGems(powers[powerIndex].Prices[powerLevel]);
        powerUpgradeLevels[powerName] = powerLevel + 1;
        SetUpgradePriceAndLevel(powerIndex);

        powers[powerIndex].UpdatePower();

        AnalyticsDelegator.Instance.TechLabUpgrade(powerName);
    }

    public void UpdateRadar() {
        Powers power = powers[0];
        surveyVisionRadius = (int)(power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name));
    }

    public void UpdateExplosive() {
        Powers power = powers[1];
        destroyRadius = (int)(power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name));
    }

    public void UpdateCooldown() {
        Powers power = powers[3];

        cooldown = (int)(power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name));
        if (cooldownTimer > cooldown) {
            cooldownTimer = cooldown;
        }
    }

    public void UpdateRewardBoost() {
        Powers power = powers[4];
        crateMultiplier = power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name);
    }

    public void UpdateProfitBoost() {
        Powers power = powers[5];
        profitMultiplier = power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name);
    }

    public void UpdateVisionBoost() {
        Powers power = powers[6];
        visionBoost = (int)(power.Level0Value + power.UpgradeValue * GetPowerLevel(power.Name));
    }

    public int GetPowerLevel(string powerName) {
        int powerLevel = 0;

        if (powerUpgradeLevels.ContainsKey(powerName)) {
            powerLevel = powerUpgradeLevels[powerName];
        }

        return powerLevel;
    }

    public void SwapPower(int powerIndex) {

        foreach (var power in powers) {
            power.IsEquipped = false;

            if (power.IsPassive) {
                // Passive powers are always active, don't show equip button
                powerPanels[power.Index].transform.GetChild(6).gameObject.SetActive(false);
            } else {
                powerPanels[power.Index].transform.GetChild(6).gameObject.SetActive(true);
            }
        }

        powers[powerIndex].IsEquipped = true;
        powerIconImage.sprite = powers[powerIndex].PowerIconWhite;
        powerPanels[powerIndex].transform.GetChild(6).gameObject.SetActive(false);

        equippedPowers.Clear();
        equippedPowers.Add(powers[powerIndex].Name);

        try {
            AnalyticsDelegator.Instance.EquipPower(powers[powerIndex].Name);
        } catch {

        }
        
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

    public void UnlockNewPower()
    {
        if (!playerState.VerifyEnoughGems(GetUnlockPrice()))
        {
            UIDelegation.Instance.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.SubtractGems(GetUnlockPrice());
        powersUnlocked++;

        UpdatePowerVisibility();
    }

    public void UpdatePowerVisibility()
    {

        foreach (var power in powers)
        {
            // Disable if index is higher than or equal to number of powers unlocked
            if (power.Index >= powersUnlocked)
            {
                powerPanels[power.Index].SetActive(false);
                powerLockedPanels[power.Index].SetActive(true);
            }
            // Enable otherwise
            else
            {
                powerPanels[power.Index].SetActive(true);
                powerLockedPanels[power.Index].SetActive(false);
            }
        }

        // Max powers unlocked
        if (powersUnlocked >= powers.Count)
        {
            // Show button as disabled
            unlockPowerButton.interactable = false;
            unlockPowerButton.GetComponent<Image>().color = new(1, 0, 0);

            // Change text
            unlockPowerButton.transform.GetChild(0).gameObject.SetActive(false);
            unlockPowerButton.transform.GetChild(1).gameObject.SetActive(true);
        }
        // Update price
        else
        {
            unlockPowerButton.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = playerState.FormatPrice(GetUnlockPrice());
        }
    }

    public void SetUpgradePriceAndLevel(int powerIndex) {
        Powers power = powers[powerIndex];

        int powerLevel = GetPowerLevel(power.Name);

        // Level
        powerPanels[powerIndex].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue("LEVEL {0}", powerLevel);
        // Power value
        powerPanels[powerIndex].transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = GetLocalizedValue(power.MainValueKey, power.Level0Value + power.UpgradeValue * powerLevel);

        // Upgrade button
        Transform upgradeButton = powerPanels[powerIndex].transform.GetChild(7);
        // Max level
        if (powerLevel >= powers[powerIndex].Prices.Length)
        {
            upgradeButton.GetComponent<Button>().interactable = false;
            upgradeButton.GetComponent<Image>().color = new(1, 0, 0);

            upgradeButton.GetChild(0).GetChild(0).gameObject.SetActive(false);
            upgradeButton.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = "MAX";
        }
        else
        {
            // Update price
            upgradeButton.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = playerState.FormatPrice(power.Prices[powerLevel]);
        }
    }

    public int GetUnlockPrice()
    {
        // m = 9000, x = powersUnlocked, b = 6000
        int price = (9000 * powersUnlocked) + 6000;

        if (price < 0)
        {
            price = 0;
        }

        return price;
    }

    public void LoadData(GameData data)
    {
        this.cooldownTimer = data.cooldownTimer;
        this.equippedPowers = data.equippedPowers;
        this.powerUpgradeLevels = data.powerUpgradeLevels;
        this.powersUnlocked = data.powersUnlocked;

        powers.Add(new(() => SurveyRadar(), "SURVEY RADAR", "REVEALS NEARBY ORES", 0, upgradeCashPrices, powerIconsWhite[0], false, false, "{0} BLOCKS", 12, 1, () => UpdateRadar()));
        powers.Add(new(() => ExplosiveCharge(), "EXPLOSIVE CHARGE", "DESTROYS NEARBY ORES", 1, upgradeCashPrices, powerIconsWhite[1], false, false, "{0} BLOCKS", 12, 1, () => UpdateExplosive()));
        powers.Add(new(() => ShowTeleporter(), "TELEPORTER", "INSTANTLY RELOCATES VEHICLE", 2, new int[0], powerIconsWhite[2], false, false, "", 0, 0, () => { }));
        powers.Add(new(() => { }, "REDUCE COOLDOWN", "REUSE POWERS FASTER", 3, upgradeCashPrices, null, false, true, "{0} SECONDS", 90, -2, () => UpdateCooldown()));
        powers.Add(new(() => { }, "INCREASE REWARD", "EARN MORE FROM SUPPLY CRATES", 4, upgradeCashPrices, null, false, true, "{0}X", 0, 0.05f, () => UpdateRewardBoost()));
        powers.Add(new(() => { }, "INCREASE PROFIT", "EXTRA PROFIT BOOST", 5, upgradeCashPrices, null, false, true, "{0}X", 0, 0.05f, () => UpdateProfitBoost()));
        powers.Add(new(() => { }, "INCREASE VISION", "SEE FURTHER WHEN MINING", 6, new int[7] { 2000, 4700, 11000, 26000, 61000, 140000, 340000 }, null, false, true, "{0} BLOCKS", 3, 1, () => UpdateVisionBoost()));

        int powerIndex = 0;

        foreach (var power in powers)
        {
            if (equippedPowers[0] == power.Name)
            {
                powerIndex = power.Index;
            }

            SetUpgradePriceAndLevel(power.Index);
            power.UpdatePower();
        }

        UpdateAllPowerPanels();

        SwapPower(powerIndex);

        GameObject explosionEffectGO = Instantiate(explosionEffect, playerVehicle.position, new());
        explosionController = explosionEffectGO.GetComponent<ExplosionController>();

        StartCoroutine(StartCooldownTimer(cooldownTimer));

        UpdatePowerVisibility();
    }

    public void SaveData(ref GameData data)
    {
        data.cooldownTimer = this.cooldownTimer;
        data.equippedPowers = this.equippedPowers;
        data.powerUpgradeLevels = this.powerUpgradeLevels;
        data.powersUnlocked = this.powersUnlocked;
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key);;

        // If no translation, just return the key
        if (entry == null) {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public void UpdateAllPowerPanels() {
        foreach (var power in powers) {
            SetUpgradePriceAndLevel(power.Index);
        }
    }
}