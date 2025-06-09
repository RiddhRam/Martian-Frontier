using System;

[Serializable]
public class VehicleUpgrade
{
    public int heatLevel;
    public int coolLevel;

    // Just saves the levels

    public VehicleUpgrade(int heatLevel, int coolLevel)
    {
        this.heatLevel = heatLevel;
        this.coolLevel = coolLevel;
    }
}
