using System;
using UnityEngine;

public class Powers
{
    public Action PowerFunction;
    public string Name;
    public string Description;
    public int Index;
    public int[] Prices;
    public Sprite PowerIconWhite;
    public int MinLevelRequired;
    public bool IsEquipped;
    public bool IsPassive;
    public string MainValueKey;
    public float Level0Value;
    public float UpgradeValue;

    public Powers(Action powerFunction, string name, string description,
                  int index, int[] prices, Sprite powerIconWhite, int minLevelRequired, 
                  bool isEquipped, bool isPassive, string mainValueKey, float level0Value,
                  float upgradeValue)
    {
        PowerFunction = powerFunction;
        Name = name;
        Description = description;
        Index = index;
        Prices = prices;
        PowerIconWhite = powerIconWhite;
        MinLevelRequired = minLevelRequired;
        IsEquipped = isEquipped;
        IsPassive = isPassive;
        MainValueKey = mainValueKey;
        Level0Value = level0Value;
        UpgradeValue = upgradeValue;
    }

    public void ActivatePower()
    {
        PowerFunction?.Invoke();
    }
}