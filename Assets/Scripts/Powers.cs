using System;
using UnityEngine;

public class Powers
{
    public Action PowerFunction;
    public string Name;
    public string Description;
    public int Index;
    public int[] Prices;
    public Sprite PowerIcon;
    public Sprite PowerIconWhite;
    public int MinLevelRequired;
    public bool IsEquipped { get; set; }

    public Powers(Action powerFunction, string name, string description,
                  int index, int[] prices, Sprite powerIcon, Sprite powerIconWhite, int minLevelRequired, bool isEquipped)
    {
        PowerFunction = powerFunction;
        Name = name;
        Description = description;
        Index = index;
        Prices = prices;
        PowerIcon = powerIcon;
        PowerIconWhite = powerIconWhite;
        MinLevelRequired = minLevelRequired;
        IsEquipped = isEquipped;
    }

    public void ActivatePower()
    {
        PowerFunction?.Invoke();
    }
}