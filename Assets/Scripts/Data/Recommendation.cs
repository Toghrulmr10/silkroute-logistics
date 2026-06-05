public class Recommendation
{
    public int                 OrderId;
    public Order               Order;
    public RecommendationPlan  Recommended;
    public RecommendationPlan[] Alternatives; // max 2
    public bool                AiActive = true; // ROUTE AI bu gün aktivdirmi (1-3-cü günlər: false)
}
