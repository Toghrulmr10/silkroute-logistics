using System.Collections;
using UnityEngine;

public class WarehouseSystem : MonoBehaviour
{
    public static WarehouseSystem Instance { get; private set; }

    const float BasePickPackMin = 5f; // oyun-dəqiqəsi (= real saniyə)

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Robot+Drone metodu üçün hazırlama
    public bool TryAssignRobotDrone(Order order)
    {
        Robot robot = GameState.Instance.GetIdleRobot();
        Drone drone = GameState.Instance.GetIdleDrone();

        if (robot == null || drone == null) return false;
        if (drone.MaxPayloadKg < order.WeightKg) return false;

        order.AssignedRobotId = robot.Id;
        order.AssignedDroneId = drone.Id;
        order.Type = DeliveryType.Drone;
        robot.Status = RobotStatus.Picking;
        robot.CurrentOrderId = order.Id;
        drone.CurrentOrderId = order.Id;

        StartCoroutine(PrepareOrder(order, robot));
        return true;
    }

    // 2× Dron metodu üçün iki dronun təyinatı (robot tələb olunmur — dronlar özü yüklənir)
    public bool TryAssignTwoDrones(Order order)
    {
        var idles = GameState.Instance.Drones.FindAll(d => d.IsIdle && d.CurrentOrderId == -1);
        if (idles.Count < 2)                     return false;
        if (idles[0].MaxPayloadKg < order.WeightKg) return false;

        Drone primary   = idles[0];
        Drone secondary = idles[1];

        order.AssignedDroneId  = primary.Id;
        order.AssignedDroneId2 = secondary.Id;
        order.Type = DeliveryType.Drone;

        // Dərhal məşğul işarələ ki, başqa sifariş eyni dronları seçməsin
        primary.Status   = DroneStatus.Loading;
        secondary.Status = DroneStatus.Loading;
        primary.CurrentOrderId   = order.Id;
        secondary.CurrentOrderId = order.Id;

        DroneSystem.Instance.LaunchTwoDroneDelivery(order);
        return true;
    }

    // Kuryer metodu üçün birbaşa çatdırılma
    public bool TryAssignCourier(Order order)
    {
        Courier courier = GameState.Instance.GetIdleCourier();
        if (courier == null) return false;

        order.AssignedCourierId = courier.Id;
        order.Type = DeliveryType.Courier;
        courier.Status = CourierStatus.Delivering;
        courier.CurrentOrderId = order.Id;

        StartCoroutine(CourierDelivery(order, courier));
        return true;
    }

    public void StopDay()
    {
        StopAllCoroutines();
        foreach (var r in GameState.Instance.Robots)
        {
            r.Status         = RobotStatus.Idle;
            r.CurrentOrderId = -1;
        }
        foreach (var c in GameState.Instance.Couriers)
        {
            c.Status         = CourierStatus.Idle;
            c.CurrentOrderId = -1;
        }
        Debug.Log("[Warehouse] Gün dayandırıldı — robotlar və kuryerlər sıfırlandı");
    }

    // ── Robot hazırlama koroutini ─────────────────────────────────────────────
    private IEnumerator PrepareOrder(Order order, Robot robot)
    {
        order.Status = OrderStatus.Preparing;
        EventBus.RobotStarted(robot, order);
        Debug.Log($"[Warehouse] ROB {robot.Id} picking ORD-{order.Id} ({order.ProductName})");

        // Pick mərhələsi
        robot.Status = RobotStatus.Picking;
        float prepTime = BasePickPackMin / robot.Speed;
        yield return new WaitForSeconds(prepTime * 0.6f);

        // Pack mərhələsi
        robot.Status = RobotStatus.Packing;
        yield return new WaitForSeconds(prepTime * 0.4f);

        // Robot serbəst olur
        robot.Status = RobotStatus.Idle;
        robot.CurrentOrderId = -1;
        order.Status = OrderStatus.Ready;

        EventBus.RobotFinished(robot, order);
        Debug.Log($"[Warehouse] ROB {robot.Id} finished — ORD-{order.Id} READY");
    }

    // ── Kuryer çatdırılma koroutini ───────────────────────────────────────────
    private IEnumerator CourierDelivery(Order order, Courier courier)
    {
        order.Status = OrderStatus.InDelivery;

        // delivery_time = distance / speed * 60 (dəqiqə → saniyə kimi)
        float deliveryTime = (order.DistanceKm / courier.SpeedKmh) * 60f;
        Debug.Log($"[Warehouse] CR {courier.Id} delivering ORD-{order.Id} — {deliveryTime:F1}s");

        yield return new WaitForSeconds(deliveryTime);

        bool success = Random.value <= courier.SuccessRate
                       && deliveryTime <= order.DeadlineSeconds;

        courier.Status = CourierStatus.Idle;
        courier.CurrentOrderId = -1;

        EventBus.CourierDelivered(courier, order, success);
        Debug.Log($"[Warehouse] CR {courier.Id} → ORD-{order.Id} {(success ? "DELIVERED" : "FAILED")}");
    }
}
