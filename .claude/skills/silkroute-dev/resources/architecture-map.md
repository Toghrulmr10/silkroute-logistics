# SilkRoute Logistics — Architecture Reference

Deeper reference for the `silkroute-dev` skill. Read this when a change spans
multiple systems or you need the full data-flow picture.

## Runtime bootstrap

`Main.cs` runs before scene load via
`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. It creates every singleton
with `CreateSingleton<T>(name)` (`new GameObject` + `AddComponent<T>` +
`DontDestroyOnLoad`) in this order:

1. **Core:** `GameState`, `TimeSystem`
2. **Systems:** `EconomySystem`, `WarehouseSystem`, `DroneSystem`, `RouteAI`,
   `OrderManager`, then Sprint-5 `SaveSystem`, `ShopSystem`, `CampaignManager`
3. **UI:** `UIManager`, `DecisionPanel`, `MapView`, `MainMenuPanel`,
   `DayReportPanel`, `ShopPanel`, `GameOverPanel`

It also fixes the main camera (orthographic, size 5, dark background) and wires
debug `[EVT]` listeners. Because creation order is fixed, **resolve cross-system
references in `Start()`**, not `Awake()`.

## Core state: GameState

Single source of truth for persistent player state. All setters are private;
mutate via methods that emit EventBus events.

| State | Default | Notes |
|-------|---------|-------|
| `Money` | 500 | `AddMoney` / `SpendMoney` (returns false if insufficient) → `OnMoneyChanged` |
| `Day` | 1 | `AdvanceDay` (+30 energy on new day) → `OnDayChanged` |
| `Reputation` | 50 | `ChangeReputation(delta)`, clamped 0–100 → `OnReputationChanged` |
| `Energy` | 100 | `SpendEnergy(amount)`, clamped 0–100 → `OnEnergyChanged` |
| `Weather` | "clear" | `SetWeather` ("mixed" → random windy/rain) |
| `CampaignIndex` | 0 | day index into the campaign |

Resources: `List<Robot>`, `List<Drone>`, `List<Courier>` with IDs `ROB-0n`,
`DRN-0n`, `CR-0n`. `GetIdleRobot/Drone/Courier()` find a free unit
(`IsIdle && CurrentOrderId == -1`). Upgrades apply via `ApplyUpgrade(ShopItem)`
using `effectType` strings: `add_robot`, `add_drone`, `drone_speed`, `battery`,
`route_upgrade`.

## Event flow (typical order lifecycle)

```
OrderManager creates order        → EventBus.OrderCreated
RouteAI evaluates                 → EventBus.RecommendationReady
Player picks method (DecisionPanel)→ EventBus.PlayerDecided
DroneSystem / courier dispatch    → OnDroneLaunched / OnRobotStarted
Delivery resolves                 → OnDroneDelivered / OnCourierDelivered (bool ok)
EconomySystem reacts to result    → GameState.AddMoney + ChangeReputation
                                  → OnMoneyChanged / OnReputationChanged
UI panels refresh from those events
```

## EventBus catalog (see Core/EventBus.cs for signatures)

- Orders: `OnOrderCreated`, `OnOrderDelivered`, `OnOrderFailed`
- Economy: `OnMoneyChanged(float)`, `OnReputationChanged(int,int)`,
  `OnEnergyChanged(int,int)`
- Routing: `OnRecommendationReady(Recommendation)`,
  `OnPlayerDecided(int, RecommendationPlan)`
- Units: `OnRobotStarted/Finished`, `OnDroneLaunched`,
  `OnDroneDelivered(Drone,Order,bool)`, `OnCourierDelivered(Courier,Order,bool)`
- Campaign (Sprint 5): `OnDayStarted(int,DayConfig)`, `OnDayEnded(int)`,
  `OnDayChanged(int)`, `OnShopItemPurchased(string)`, `OnGameWon`,
  `OnGameLost(string)`

Add new events in pairs: the `event Action<...>` field + a static emit method
`Xxx(...) => OnXxx?.Invoke(...)`.

## Economy formula (EconomySystem.CalculateReward)

```
reward = order.Revenue
       × priorityMultiplier   // Normal 1.0, High 1.5, Vip 2.0
       × timeBonus            // <0.8 deadline → 1.2 ; ≤1.0 → 1.0 ; late → 0.5
```
Reputation: drone success +2, courier success +1, any failure −5. Each dispatch
also charges the unit's `CreditPerTask` / `CreditCost` (and robots spend energy).

## UI conventions

- Built in code with legacy uGUI (`Text`/`Image`/`Button`), manual
  `RectTransform` anchors. No TMP.
- Font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` (Arial
  fallback) — copy the `GetFont()` helper used in every panel.
- Modal panels promote to their own overlay Canvas:
  `UIManager.PromoteToOverlay(root, sortingOrder)` + `SetAsLastSibling()`.
  This was added to fix modals rendering under `MapPanel`.
- Money strings: `¥{value:N0}`. Player-facing text in Azerbaijani.

## Save/load checklist

Persistent state lives in `GameSaveData` (Data/) and round-trips through
`GameState.ToSaveData()` / `LoadFromSave()`, driven by `SaveSystem`. Adding state
means touching **all three** plus a default for legacy saves. Currently persisted:
campaign day index, money, reputation, energy, robot/drone counts, purchased
upgrades, `droneSpeedMult`, `batteryMult`, `routeUpgraded`.

## Tech constraints

- Unity `6000.3.15f1`, URP 2D, new Input System (`com.unity.inputsystem`).
- No asmdef files (single default assembly), no Editor scripts, no DOTS/ECS,
  no Netcode, single-player only.
- Unity control bridge installed locally: `com.coplaydev.unity-mcp` (MCP), not
  `unity-cli`.
