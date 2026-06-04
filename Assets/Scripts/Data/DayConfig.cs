[System.Serializable]
public class DayConfig
{
    public int    dayNumber;
    public string title;
    public int    orderCount;
    public string weather;
    public bool   unlockRobot;
    public bool   unlockDrone;
    public bool   routeAiActive;
    public string goalDescription;
    public int    goalDeliveries;
    public int    goalReputation;
    public float  goalMoney;
    public float  goalSuccessRate;
}

[System.Serializable]
public class DayConfigList
{
    public DayConfig[] days;
}
