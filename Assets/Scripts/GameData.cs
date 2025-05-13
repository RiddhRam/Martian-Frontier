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
    public float highestMined;
    public Vector3 playerPos;
    public float playerRotation;
    public List<string> vehiclesOwned;
    public string currentVehicle;
    
    public int refineryTimer;
    // Keep track of both in user used a vision boost when destroying tiles. Reveal all tiles first, then set destroyed ones to null
    public SerializableDictionary<Vector2Int, int>[,] destroyedTilemapsTileValues;
    public SerializableDictionary<Vector2Int, int>[,] revealedTilemapsTileValues;
    public int seed;
    public int highestRow;
    public int mineInitialization;

    public bool finishedTutorial;
    public bool askedForReview;
    public int lastChallengeDate;
    public int[] challengeProgress;
    public bool[] challengeCollection;
    public int superChallengeTimer;
    public string userGems;
    public string gemsEarned;
    // Actually current blocks mined, not ores
    public int currentOresMined;
    public long gemRewardsToCollect;
    public int tutorialScreenIndex;

    public SerializableDictionary<string, VehicleUpgrade> vehicleUpgradeLevels;
    public SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    public List<string> customizationsOwned;


    public float[] materialProfitMultipliers;
    public int cratesAvailable;
    public int progressToNextCrate;
    public string currentCoopVehicle;
    public int rewardAdTimer;
    public int cooldownTimer;
    public List<string> equippedPowers;
    public SerializableDictionary<string, int> powerUpgradeLevels;
    public string userCredits;
    public int twoDayIntervals;
    public SerializableDictionary<string, int> magnetHaulerUpgrades;
    public int magnetHaulerAdTimer;
    public int[] magnetHaulerChallengeProgress;
    public bool[] magnetHaulerChallengeCollection;
    public int magnetHaulerSuperChallengeTimer;
    public SerializableDictionary<string, int> oreBlasterUpgrades;
    public int oreBlasterAdTimer;
    public int[] oreBlasterChallengeProgress;
    public bool[] oreBlasterChallengeCollection;
    public int oreBlasterSuperChallengeTimer;

    // bp = Beta Player. 0 = not a beta player, 2 = beta player
    public int bp;

    // the first version (android bundle) id that this player last played on. Can also be found in CloudDelegator.cs
    public int id;

    public GameData() {
        // Starter cash
        this.userCash = "10000";
        this.userXP = "0";
        this.playerPos = new(0, 3, 0);
        this.playerRotation = 180;
        this.blocksMined = "0";
        this.materialsSold = "0";
        this.moneyEarned = "0";

        // This is just so the supply crates rewards and other things aren't too low
        this.highestMined = 5_000;
        
        this.vehiclesOwned = new List<string> { "GRINDER" };
        this.currentVehicle = "GRINDER";

        this.refineryTimer = 120;
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
        this.vehicleCustomizations = new();
        this.customizationsOwned = new();

        this.materialProfitMultipliers = new float[9];
        this.cratesAvailable = 0;
        this.progressToNextCrate = 0;
        this.currentCoopVehicle = "GRINDER";
        this.rewardAdTimer = 0;
        this.cooldownTimer = 0;
        this.equippedPowers = new() { "SURVEY RADAR" };
        this.powerUpgradeLevels = new();
        
        this.userCredits = "0";
        this.twoDayIntervals = 0;

        this.magnetHaulerUpgrades = new();
        this.magnetHaulerAdTimer = 0;
        this.magnetHaulerChallengeProgress = new int[6];
        this.magnetHaulerChallengeCollection = new bool[6];
        this.magnetHaulerSuperChallengeTimer = 1200;

        this.oreBlasterUpgrades = new();
        this.oreBlasterAdTimer = 0;
        this.oreBlasterChallengeProgress = new int[6];
        this.oreBlasterChallengeCollection = new bool[6];
        this.oreBlasterSuperChallengeTimer = 1200;

        this.bp = 0;
        
        this.id = 108;
    }
}
