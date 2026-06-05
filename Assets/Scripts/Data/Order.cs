public enum OrderStatus
{
    New, Analyzing, AwaitingDecision, Preparing, Ready, InDelivery, Delivered, Failed
}

public enum OrderPriority { Low, Normal, High, Vip }
public enum DeliveryType  { Courier, Robot, Drone }   // display/coloring üçün

[System.Serializable]
public class Order
{
    public int           Id;
    public string        CustomerName;
    public string        ProductName;
    public float         WeightKg;
    public float         DistanceKm;
    public float         Revenue;
    public float         DeadlineSeconds;
    public OrderPriority Priority;
    public OrderStatus   Status;
    public DeliveryType  Type;
    public float         CreatedAt;

    public string AssignedRobotId   = "";
    public string AssignedDroneId   = "";
    public string AssignedDroneId2  = "";   // 2× Dron metodu üçün ikinci dron
    public string AssignedCourierId = "";

    public Order(int id, string customer, string product, float weight, float distance,
                 float revenue, float deadline, OrderPriority priority, float createdAt)
    {
        Id              = id;
        CustomerName    = customer;
        ProductName     = product;
        WeightKg        = weight;
        DistanceKm      = distance;
        Revenue         = revenue;
        DeadlineSeconds = deadline;
        Priority        = priority;
        Status          = OrderStatus.New;
        CreatedAt       = createdAt;
    }
}
