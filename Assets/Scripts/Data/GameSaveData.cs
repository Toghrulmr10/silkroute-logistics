using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int              campaignDayIndex;
    public float            money;
    public int              reputation;
    public int              energy;
    public int              robotCount;
    public int              droneCount;
    public List<string>     purchasedUpgrades = new();
    public float            droneSpeedMult;
    public float            batteryMult;
    public bool             routeUpgraded;
}
