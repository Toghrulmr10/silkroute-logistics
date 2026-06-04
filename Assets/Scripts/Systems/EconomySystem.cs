using UnityEngine;

public class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EventBus.OnRobotFinished    += OnRobotFinished;
        EventBus.OnDroneDelivered   += OnDroneDelivered;
        EventBus.OnCourierDelivered += OnCourierDelivered;
    }

    // Robot hazırlama xərci
    private void OnRobotFinished(Robot robot, Order order)
    {
        GameState.Instance.SpendMoney(robot.CreditPerTask);
        GameState.Instance.SpendEnergy(robot.EnergyPerTask);
        Debug.Log($"[Economy] Robot xərci: -{robot.CreditPerTask}cr -{robot.EnergyPerTask}enerji");
    }

    // Dron çatdırılma nəticəsi
    private void OnDroneDelivered(Drone drone, Order order, bool success)
    {
        GameState.Instance.SpendMoney(drone.CreditPerTask);

        if (success)
        {
            float reward = CalculateReward(order);
            GameState.Instance.AddMoney(reward);
            GameState.Instance.ChangeReputation(+2);
            Debug.Log($"[Economy] Çatdırıldı → +¥{reward:F0} | rep +2");
        }
        else
        {
            GameState.Instance.ChangeReputation(-5);
            Debug.Log($"[Economy] Uğursuz dron → rep -5");
        }
    }

    // Kuryer çatdırılma nəticəsi
    private void OnCourierDelivered(Courier courier, Order order, bool success)
    {
        GameState.Instance.SpendMoney(courier.CreditCost);

        if (success)
        {
            float reward = CalculateReward(order);
            GameState.Instance.AddMoney(reward);
            GameState.Instance.ChangeReputation(+1);
            Debug.Log($"[Economy] Kuryer çatdırdı → +¥{reward:F0} | rep +1");
        }
        else
        {
            GameState.Instance.ChangeReputation(-5);
            Debug.Log($"[Economy] Kuryer uğursuz → rep -5");
        }
    }

    // Mükafat hesabı: prioritet çarpanı + vaxt bonusu
    private static float CalculateReward(Order order)
    {
        float multiplier = order.Priority switch
        {
            OrderPriority.High => 1.5f,
            OrderPriority.Vip  => 2.0f,
            _                  => 1.0f
        };

        float elapsed = TimeSystem.Instance.GameTimeSeconds - order.CreatedAt;
        float timeRatio = elapsed / order.DeadlineSeconds;

        float timeBonus = timeRatio < 0.8f ? 1.2f   // vaxtından əvvəl
                        : timeRatio <= 1f  ? 1.0f   // vaxtında
                                          : 0.5f;   // gecikdi

        return order.Revenue * multiplier * timeBonus;
    }
}
