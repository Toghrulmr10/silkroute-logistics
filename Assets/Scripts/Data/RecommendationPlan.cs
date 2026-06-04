public enum DeliveryMethod { RobotDrone, CourierOnly, TwoDrones }
public enum RiskLevel      { Low, Medium, High }

[System.Serializable]
public class RecommendationPlan
{
    public DeliveryMethod Method;
    public string         RobotId   = "";
    public string         DroneId   = "";
    public string         CourierId = "";
    public float          EstTimeSec;
    public int            EstCost;
    public RiskLevel      Risk;
    public float          SuccessProbability;
    public string         Reasoning;
    public float          Score;

    public string MethodLabel => Method switch
    {
        DeliveryMethod.RobotDrone  => "Robot + Dron",
        DeliveryMethod.TwoDrones   => "2× Dron",
        _                          => "Kuryer"
    };

    public string RiskLabel => Risk switch
    {
        RiskLevel.Low    => "Aşağı",
        RiskLevel.Medium => "Orta",
        _                => "Yüksək"
    };
}
