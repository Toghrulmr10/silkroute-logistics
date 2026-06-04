using System;

public static class EventBus
{
    // ── Sprint 1-3: Mövcud hadisələr ───────────────────────────────────────────
    public static event Action<Order> OnOrderCreated;
    public static event Action<Order> OnOrderDelivered;
    public static event Action<Order> OnOrderFailed;
    public static event Action<float> OnMoneyChanged;
    public static event Action<int>   OnDayChanged;

    public static event Action<Recommendation>            OnRecommendationReady;
    public static event Action<int, RecommendationPlan>   OnPlayerDecided;

    public static event Action<Robot, Order>              OnRobotStarted;
    public static event Action<Robot, Order>              OnRobotFinished;
    public static event Action<Drone, Order>              OnDroneLaunched;
    public static event Action<Drone, Order, bool>        OnDroneDelivered;
    public static event Action<Courier, Order, bool>      OnCourierDelivered;
    public static event Action<int, int>                  OnReputationChanged;
    public static event Action<int, int>                  OnEnergyChanged;

    // ── Sprint 5: Yeni hadisələr ────────────────────────────────────────────────
    public static event Action<int, DayConfig>  OnDayStarted;
    public static event Action<int>             OnDayEnded;
    public static event Action<string>          OnShopItemPurchased;
    public static event Action                  OnGameWon;
    public static event Action<string>          OnGameLost;

    // ── Sprint 1-3: Emit metodları ──────────────────────────────────────────────
    public static void OrderCreated(Order o)                          => OnOrderCreated?.Invoke(o);
    public static void OrderDelivered(Order o)                        => OnOrderDelivered?.Invoke(o);
    public static void OrderFailed(Order o)                           => OnOrderFailed?.Invoke(o);
    public static void MoneyChanged(float amount)                     => OnMoneyChanged?.Invoke(amount);
    public static void DayChanged(int day)                            => OnDayChanged?.Invoke(day);
    public static void RecommendationReady(Recommendation r)          => OnRecommendationReady?.Invoke(r);
    public static void PlayerDecided(int id, RecommendationPlan p)    => OnPlayerDecided?.Invoke(id, p);
    public static void RobotStarted(Robot r, Order o)                 => OnRobotStarted?.Invoke(r, o);
    public static void RobotFinished(Robot r, Order o)                => OnRobotFinished?.Invoke(r, o);
    public static void DroneLaunched(Drone d, Order o)                => OnDroneLaunched?.Invoke(d, o);
    public static void DroneDelivered(Drone d, Order o, bool ok)      => OnDroneDelivered?.Invoke(d, o, ok);
    public static void CourierDelivered(Courier c, Order o, bool ok)  => OnCourierDelivered?.Invoke(c, o, ok);
    public static void ReputationChanged(int val, int delta)          => OnReputationChanged?.Invoke(val, delta);
    public static void EnergyChanged(int val, int delta)              => OnEnergyChanged?.Invoke(val, delta);

    // ── Sprint 5: Emit metodları ────────────────────────────────────────────────
    public static void DayStarted(int day, DayConfig cfg)  => OnDayStarted?.Invoke(day, cfg);
    public static void DayEnded(int day)                   => OnDayEnded?.Invoke(day);
    public static void ShopItemPurchased(string id)        => OnShopItemPurchased?.Invoke(id);
    public static void GameWon()                           => OnGameWon?.Invoke();
    public static void GameLost(string reason)             => OnGameLost?.Invoke(reason);
}
