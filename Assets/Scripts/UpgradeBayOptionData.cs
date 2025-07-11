using System.Collections.Generic;

public struct UpgradeBayOptionData
{
    public string upgradeType;
    public ulong price;
    public int imageIndex;
    // Used for any extra data needed, like when purchasing an ore profit, we need the ore index
    public int[] extraData;

    public UpgradeBayOptionData(string upgradeType, ulong price, int imageIndex, int[] extraData = null)
    {
        this.upgradeType = upgradeType;
        this.price = price;
        this.imageIndex = imageIndex;
        this.extraData = extraData;
    }
}