using System.Collections;
using UnityEngine;

public class DroneSystem : MonoBehaviour
{
    public static DroneSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LaunchDelivery(Order order)
    {
        Drone drone = GameState.Instance.Drones.Find(d => d.Id == order.AssignedDroneId);
        if (drone == null)
        {
            Debug.LogWarning($"[DroneSystem] Drone '{order.AssignedDroneId}' tapılmadı — ORD-{order.Id}");
            return;
        }
        StartCoroutine(FlightCoroutine(order, drone));
    }

    private IEnumerator FlightCoroutine(Order order, Drone drone)
    {
        float weatherFactor = WeatherFactor(GameState.Instance.Weather);
        float flightTimeSec = (order.DistanceKm / (drone.SpeedKmh * weatherFactor)) * 60f;
        float batteryCost   = drone.RoundTripBatteryCost(order.DistanceKm);
        float batteryFactor = drone.Battery >= batteryCost * 2f ? 1.0f : 0.7f;

        // Uğur ehtimalı
        float successProb = 0.9f
            * weatherFactor
            * batteryFactor
            * (order.WeightKg <= drone.MaxPayloadKg ? 1f : 0f);

        // Yüklənmə → uçuş
        drone.Status = DroneStatus.Loading;
        order.Status = OrderStatus.InDelivery;
        yield return new WaitForSeconds(1f);

        drone.Status = DroneStatus.InFlight;
        EventBus.DroneLaunched(drone, order);
        Debug.Log($"[Drone] {drone.Id} launched — ORD-{order.Id} | dist:{order.DistanceKm:F1}km | " +
                  $"eta:{flightTimeSec:F1}s | success:{successProb:P0}");

        yield return new WaitForSeconds(flightTimeSec);

        bool success = Random.value <= successProb
                       && flightTimeSec <= order.DeadlineSeconds;

        // Batareya azalt
        drone.Battery = Mathf.Clamp(drone.Battery - batteryCost, 0f, 100f);

        EventBus.DroneDelivered(drone, order, success);
        Debug.Log($"[Drone] {drone.Id} → ORD-{order.Id} {(success ? "DELIVERED ✓" : "FAILED ✗")} | battery:{drone.Battery:F0}%");

        // Geri dönüş + şarj
        drone.Status = DroneStatus.Returning;
        yield return new WaitForSeconds(flightTimeSec * 0.8f);

        drone.Status = DroneStatus.Charging;
        yield return StartCoroutine(ChargeCoroutine(drone));

        drone.CurrentOrderId = -1;
    }

    // ── 2× Dron çatdırılması (redundans) ──────────────────────────────────────
    public void LaunchTwoDroneDelivery(Order order)
    {
        Drone a = GameState.Instance.Drones.Find(d => d.Id == order.AssignedDroneId);
        Drone b = GameState.Instance.Drones.Find(d => d.Id == order.AssignedDroneId2);
        if (a == null || b == null)
        {
            Debug.LogWarning($"[DroneSystem] 2× dron tapılmadı — ORD-{order.Id}");
            return;
        }
        StartCoroutine(TwoDroneFlight(order, a, b));
    }

    private IEnumerator TwoDroneFlight(Order order, Drone a, Drone b)
    {
        float weatherFactor = WeatherFactor(GameState.Instance.Weather);
        float flightTimeSec = (order.DistanceKm / (a.SpeedKmh * weatherFactor)) * 60f;

        float payloadOk    = order.WeightKg <= a.MaxPayloadKg ? 1f : 0f;
        float singleProb   = 0.9f * weatherFactor * payloadOk;
        // Redundans: yalnız hər iki dron uğursuz olarsa çatdırılma alınmır
        float successProb  = (1f - (1f - singleProb) * (1f - singleProb)) * payloadOk;

        order.Status = OrderStatus.InDelivery;
        a.Status = DroneStatus.Loading;
        b.Status = DroneStatus.Loading;
        yield return new WaitForSeconds(1f);

        a.Status = DroneStatus.InFlight;
        b.Status = DroneStatus.InFlight;
        EventBus.DroneLaunched(a, order);
        Debug.Log($"[Drone] 2× ({a.Id}+{b.Id}) launched — ORD-{order.Id} | " +
                  $"eta:{flightTimeSec:F1}s | success:{successProb:P0}");

        yield return new WaitForSeconds(flightTimeSec);

        bool success = Random.value <= successProb
                       && flightTimeSec <= order.DeadlineSeconds;

        a.Battery = Mathf.Clamp(a.Battery - a.RoundTripBatteryCost(order.DistanceKm), 0f, 100f);
        b.Battery = Mathf.Clamp(b.Battery - b.RoundTripBatteryCost(order.DistanceKm), 0f, 100f);

        // İkinci dronun əməliyyat xərci (birincisi EconomySystem tərəfindən tutulur)
        GameState.Instance.SpendMoney(b.CreditPerTask);

        EventBus.DroneDelivered(a, order, success);
        Debug.Log($"[Drone] 2× → ORD-{order.Id} {(success ? "DELIVERED ✓" : "FAILED ✗")}");

        a.Status = DroneStatus.Returning;
        b.Status = DroneStatus.Returning;
        yield return new WaitForSeconds(flightTimeSec * 0.8f);

        a.Status = DroneStatus.Charging;
        b.Status = DroneStatus.Charging;
        StartCoroutine(ChargeCoroutine(b));
        yield return StartCoroutine(ChargeCoroutine(a));

        a.CurrentOrderId = -1;
        b.CurrentOrderId = -1;
    }

    private IEnumerator ChargeCoroutine(Drone drone)
    {
        while (drone.Battery < 100f)
        {
            drone.Battery = Mathf.Min(drone.Battery + Drone.ChargeRatePerSec * Time.deltaTime, 100f);
            yield return null;
        }
        drone.Status = DroneStatus.Idle;
        Debug.Log($"[Drone] {drone.Id} fully charged — idle");
    }

    private static float WeatherFactor(string weather) => weather switch
    {
        "windy" => 0.85f,
        "rain"  => 0.70f,
        _       => 1.00f
    };
}
