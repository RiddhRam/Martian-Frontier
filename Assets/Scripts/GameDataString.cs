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
    public string haulerCargo;
    // Uncollected materials
    public string materials;
    public string refineryInefficiency;
    public string refineryCapacity;
    public string refineryBattery;
    public string destroyedTilemapsTileValues;
    public string revealedTilemapsTileValues;
    public string seed;
    public string highestRow;
    public string mineInitialization;
    public string timerIndexes;
    public string rebirthProfitMultiplier;
    public string finishedTutorial;
}
