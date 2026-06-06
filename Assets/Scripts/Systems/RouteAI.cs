using System.Collections.Generic;
using UnityEngine;

public class RouteAI : MonoBehaviour
{
    public static RouteAI Instance { get; private set; }

    private int _rainFailureCount = 0;
    private float WeatherAdjustment => Mathf.Max(-0.20f, _rainFailureCount * -0.05f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EventBus.OnDroneDelivered += (_, order, success) =>
        {
            if (!success && GameState.Instance.Weather == "rain")
                _rainFailureCount++;
        };
    }

    // ── Əsas metod ───────────────────────────────────────────────────────────
    public Recommendation GenerateRecommendation(Order order)
    {
        var plans = new List<RecommendationPlan>();

        var rd = RobotDronePlan(order);
        if (rd != null) plans.Add(rd);

        var cr = CourierPlan(order);
        if (cr != null) plans.Add(cr);

        var td = TwoDronePlan(order);
        if (td != null) plans.Add(td);

        foreach (var p in plans)
            p.Score = CalcScore(p, order);

        plans.Sort((a, b) => b.Score.CompareTo(a.Score));

        var alts = new List<RecommendationPlan>();
        for (int i = 1; i < Mathf.Min(plans.Count, 3); i++)
            alts.Add(plans[i]);

        bool aiActive = CampaignManager.Instance == null || CampaignManager.Instance.RouteAiActive;

        var rec = new Recommendation
        {
            OrderId      = order.Id,
            Order        = order,
            Recommended  = plans.Count > 0 ? plans[0] : null,
            Alternatives = alts.ToArray(),
            AiActive     = aiActive
        };

        Debug.Log($"[ROUTE] ORD-{order.Id} → {plans.Count} plan. Tövsiyə: {rec.Recommended?.MethodLabel}");
        return rec;
    }

    // ── Plan hesablayıcılar ───────────────────────────────────────────────────
    private RecommendationPlan RobotDronePlan(Order order)
    {
        var robot = GameState.Instance.GetIdleRobot();
        var drone = GameState.Instance.GetIdleDrone();
        if (robot == null || drone == null)             return null;
        if (drone.MaxPayloadKg < order.WeightKg)        return null;

        float wf         = Mathf.Clamp(WeatherFactor() + WeatherAdjustment, 0.5f, 1f);
        float prep       = 5f / robot.Speed;
        float flight     = (order.DistanceKm / (drone.SpeedKmh * wf)) * 60f;
        float batCost    = drone.RoundTripBatteryCost(order.DistanceKm);
        float batFactor  = drone.Battery >= batCost * 2f ? 1f : 0.7f;
        float success    = 0.9f * wf * batFactor;
        if (GameState.Instance.RouteUpgraded) success = Mathf.Min(success + 0.08f, 1f);
        float estTime    = prep + flight;

        return new RecommendationPlan
        {
            Method             = DeliveryMethod.RobotDrone,
            RobotId            = robot.Id,
            DroneId            = drone.Id,
            EstTimeSec         = estTime,
            EstCost            = robot.CreditPerTask + drone.CreditPerTask,
            SuccessProbability = success,
            Risk               = CalcRisk(success, estTime, order.DeadlineSeconds),
            Reasoning          = $"{order.WeightKg:F1}kq dron üçün uyğundur. Hava: {GameState.Instance.Weather}."
        };
    }

    private RecommendationPlan CourierPlan(Order order)
    {
        var courier = GameState.Instance.GetIdleCourier();
        if (courier == null) return null;

        float delivTime = (order.DistanceKm / courier.SpeedKmh) * 60f;

        return new RecommendationPlan
        {
            Method             = DeliveryMethod.CourierOnly,
            CourierId          = courier.Id,
            EstTimeSec         = delivTime,
            EstCost            = courier.CreditCost,
            SuccessProbability = courier.SuccessRate,
            Risk               = RiskLevel.High,
            Reasoning          = "Ucuz, lakin yavaş və uğur ehtimalı aşağıdır."
        };
    }

    private RecommendationPlan TwoDronePlan(Order order)
    {
        var idles = GameState.Instance.Drones.FindAll(d => d.IsIdle && d.CurrentOrderId == -1);
        if (idles.Count < 2)                     return null;
        if (idles[0].MaxPayloadKg < order.WeightKg) return null;

        var drone    = idles[0];
        float wf     = Mathf.Clamp(WeatherFactor() + WeatherAdjustment, 0.5f, 1f);
        float flight = (order.DistanceKm / (drone.SpeedKmh * wf)) * 60f;
        float success = Mathf.Min(0.98f, 0.9f * wf * 1.1f);
        if (GameState.Instance.RouteUpgraded) success = Mathf.Min(success + 0.08f, 0.99f);

        return new RecommendationPlan
        {
            Method             = DeliveryMethod.TwoDrones,
            DroneId            = drone.Id,
            EstTimeSec         = flight,
            EstCost            = drone.CreditPerTask * 2,
            SuccessProbability = success,
            Risk               = CalcRisk(success, flight, order.DeadlineSeconds),
            Reasoning          = "2 dron: daha sürətli, redundans sayəsində etibarlı."
        };
    }

    // ── Köməkçi metodlar ──────────────────────────────────────────────────────
    private static float CalcScore(RecommendationPlan p, Order order)
    {
        float riskPenalty = p.Risk switch
        {
            RiskLevel.Medium => 10f,
            RiskLevel.High   => 25f,
            _                => 0f
        };
        return p.SuccessProbability * 100f
               - p.EstCost * 2f
               - (p.EstTimeSec > order.DeadlineSeconds ? 50f : 0f)
               - riskPenalty;
    }

    private static RiskLevel CalcRisk(float prob, float estTime, float deadline)
    {
        if (prob < 0.70f || estTime > deadline * 0.9f) return RiskLevel.High;
        if (prob < 0.85f)                              return RiskLevel.Medium;
        return RiskLevel.Low;
    }

    private float WeatherFactor() => GameState.Instance.Weather switch
    {
        "windy" => 0.85f,
        "rain"  => 0.70f,
        _       => 1.00f
    };
}
