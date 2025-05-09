[System.Serializable]
public class GameDataString
{
    // This is going to be treated as a json version of GameData for when loading the game
    public string userCash;
    public string userXP;
    public string blocksMined;
    public string materialsSold;
    public string moneyEarned;
    public string playerPos;
    public string playerRotation;
    public string vehiclesOwned;
    public string currentVehicle;

    public string refineryTimer;
    public string destroyedTilemapsTileValues;
    public string revealedTilemapsTileValues;
    public string seed;
    public string highestRow;
    public string mineInitialization;

    public string finishedTutorial;
    public string askedForReview;
    public string lastChallengeDate;
    public string challengeProgress;
    public string challengeCollection;
    public string superChallengeTimer;
    public string userGems;
    public string gemsEarned;
    public string currentOresMined;
    public string gemRewardsToCollect;
    public string tutorialScreenIndex;

    public string vehicleUpgradeLevels;
    public string vehicleCustomizations;
    public string customizationsOwned;

    public string materialProfitMultipliers;
    public string cratesAvailable;
    public string progressToNextCrate;
    public string currentCoopVehicle;
    public string rewardAdTimer;
    public string cooldownTimer;
    public string equippedPowers;
    public string powerUpgradeLevels;
    public string userCredits;
    public string twoDayIntervals;
    public string magnetHaulerUpgrades;
    public string magnetHaulerAdTimer;
    public string magnetHaulerChallengeProgress;
    public string magnetHaulerChallengeCollection;
    public string magnetHaulerSuperChallengeTimer;
    public string oreBlasterUpgrades;
    public string oreBlasterAdTimer;
    public string oreBlasterChallengeProgress;
    public string oreBlasterChallengeCollection;
    public string oreBlasterSuperChallengeTimer;

    public GameDataString() {
        this.userCash = null;
        this.userXP = null;
        this.playerPos = null;
        this.playerRotation = null;
        this.blocksMined = null;
        this.materialsSold = null;
        this.moneyEarned = null;
        this.vehiclesOwned = null;
        this.currentVehicle = null;

        this.refineryTimer = null;
        this.destroyedTilemapsTileValues = null;
        this.revealedTilemapsTileValues = null;        
        this.seed = null;
        this.highestRow = null;
        this.mineInitialization = null;

        this.finishedTutorial = null;
        this.askedForReview = null;
        this.lastChallengeDate = null;
        this.challengeProgress = null;
        this.challengeCollection = null;
        this.superChallengeTimer = null;
        this.userGems = null;
        this.gemsEarned = null;
        this.currentOresMined = null;
        this.gemRewardsToCollect = null;
        this.tutorialScreenIndex = null;

        this.vehicleUpgradeLevels = null;
        this.vehicleCustomizations = null;
        this.customizationsOwned = null;

        this.materialProfitMultipliers = null;
        this.cratesAvailable = null;
        this.progressToNextCrate = null;
        this.currentCoopVehicle = null;
        this.rewardAdTimer = null;
        this.cooldownTimer = null;
        this.equippedPowers = null;
        this.powerUpgradeLevels = null;

        this.userCredits = null;
        this.twoDayIntervals = null;

        this.magnetHaulerUpgrades = null;
        this.magnetHaulerAdTimer = null;
        this.magnetHaulerChallengeProgress = null;
        this.magnetHaulerChallengeCollection = null;
        this.magnetHaulerSuperChallengeTimer = null;

        this.oreBlasterUpgrades = null;
        this.oreBlasterAdTimer = null;
        this.oreBlasterChallengeProgress = null;
        this.oreBlasterChallengeCollection = null;
        this.oreBlasterSuperChallengeTimer = null;
    }
}
