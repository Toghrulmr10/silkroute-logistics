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
