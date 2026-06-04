public enum DroneStatus { Idle, Loading, InFlight, Returning, Charging }

[System.Serializable]
public class Drone
{
    public string      Id;
    public DroneStatus Status        = DroneStatus.Idle;
    public float       Battery       = 100f;
    public int         MaxCharge     = 100;
    public float       SpeedKmh      = 40f;
    public float       MaxPayloadKg  = 6f;
    public int         CreditPerTask = 3;
    public int         CurrentOrderId = -1;

    public const float BatteryPerKm    = 5f;
    public const float ChargeRatePerSec = 20f;

    public Drone(string id) => Id = id;

    public bool IsIdle => Status == DroneStatus.Idle;

    public float RoundTripBatteryCost(float distanceKm) => distanceKm * BatteryPerKm * 2f;
}
