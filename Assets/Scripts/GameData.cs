using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // Make everything have [Serialize Field] or public or else it won't be loaded or saved
    // Not sure about [Serialize Field] in production, but it works in development
    // Public works in production for sure
    public string userCash;
    public string userXP;
    public string blocksMined;
    public string materialsSold;
    public string moneyEarned;
    public Vector3 playerPos;
    public float playerRotation;
    public List<string> vehiclesOwned;
    public string currentVehicle;
    public int[] haulerCargo;
    // Uncollected materials
    public SerializableDictionary<string, MaterialManagerData> materials;
    public float refineryCapacity;
    public float refineryBattery;
    // Keep track of both in user used a vision boost when destroying tiles. Reveal all tiles first, then set destroyed ones to null
    public SerializableDictionary<Vector2Int, int>[,] destroyedTilemapsTileValues;
    public SerializableDictionary<Vector2Int, int>[,] revealedTilemapsTileValues;
    public int seed;
    public int highestRow;
    public int mineInitialization;
    public float rebirthProfitMultiplier;
    public bool finishedTutorial;
    public bool askedForReview;
    public int lastChallengeDate;
    public int[] challengeProgress;
    public bool[] challengeCollection;
    public int superChallengeTimer;
    public string userGems;
    public string gemsEarned;
    public int currentOresMined;
    public long gemRewardsToCollect;
    public int tutorialScreenIndex;
    public SerializableDictionary<string, int> vehicleUpgradeLevels;
    public float[] materialProfitMultipliers;
    public int cratesAvailable;
    public int progressToNextCrate;
    public string currentCoopVehicle;
    public int rewardAdTimer;
    public int visionRadius;
    public int visionBoost;
    public float refineryProfitMultiplier;
    public float refineryProfitMultiplierBoost;
    public int destroyRadius;

    public GameData() {
        this.userCash = "0";
        this.userXP = "0";
        this.playerPos = new(4.5f, 5.4f, 0);
        this.playerRotation = 180;
        this.blocksMined = "0";
        this.materialsSold = "0";
        this.moneyEarned = "0";
        this.vehiclesOwned = new List<string> { "GRINDER I", "STUBBY" };
        this.currentVehicle = "GRINDER I";
        this.haulerCargo = new int[9];
        this.materials = new();
        this.refineryCapacity = 120;
        this.refineryBattery = 120;
        // SEARCH FOR [42] TO FIND ALL OCCURRENCES OF THE LENGTH, THERE MAY BE MORE IN DEPTH STUFF IN MineRenderer.cs
        this.destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[6, 42];
        this.revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[6, 42];
        
        for (int i = 0; i != this.destroyedTilemapsTileValues.GetLength(0); i++) {
            for (int j = 0; j < this.destroyedTilemapsTileValues.GetLength(1); j++)
            {
                this.destroyedTilemapsTileValues[i, j] = new SerializableDictionary<Vector2Int, int>();
                this.revealedTilemapsTileValues[i, j] = new SerializableDictionary<Vector2Int, int>();
            }
        }
        
        this.seed = 0;
        this.highestRow = 0;
        this.mineInitialization = 0;
        this.rebirthProfitMultiplier = 0;
        this.finishedTutorial = false;
        this.askedForReview = false;
        this.lastChallengeDate = (int) (DateTime.UtcNow.Date - new DateTime(2024, 12, 8, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        this.challengeProgress = new int[6];
        this.challengeCollection = new bool[6];
        this.superChallengeTimer = 1200;
        this.userGems = "0";
        this.gemsEarned = "0";
        this.currentOresMined = 0;
        this.gemRewardsToCollect = 0;
        this.tutorialScreenIndex = 0;
        this.vehicleUpgradeLevels = new();
        this.materialProfitMultipliers = new float[9];
        this.cratesAvailable = 0;
        this.progressToNextCrate = 0;
        this.currentCoopVehicle = "GRINDER I";
        this.rewardAdTimer = 0;
        this.visionRadius = 10;
        this.visionBoost = 3;
        this.refineryProfitMultiplier = 1;
        this.refineryProfitMultiplierBoost = 2;
        this.destroyRadius = 6;
    }
}
