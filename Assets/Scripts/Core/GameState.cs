using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public float  Money         { get; private set; } = 500f;
    public int    Day           { get; private set; } = 1;
    public int    Reputation    { get; private set; } = 50;
    public int    Energy        { get; private set; } = 100;
    public string Weather       { get; private set; } = "clear";
    public int    CampaignIndex { get; private set; } = 0;

    public List<Robot>   Robots   { get; private set; } = new();
    public List<Drone>   Drones   { get; private set; } = new();
    public List<Courier> Couriers { get; private set; } = new();

    public List<string> PurchasedUpgrades { get; private set; } = new();
    public float DroneSpeedMult { get; private set; } = 1f;
    public float BatteryMult    { get; private set; } = 1f;
    public bool  RouteUpgraded  { get; private set; } = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitStartingResources();
    }

    private void InitStartingResources()
    {
        Couriers.Add(new Courier("CR-01"));
        // Robot ve dron kampaniya boyunca açılır
    }

    // ── Resurs kilidini aç ────────────────────────────────────────────────────
    public void UnlockRobot()
    {
        string id = $"ROB-0{Robots.Count + 1}";
        Robots.Add(new Robot(id));
        Debug.Log($"[GameState] Robot açıldı: {id}");
    }

    public void UnlockDrone()
    {
        string id = $"DRN-0{Drones.Count + 1}";
        Drones.Add(new Drone(id));
        Debug.Log($"[GameState] Dron açıldı: {id}");
    }

    public void AddRobot()
    {
        string id = $"ROB-0{Robots.Count + 1}";
        Robots.Add(new Robot(id));
    }

    public void AddDrone()
    {
        string id = $"DRN-0{Drones.Count + 1}";
        var d = new Drone(id);
        d.SpeedKmh    *= DroneSpeedMult;
        d.MaxCharge   = Mathf.RoundToInt(d.MaxCharge * BatteryMult);
        Drones.Add(d);
    }

    // ── Hava ──────────────────────────────────────────────────────────────────
    public void SetWeather(string w)
    {
        if (w == "mixed")
            w = Random.value > 0.5f ? "windy" : "rain";
        Weather = w;
        Debug.Log($"[GameState] Hava: {Weather}");
    }

    // ── Pul ───────────────────────────────────────────────────────────────────
    public void AddMoney(float amount)
    {
        Money += amount;
        EventBus.MoneyChanged(Money);
    }

    public bool SpendMoney(float amount)
    {
        if (Money < amount) return false;
        Money -= amount;
        EventBus.MoneyChanged(Money);
        return true;
    }

    // ── Reputasiya ────────────────────────────────────────────────────────────
    public void ChangeReputation(int delta)
    {
        int old = Reputation;
        Reputation = Mathf.Clamp(Reputation + delta, 0, 100);
        if (Reputation != old)
            EventBus.ReputationChanged(Reputation, Reputation - old);
    }

    // ── Enerji ────────────────────────────────────────────────────────────────
    public void SpendEnergy(int amount)
    {
        int old = Energy;
        Energy = Mathf.Clamp(Energy - amount, 0, 100);
        if (Energy != old)
            EventBus.EnergyChanged(Energy, Energy - old);
    }

    // ── Gün ───────────────────────────────────────────────────────────────────
    public void AdvanceDay()
    {
        Day++;
        Energy = Mathf.Clamp(Energy + 30, 0, 100);
        EventBus.DayChanged(Day);
    }

    public void SetCampaignDay(int index) => CampaignIndex = index;

    // ── Yüksəltmə ─────────────────────────────────────────────────────────────
    public void ApplyUpgrade(ShopItem item)
    {
        if (item.isOneTime && PurchasedUpgrades.Contains(item.id)) return;
        if (item.isOneTime) PurchasedUpgrades.Add(item.id);

        switch (item.effectType)
        {
            case "add_robot":   AddRobot();  break;
            case "add_drone":   AddDrone();  break;
            case "drone_speed":
                DroneSpeedMult *= item.effectValue;
                foreach (var d in Drones) d.SpeedKmh *= item.effectValue;
                break;
            case "battery":
                BatteryMult *= item.effectValue;
                foreach (var d in Drones) d.MaxCharge = Mathf.RoundToInt(d.MaxCharge * item.effectValue);
                break;
            case "route_upgrade":
                RouteUpgraded = true;
                break;
        }
    }

    // ── Yeni oyun / reset ─────────────────────────────────────────────────────
    public void ResetForNewGame()
    {
        Money      = 500f;
        Day        = 1;
        Reputation = 50;
        Energy     = 100;
        Weather    = "clear";
        CampaignIndex = 0;
        DroneSpeedMult = 1f;
        BatteryMult    = 1f;
        RouteUpgraded  = false;
        PurchasedUpgrades.Clear();
        Robots.Clear();
        Drones.Clear();
        Couriers.Clear();
        InitStartingResources();
        EventBus.MoneyChanged(Money);
        EventBus.DayChanged(Day);
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public GameSaveData ToSaveData() => new()
    {
        campaignDayIndex  = CampaignIndex,
        money             = Money,
        reputation        = Reputation,
        energy            = Energy,
        robotCount        = Robots.Count,
        droneCount        = Drones.Count,
        purchasedUpgrades = new List<string>(PurchasedUpgrades),
        droneSpeedMult    = DroneSpeedMult,
        batteryMult       = BatteryMult,
        routeUpgraded     = RouteUpgraded
    };

    public void LoadFromSave(GameSaveData s)
    {
        Money         = s.money;
        Reputation    = s.reputation;
        Energy        = s.energy;
        Day           = s.campaignDayIndex + 1;
        CampaignIndex = s.campaignDayIndex;
        DroneSpeedMult = s.droneSpeedMult > 0 ? s.droneSpeedMult : 1f;
        BatteryMult    = s.batteryMult    > 0 ? s.batteryMult    : 1f;
        RouteUpgraded  = s.routeUpgraded;
        PurchasedUpgrades = new List<string>(s.purchasedUpgrades ?? new List<string>());

        Robots.Clear(); Drones.Clear(); Couriers.Clear();
        Couriers.Add(new Courier("CR-01"));
        for (int i = 0; i < s.robotCount; i++) AddRobot();
        for (int i = 0; i < s.droneCount; i++) AddDrone();

        EventBus.MoneyChanged(Money);
        EventBus.DayChanged(Day);
    }

    // ── Resurs axtarışı ───────────────────────────────────────────────────────
    public Robot   GetIdleRobot()   => Robots.Find(r => r.IsIdle && r.CurrentOrderId == -1);
    public Drone   GetIdleDrone()   => Drones.Find(d => d.IsIdle && d.CurrentOrderId == -1);
    public Courier GetIdleCourier() => Couriers.Find(c => c.IsIdle && c.CurrentOrderId == -1);
}
