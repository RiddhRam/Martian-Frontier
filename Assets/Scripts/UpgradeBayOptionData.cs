using UnityEngine;

public struct UpgradeBayOptionData
{
    public GameObject GO;
    public string upgradeType;
    public int iteration;
    public ulong price;

    public UpgradeBayOptionData(GameObject GO, string upgradeType, int iteration, ulong price)
    {
        this.GO = GO;
        this.upgradeType = upgradeType;
        this.iteration = iteration;
        this.price = price;
    }
}