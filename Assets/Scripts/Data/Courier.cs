public enum CourierStatus { Idle, Delivering }

[System.Serializable]
public class Courier
{
    public string        Id;
    public CourierStatus Status       = CourierStatus.Idle;
    public float         SpeedKmh     = 15f;
    public float         SuccessRate  = 0.6f;
    public int           CreditCost   = 1;
    public int           CurrentOrderId = -1;

    public Courier(string id) => Id = id;

    public bool IsIdle => Status == CourierStatus.Idle;
}
