using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    private readonly List<Order>                          _activeOrders     = new();
    private readonly Dictionary<int, RecommendationPlan> _pendingDecisions = new();
    private int  _nextId          = 1;
    private int  _maxOrdersToday  = 0;
    private int  _spawnedToday    = 0;
    private bool _dayActive       = false;

    private static readonly string[] Customers =
        { "Chen Wei", "Li Fang", "Wang Bo", "Zhang Mei", "Liu Hong", "Xu Ming", "Zhao Lei" };

    private static readonly string[] Products =
        { "Elektronika paketi", "Tibbi ləvazimat", "Geyim sifariş", "Qida çatdırılma", "Sənədlər" };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EventBus.OnPlayerDecided    += (id, plan) => _pendingDecisions[id] = plan;
        EventBus.OnRobotFinished    += OnRobotFinished;
        EventBus.OnDroneDelivered   += OnDroneDelivered;
        EventBus.OnCourierDelivered += OnCourierDelivered;
    }

    // ── Gün idarəetməsi ───────────────────────────────────────────────────────
    public void StartDay(int orderCount)
    {
        _maxOrdersToday = orderCount;
        _spawnedToday   = 0;
        _dayActive      = true;
        _activeOrders.Clear();
        _pendingDecisions.Clear();
        StartCoroutine(SpawnLoop());
    }

    public void StopDay()
    {
        _dayActive = false;
        StopAllCoroutines();
    }

    // ── Spawn döngəsi ────────────────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(3f); // gün başlayanda qısa gecikmə

        while (_dayActive && _spawnedToday < _maxOrdersToday)
        {
            SpawnOrder();
            float interval = Mathf.Max(6f, 480f / _maxOrdersToday); // günə bölünmüş interval
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnOrder()
    {
        string customer = Customers[Random.Range(0, Customers.Length)];
        string product  = Products[Random.Range(0, Products.Length)];
        float  weight   = Random.Range(0.5f, 6f);
        float  distance = Random.Range(1f, 10f);
        float  revenue  = Mathf.Round(distance * Random.Range(5f, 12f));
        float  deadline = Mathf.Max(distance * 4f, Random.Range(30f, 100f));

        int roll = Random.Range(0, 10);
        var priority = roll >= 9 ? OrderPriority.Vip
                     : roll >= 7 ? OrderPriority.High
                     : roll >= 4 ? OrderPriority.Normal
                                 : OrderPriority.Low;

        var order = new Order(_nextId++, customer, product, weight, distance,
                              revenue, deadline, priority, TimeSystem.Instance.GameTimeSeconds);

        _activeOrders.Add(order);
        _spawnedToday++;
        EventBus.OrderCreated(order);

        Debug.Log($"[OrderMgr] ORD-{order.Id} — {customer} | {product} | " +
                  $"{weight:F1}kg {distance:F1}km deadline:{deadline:F0}s [{priority}] ({_spawnedToday}/{_maxOrdersToday})");

        StartCoroutine(StateMachine(order));
    }

    // ── Vəziyyət maşını ──────────────────────────────────────────────────────
    private IEnumerator StateMachine(Order order)
    {
        order.Status = OrderStatus.Analyzing;
        yield return new WaitForSeconds(0.5f);

        order.Status = OrderStatus.AwaitingDecision;
        var rec = RouteAI.Instance.GenerateRecommendation(order);

        if (rec.Recommended == null)
        {
            FailOrder(order, "Mövcud resurs yoxdur");
            yield break;
        }

        EventBus.RecommendationReady(rec);

        float waited = 0f;
        while (!_pendingDecisions.ContainsKey(order.Id))
        {
            waited += Time.deltaTime;
            if (waited >= order.DeadlineSeconds * 0.8f)
            {
                _pendingDecisions[order.Id] = rec.Recommended;
                break;
            }
            yield return null;
        }

        var chosen = _pendingDecisions[order.Id];
        _pendingDecisions.Remove(order.Id);

        bool assigned = chosen.Method switch
        {
            DeliveryMethod.RobotDrone  => WarehouseSystem.Instance.TryAssignRobotDrone(order),
            DeliveryMethod.CourierOnly => WarehouseSystem.Instance.TryAssignCourier(order),
            DeliveryMethod.TwoDrones   => WarehouseSystem.Instance.TryAssignTwoDrones(order),
            _                          => false
        };

        if (!assigned)
            FailOrder(order, "Resurs artıq məşğuldur");
    }

    private void FailOrder(Order order, string reason)
    {
        order.Status = OrderStatus.Failed;
        GameState.Instance.ChangeReputation(-3);
        EventBus.OrderFailed(order);
        _activeOrders.Remove(order);
        Debug.Log($"[OrderMgr] ORD-{order.Id} FAILED — {reason}");
    }

    // ── EventBus dinləyiciləri ───────────────────────────────────────────────
    private void OnRobotFinished(Robot robot, Order order)
    {
        if (order.Status == OrderStatus.Ready)
            DroneSystem.Instance.LaunchDelivery(order);
    }

    private void OnDroneDelivered(Drone drone, Order order, bool success)
    {
        order.Status = success ? OrderStatus.Delivered : OrderStatus.Failed;
        if (success) EventBus.OrderDelivered(order);
        else         EventBus.OrderFailed(order);
        _activeOrders.Remove(order);
    }

    private void OnCourierDelivered(Courier courier, Order order, bool success)
    {
        order.Status = success ? OrderStatus.Delivered : OrderStatus.Failed;
        if (success) EventBus.OrderDelivered(order);
        else         EventBus.OrderFailed(order);
        _activeOrders.Remove(order);
    }

    public IReadOnlyList<Order> ActiveOrders => _activeOrders;
}
