using UnityEngine;

public static class Main
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterScene()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor  = new Color(0.04f, 0.05f, 0.04f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            Debug.Log("[Fix] Kamera Orthographic edildi.");
        }
        var root = UIManager.Instance?.CanvasRoot;
        Debug.Log(root != null ? $"[Fix] CanvasRoot OK: {root.name}" : "[FIX] CanvasRoot NULL — UIManager problemi!");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        // ── Core ──────────────────────────────────────────────────────────────
        CreateSingleton<GameState>("GameState");
        CreateSingleton<TimeSystem>("TimeSystem");

        // ── Systems ───────────────────────────────────────────────────────────
        CreateSingleton<EconomySystem>("EconomySystem");
        CreateSingleton<WarehouseSystem>("WarehouseSystem");
        CreateSingleton<DroneSystem>("DroneSystem");
        CreateSingleton<RouteAI>("RouteAI");
        CreateSingleton<OrderManager>("OrderManager");

        // ── Sprint 5: Kampaniya sistemləri ────────────────────────────────────
        CreateSingleton<SaveSystem>("SaveSystem");
        CreateSingleton<ShopSystem>("ShopSystem");
        CreateSingleton<CampaignManager>("CampaignManager");

        // ── UI ────────────────────────────────────────────────────────────────
        CreateSingleton<UIManager>("UIManager");
        CreateSingleton<DecisionPanel>("DecisionPanel");
        CreateSingleton<MapView>("MapView");
        CreateSingleton<MainMenuPanel>("MainMenuPanel");
        CreateSingleton<DayReportPanel>("DayReportPanel");
        CreateSingleton<ShopPanel>("ShopPanel");
        CreateSingleton<GameOverPanel>("GameOverPanel");

        SubscribeDebugListeners();

        Debug.Log("[SilkRoute] Sprint 5 bootstrap tamamlandı — Kampaniya + Mağaza + Save aktiv.");
    }

    static void CreateSingleton<T>(string goName) where T : MonoBehaviour
    {
        var go = new GameObject(goName);
        go.AddComponent<T>();
        Object.DontDestroyOnLoad(go);
    }

    static void SubscribeDebugListeners()
    {
        EventBus.OnOrderCreated        += o => Debug.Log($"[EVT] Created   ORD-{o.Id} {o.ProductName}");
        EventBus.OnOrderDelivered      += o => Debug.Log($"[EVT] Delivered ORD-{o.Id}");
        EventBus.OnOrderFailed         += o => Debug.Log($"[EVT] Failed    ORD-{o.Id}");
        EventBus.OnMoneyChanged        += m => Debug.Log($"[EVT] Pul → ¥{m:F0}");
        EventBus.OnDayChanged          += d => Debug.Log($"[EVT] Gün → {d}");
        EventBus.OnDayStarted          += (d, cfg) => Debug.Log($"[EVT] Gün {d} başladı: {cfg.title}");
        EventBus.OnDayEnded            += d => Debug.Log($"[EVT] Gün {d} bitdi");
        EventBus.OnShopItemPurchased   += id => Debug.Log($"[EVT] Alış: {id}");
        EventBus.OnGameWon             += () => Debug.Log("[EVT] *** QALİB ***");
        EventBus.OnGameLost            += r => Debug.Log($"[EVT] *** UDUZDU *** {r}");
        EventBus.OnRecommendationReady += r => Debug.Log($"[EVT] ROUTE ORD-{r.OrderId}: {r.Recommended?.MethodLabel}");
        EventBus.OnPlayerDecided       += (id, p) => Debug.Log($"[EVT] Qərar ORD-{id} → {p.MethodLabel}");
        EventBus.OnReputationChanged   += (v, d) => Debug.Log($"[EVT] Rep {v} ({(d >= 0 ? "+" : "")}{d})");
    }
}
