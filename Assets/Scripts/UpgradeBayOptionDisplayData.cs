using UnityEngine;

public class UpgradeBayOptionDisplayData
{
    public GameObject display;
    // Some upgrade types have subtypes or dyanmic modifiers, like profits, so the string changes too. 
    // The base type helps us identify the upgrade type
    public string baseType;
}