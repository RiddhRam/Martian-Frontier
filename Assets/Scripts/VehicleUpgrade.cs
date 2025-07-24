using System;

[Serializable]
public class VehicleUpgrade
{
    public int heatLevel;
    public int coolLevel;
    public int droneLevel;

    // Just saves the levels

    public VehicleUpgrade(int heatLevel, int coolLevel, int droneLevel)
    {
        this.heatLevel = heatLevel;
        this.coolLevel = coolLevel;
        this.droneLevel = droneLevel;
    }
}
