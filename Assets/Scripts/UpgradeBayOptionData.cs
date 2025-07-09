using UnityEngine;

public struct UpgradeBayOptionData
{
    public GameObject GO;
    public string name;
    public int iteration;

    public UpgradeBayOptionData(GameObject GO, string name, int iteration)
    {
        this.GO = GO;
        this.name = name;
        this.iteration = iteration;
    }
}