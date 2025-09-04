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
    public double highestMined;
    public List<string> vehiclesOwned;
    public string currentVehicle;
    
    public int mineCount;
    // Helps with onboarding, we can track if its the first time a player reaches a new mine
    public int highestLevelReached;
    // key: oreIndex. value: level
    public SerializableDictionary<int, int> oreUpgrades;
    public int targetDepth;

    public bool finishedTutorial;
    public bool askedForReview;
    public int lastChallengeDate;
    public int[] challengeProgress;
    public bool[] challengeCollection;
    public int superChallengeTimer;
    public string userGems;
    public string gemsEarned;
    // Current blocks mined, not ores
    public int currentOresMined;
    public long gemRewardsToCollect;

    public int tutorialScreenIndex;

    // For vehicle upgrade bay
    public SerializableDictionary<string, VehicleUpgrade> vehicleUpgradeLevels;
    public HashSet<string> upgradeBayOptionsPurchased;
    public SerializableDictionary<string, VehicleCustomization> vehicleCustomizations;
    public List<string> customizationsOwned;

    public int cratesAvailable;
    public int progressToNextCrate;

    public string currentCoopVehicle;

    public int cooldownTimer;
    public List<string> equippedPowers;
    public SerializableDictionary<string, int> powerUpgradeLevels;
    // The number of powers unlocked by the player
    public int powersUnlocked;

    // The ores that the player has discovered so far in the current mine
    public HashSet<int> discoveredOres;

    // Player leaderboard score
    public string playerLS;
    public int uniqueUserInt;
    // Two day interval leaderboards
    public int twoDIL;

    public string userCredits;
    
    // Two day interval minigames
    public int twoDIM;

    public SerializableDictionary<string, int> magnetHaulerUpgrades;
    public int[] magnetHaulerChallengeProgress;
    public bool[] magnetHaulerChallengeCollection;
    public int magnetHaulerSuperChallengeTimer;

    public SerializableDictionary<string, int> oreBlasterUpgrades;
    public int[] oreBlasterChallengeProgress;
    public bool[] oreBlasterChallengeCollection;
    public int oreBlasterSuperChallengeTimer;

    // The time the player last logged off
    public long offlineTime;

    // bp = Beta Player. 0 = not a beta player, 2 = beta player
    public int bp;

    // the first version (android bundle) id that this player last played on. Can also be found in CloudDelegator.cs
    public int id;

    public GameData() {
        // Starter cash
        this.userCash = "5";
        this.userXP = "0";
        this.blocksMined = "0";
        this.materialsSold = "0";
        this.moneyEarned = "0";

        // This is just so the supply crates rewards and other things aren't too low
        this.highestMined = 50_000;
        
        this.vehiclesOwned = new List<string> { "GRINDER" };
        this.currentVehicle = "GRINDER";

        this.mineCount = 1;
        this.highestLevelReached = 0;
        this.oreUpgrades = new();
        this.targetDepth = 1;

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
        this.upgradeBayOptionsPurchased = new();
        this.vehicleCustomizations = new();
        this.customizationsOwned = new();

        this.cratesAvailable = 0;
        this.progressToNextCrate = 0;
        this.currentCoopVehicle = "GRINDER";
        
        this.cooldownTimer = 0;
        this.equippedPowers = new() { "SURVEY RADAR" };
        this.powerUpgradeLevels = new();
        this.powersUnlocked = 1;

        this.discoveredOres = new();

        this.playerLS = "0";
        this.uniqueUserInt = 0;
        this.twoDIL = 0;
        
        this.userCredits = "0";
        this.twoDIM = 0;

        this.magnetHaulerUpgrades = new();
        this.magnetHaulerChallengeProgress = new int[6];
        this.magnetHaulerChallengeCollection = new bool[6];
        this.magnetHaulerSuperChallengeTimer = 1200;

        this.oreBlasterUpgrades = new();
        this.oreBlasterChallengeProgress = new int[6];
        this.oreBlasterChallengeCollection = new bool[6];
        this.oreBlasterSuperChallengeTimer = 1200;

        this.offlineTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        this.bp = 0;
        this.id = 147;
    }
}
