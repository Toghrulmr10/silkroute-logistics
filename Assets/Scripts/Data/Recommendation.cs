public class Recommendation
{
    public int                 OrderId;
    public Order               Order;
    public RecommendationPlan  Recommended;
    public RecommendationPlan[] Alternatives; // max 2
}
