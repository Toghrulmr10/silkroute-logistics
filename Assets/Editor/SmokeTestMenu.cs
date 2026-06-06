using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SmokeTestMenu
{
    [MenuItem("SmokeTest/Start New Game")]
    static void StartNewGame()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Play mode!"); return; }
        if (CampaignManager.Instance == null) { Debug.LogError("[SmokeTest] CampaignManager.Instance null — bootstrap tamamlanmayib!"); return; }
        ForceHideMenu();
        CampaignManager.Instance.StartNewGame();
        Debug.Log("[SmokeTest] StartNewGame cagrildi");
    }

    [MenuItem("SmokeTest/Force Hide Menu")]
    static void ForceHideMenuCmd()
    {
        if (!Application.isPlaying) return;
        ForceHideMenu();
    }

    static void ForceHideMenu()
    {
        // 1. Via Instance reference
        if (MainMenuPanel.Instance != null)
            MainMenuPanel.Instance.Hide();

        // 2. Backup: find by name and disable every Canvas with sortOrder>=50 named MainMenuPanel
        var all = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.gameObject.name == "MainMenuPanel")
            {
                c.gameObject.SetActive(false);
                Debug.Log($"[SmokeTest] Canvas '{c.gameObject.name}' force-gizlendi (sortOrder={c.sortingOrder})");
            }
        }

        Debug.Log("[SmokeTest] ForceHideMenu tamamlandi");
    }

    [MenuItem("SmokeTest/Print State")]
    static void PrintState()
    {
        if (!Application.isPlaying) return;
        var gs = GameState.Instance;
        var mm = MainMenuPanel.Instance;
        bool menuVisible = false;
        if (mm != null)
        {
            // Use reflection to check _root active state
            var field = typeof(MainMenuPanel).GetField("_root",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var root = field.GetValue(mm) as GameObject;
                menuVisible = root != null && root.activeSelf;
            }
        }
        float gameTime = TimeSystem.Instance != null ? TimeSystem.Instance.GameTimeSeconds : -1f;
        int activeOrders = OrderManager.Instance?.ActiveOrders?.Count ?? -1;
        bool tsRunning = false;
        if (TimeSystem.Instance != null)
        {
            var f = typeof(TimeSystem).GetField("_running",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) tsRunning = (bool)f.GetValue(TimeSystem.Instance);
        }
        Debug.Log($"[SmokeTest STATE] Day={gs?.Day} Money=¥{gs?.Money:N0} Rep={gs?.Reputation} Energy={gs?.Energy} " +
                  $"GameActive={CampaignManager.Instance?.GameActive} MenuVisible={menuVisible} TimeScale={Time.timeScale} " +
                  $"GameTimeSec={gameTime:F1} ActiveOrders={activeOrders} " +
                  $"TS_running={tsRunning} deltaTime={Time.deltaTime:F4} runInBg={Application.runInBackground} frame={Time.frameCount}");
    }

    [MenuItem("SmokeTest/Set TimeScale 4")]
    static void SetTimeScale4() { Time.timeScale = 4f; Debug.Log("[SmokeTest] timeScale = 4"); }

    [MenuItem("SmokeTest/Set TimeScale 1")]
    static void SetTimeScale1() { Time.timeScale = 1f; Debug.Log("[SmokeTest] timeScale = 1"); }

    // Force the Unity GameView to receive focus so the game loop begins ticking
    [MenuItem("SmokeTest/Focus Game View")]
    static void FocusGameView()
    {
        var t = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (t != null)
        {
            var w = EditorWindow.GetWindow(t);
            w.Focus();
            Debug.Log("[SmokeTest] GameView focused");
        }
        else Debug.LogWarning("[SmokeTest] GameView type not found");
    }

    // Directly advance GameTimeSeconds (bypasses coroutine scheduler — for testing only)
    [MenuItem("SmokeTest/Advance Time 60s")]
    static void AdvanceTime60()  => AdvanceGameTime(60f);

    [MenuItem("SmokeTest/Advance Time 120s")]
    static void AdvanceTime120() => AdvanceGameTime(120f);

    [MenuItem("SmokeTest/Advance Time 480s")]
    static void AdvanceTime480() => AdvanceGameTime(480f);

    static void AdvanceGameTime(float seconds)
    {
        if (!Application.isPlaying) return;
        var ts = TimeSystem.Instance;
        if (ts == null) { Debug.LogError("[SmokeTest] TimeSystem null"); return; }
        var f = typeof(TimeSystem).GetField("GameTimeSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);
        // Use backing field for auto-property
        var bf = typeof(TimeSystem).GetField("<GameTimeSeconds>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bf != null)
        {
            float cur = (float)bf.GetValue(ts);
            bf.SetValue(ts, cur + seconds);
            Debug.Log($"[SmokeTest] Time advanced by {seconds}s → {cur + seconds:F1}");
        }
        else Debug.LogWarning("[SmokeTest] Could not find GameTimeSeconds backing field");
    }

    // ── Simulate a full day: spawn orders, fire deliveries, end day ──────────
    [MenuItem("SmokeTest/Simulate Full Day")]
    static void SimulateFullDay()
    {
        if (!Application.isPlaying) return;
        var gs = GameState.Instance;
        var ts = TimeSystem.Instance;
        var ri = RouteAI.Instance;
        var om = OrderManager.Instance;

        if (gs == null || ts == null || ri == null || om == null)
        { Debug.LogError("[SmokeTest] Null singleton — start game first"); return; }

        // Read _maxOrdersToday and _nextId via reflection
        var fMax = typeof(OrderManager).GetField("_maxOrdersToday",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fId  = typeof(OrderManager).GetField("_nextId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int maxOrders = fMax != null ? (int)fMax.GetValue(om) : 3;
        int nextId    = fId  != null ? (int)fId.GetValue(om)  : 1;

        string[] customers = { "Chen Wei", "Li Fang", "Wang Bo", "Zhang Mei", "Liu Hong", "Xu Ming", "Zhao Lei" };
        string[] products  = { "Elektronika", "Tibbi ləvazimat", "Geyim", "Qida", "Sənədlər" };

        int delivered = 0, failed = 0;

        for (int i = 0; i < maxOrders; i++)
        {
            float weight   = UnityEngine.Random.Range(0.5f, 4f);
            float distance = UnityEngine.Random.Range(1f, 8f);
            float revenue  = Mathf.Round(distance * UnityEngine.Random.Range(5f, 12f));
            float deadline = Mathf.Max(distance * 4f, UnityEngine.Random.Range(60f, 150f));

            var order = new Order(nextId++,
                customers[i % customers.Length], products[i % products.Length],
                weight, distance, revenue, deadline, OrderPriority.Normal,
                ts.GameTimeSeconds);

            var rec = ri.GenerateRecommendation(order);
            if (rec.Recommended == null)
            {
                gs.ChangeReputation(-3);
                Debug.Log($"[SmokeTest DAY] ORD-{order.Id}: no resources → FAILED");
                failed++;
                continue;
            }

            bool success = SimulateDelivery(order, rec.Recommended.Method, gs);
            if (success) delivered++;
            else         failed++;

            Debug.Log($"[SmokeTest DAY] ORD-{order.Id} [{rec.Recommended.Method}] → {(success ? "DELIVERED ✓" : "FAILED ✗")} | ¥{gs.Money:N0} Rep={gs.Reputation}");
        }

        if (fId != null) fId.SetValue(om, nextId);

        // End the day directly — triggers DayReportPanel
        EventBus.DayEnded(gs.Day);
        Debug.Log($"[SmokeTest DAY] Day {gs.Day} END — delivered={delivered} failed={failed} | ¥{gs.Money:N0} Rep={gs.Reputation}");
    }

    // Synchronously simulates one delivery and returns success
    static bool SimulateDelivery(Order order, DeliveryMethod method, GameState gs)
    {
        if (method == DeliveryMethod.CourierOnly)
        {
            Courier cr = gs.GetIdleCourier();
            if (cr == null) { gs.ChangeReputation(-3); return false; }
            cr.Status = CourierStatus.Delivering;
            cr.CurrentOrderId = order.Id;
            order.AssignedCourierId = cr.Id;
            order.Type = DeliveryType.Courier;
            EventBus.CourierDelivered(cr, order, true);   // EconomySystem + OrderManager react
            cr.Status = CourierStatus.Idle;
            cr.CurrentOrderId = -1;
            return true;
        }
        else // RobotDrone or TwoDrones
        {
            Drone d = gs.GetIdleDrone();
            if (d == null)
            {
                // fall back to courier
                Courier cr = gs.GetIdleCourier();
                if (cr == null) { gs.ChangeReputation(-3); return false; }
                cr.Status = CourierStatus.Delivering;
                cr.CurrentOrderId = order.Id;
                order.AssignedCourierId = cr.Id;
                order.Type = DeliveryType.Courier;
                EventBus.CourierDelivered(cr, order, true);
                cr.Status = CourierStatus.Idle;
                cr.CurrentOrderId = -1;
                return true;
            }
            d.Status = DroneStatus.Delivering;
            d.CurrentOrderId = order.Id;
            order.AssignedDroneId = d.Id;
            order.Type = DeliveryType.Drone;
            // Reduce battery realistically
            d.Battery = Mathf.Clamp(d.Battery - d.RoundTripBatteryCost(order.DistanceKm), 0f, 100f);
            EventBus.DroneDelivered(d, order, true);      // EconomySystem + OrderManager react
            d.Status = DroneStatus.Idle;
            d.CurrentOrderId = -1;
            d.Battery = 100f; // reset for next order
            return true;
        }
    }

    [MenuItem("SmokeTest/Proceed To Shop")]
    static void ProceedToShop()
    {
        if (!Application.isPlaying) return;
        CampaignManager.Instance?.ProceedToShop();
        Debug.Log("[SmokeTest] ProceedToShop");
    }

    [MenuItem("SmokeTest/Proceed To Next Day")]
    static void ProceedToNextDay()
    {
        if (!Application.isPlaying) return;
        CampaignManager.Instance?.ProceedToNextDay();
        Debug.Log("[SmokeTest] ProceedToNextDay");
    }
}
