using System;

[Serializable]
public class VehicleCustomization
{
    public string body;
    public string drill;

    // Just saves the names, nothing else

    public VehicleCustomization(string body, string drill)
    {
        this.body  = body;
        this.drill = drill;
    }
}
