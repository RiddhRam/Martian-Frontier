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
    public float refineryInefficiency;
    public float refineryCapacity;
    public float refineryBattery;
    public SerializableDictionary<Vector2Int, int>[] destroyedTilemapsTileValues;
    public SerializableDictionary<Vector2Int, int>[] revealedTilemapsTileValues;
    public int seed;
    public int highestRow;
    public int mineInitialization;
    public int[] timerIndexes;

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
        this.refineryInefficiency = 100;
        this.refineryCapacity = 120;
        this.refineryBattery = 120;
        this.destroyedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[36];
        this.revealedTilemapsTileValues = new SerializableDictionary<Vector2Int, int>[36];
        
        for (int i = 0; i < this.destroyedTilemapsTileValues.Length; i++)
        {
            this.destroyedTilemapsTileValues[i] = new SerializableDictionary<Vector2Int, int>();
            this.revealedTilemapsTileValues[i] = new SerializableDictionary<Vector2Int, int>();
        }

        this.seed = 0;
        this.highestRow = 0;
        this.mineInitialization = 0;
        this.timerIndexes = new int[3];
    }
}
