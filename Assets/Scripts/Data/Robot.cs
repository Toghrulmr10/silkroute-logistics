public enum RobotStatus { Idle, Picking, Packing }

[System.Serializable]
public class Robot
{
    public string      Id;
    public RobotStatus Status        = RobotStatus.Idle;
    public float       Speed         = 1.0f;
    public int         EnergyPerTask = 5;
    public int         CreditPerTask = 2;
    public int         CurrentOrderId = -1;

    public Robot(string id) => Id = id;

    public bool IsIdle => Status == RobotStatus.Idle;
}
