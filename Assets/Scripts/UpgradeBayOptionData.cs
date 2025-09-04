using System.Collections.Generic;

public struct UpgradeBayOptionData
{
    public string upgradeType;
    // Some upgrade types have subtypes or dyanmic modifiers, like profits, so the string changes too. 
    // The base type helps us identify the upgrade type
    public string baseType;
    public ulong price;
    public int imageIndex;
    // Used for any extra data needed, like when purchasing an ore profit, we need the ore index
    public int[] extraData;

    public UpgradeBayOptionData(string upgradeType, string baseType, ulong price, int imageIndex, int[] extraData = null)
    {
        this.upgradeType = upgradeType;
        this.baseType = baseType;
        this.price = price;
        this.imageIndex = imageIndex;
        this.extraData = extraData;
    }
}