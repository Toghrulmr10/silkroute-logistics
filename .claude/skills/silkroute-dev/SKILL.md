---
name: silkroute-dev
description: >-
  Implements features and fixes for the SilkRoute Logistics game — a 2D Unity 6
  (URP) single-player management game built with a code-driven MonoBehaviour +
  static Singleton + EventBus architecture. Use when adding or editing systems,
  UI panels, data models, economy/reputation logic, campaign days, the shop, or
  save/load; when wiring new game events; or when touching anything under
  Assets/Scripts. Knows the project's exact conventions so generated code drops
  in without rework.
allowed-tools: Read, Grep, Glob, Edit, Write, Bash
---

# SilkRoute Logistics — Development Skill

SilkRoute Logistics is a **2D, single-player logistics-management game** in
**Unity 6000.3.15f1** using the **URP 2D** renderer and the **new Input System**.
The player runs a delivery company across a multi-day campaign, dispatching
**couriers, drones, and robots** to fulfill **orders**, managing **money,
reputation, and energy**, and buying upgrades in the **shop**.

This skill encodes the project's real architecture and conventions. Follow them
exactly — do not introduce patterns from generic Unity tutorials (no DOTS/ECS,
no Netcode, no TextMeshPro, no prefab-authored scenes).

## Golden rules (read before writing code)

1. **No scene authoring.** Everything is created in code at runtime. There is one
   near-empty scene (`Assets/Scenes/SampleScene.unity`). Do **not** instruct the
   user to drag things in the Editor or build prefabs. New objects are spawned
   with `new GameObject()` / `AddComponent<T>()`.
2. **Singletons, not DI.** Every system/UI/manager is a `MonoBehaviour` singleton
   registered in `Assets/Scripts/Main.cs`. See the Singleton template below.
3. **EventBus is the only cross-system channel.** Systems never call each other's
   internals directly for state changes — they emit/subscribe via `EventBus`.
   Read it before adding events: `Assets/Scripts/Core/EventBus.cs`.
4. **GameState owns all persistent player state.** Money/Reputation/Energy/Day/
   resources live in `GameState`. Mutate them only through `GameState` methods
   (`AddMoney`, `SpendMoney`, `ChangeReputation`, `SpendEnergy`, ...), which emit
   the right events. Never mutate `GameState` properties directly (their setters
   are private by design).
5. **Legacy uGUI, not TMP.** UI uses `UnityEngine.UI` (`Text`, `Image`, `Button`).
   Fonts come from `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`.
   Do not add TextMeshPro.
6. **Language convention:** code comments and **player-facing UI strings are in
   Azerbaijani**; type/member names are in English. Currency is shown as `¥`.
7. **No tests exist yet** but the Test Framework is installed. If asked to add
   tests, use `com.unity.test-framework` (NUnit), not a custom harness.

## Project map

```
Assets/Scripts/
  Main.cs                 # Bootstrap: RuntimeInitializeOnLoadMethod creates all singletons
  Core/
    GameState.cs          # All persistent player state + save/load DTO
    EventBus.cs           # Static events + emit methods (the ONLY cross-system bus)
    TimeSystem.cs         # GameTimeSeconds clock
  Systems/                # EconomySystem, OrderManager, DroneSystem, RouteAI,
                          # WarehouseSystem, ShopSystem, CampaignManager,
                          # SaveSystem, UIManager, MapView
  UI/                     # *Panel.cs — each builds its own uGUI tree in code
  Data/                   # Plain C# classes: Order, Drone, Robot, Courier,
                          # ShopItem, DayConfig, GameSaveData, Recommendation...
Assets/Resources/Data/    # Runtime-loaded data (campaign/day configs)
```

When unsure how something works, **Read the real file** — the codebase is small
(~29 scripts). Prefer matching an existing sibling over inventing a new shape.

## Adding a new system (Singleton template)

Match this exact shape (taken from `EconomySystem` / `GameState`):

```csharp
using UnityEngine;

public class FooSystem : MonoBehaviour
{
    public static FooSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Subscribe to EventBus here, never in Awake (other singletons may not exist yet)
        EventBus.OnDayStarted += OnDayStarted;
    }

    void OnDestroy()
    {
        EventBus.OnDayStarted -= OnDayStarted; // always unsubscribe
    }

    private void OnDayStarted(int day, DayConfig cfg)
    {
        // ...
        Debug.Log($"[Foo] Gün {day} başladı"); // tag logs with [Foo]
    }
}
```

Then **register it in `Main.cs`** in the matching section, e.g.:
```csharp
CreateSingleton<FooSystem>("FooSystem");
```
Order matters only in that Core comes first, then Systems, then UI. Cross-system
references should be resolved in `Start()` (not `Awake()`), or lazily via
`FooSystem.Instance`.

## Adding a new game event

1. Add the `event Action<...>` field in `EventBus.cs` under the right sprint
   section.
2. Add the matching `=> OnXxx?.Invoke(...)` emit method.
3. Emit from the system that owns the state change; subscribe from consumers in
   their `Start()`.
4. Consider adding a debug listener in `Main.SubscribeDebugListeners()` using the
   existing `[EVT] ...` log style.

## Building a UI panel

UI panels are singletons that build their own tree in `Build()` (called from
`Awake`) and toggle with `Show()`/`Hide()`. Follow `ShopPanel` / `DayReportPanel`:

- Parent to `UIManager.Instance.CanvasRoot`.
- Use `new GameObject(...)`, `AddComponent<Image/Text/Button>()`, and set
  `RectTransform` anchors/offsets manually (see ShopPanel for the idiom).
- Get fonts via the local `GetFont()` helper (`LegacyRuntime.ttf`, Arial fallback).
- For modal panels that must sit above the map, use
  `UIManager.PromoteToOverlay(root, sortingOrder)` and
  `transform.SetAsLastSibling()` — modals render on their own Canvas/sortingOrder
  (see recent fixes), so don't rely on sibling order alone for layering.
- Update labels reactively by subscribing to EventBus in `Start()`
  (e.g. `OnMoneyChanged` → refresh `¥` text).
- Player-facing strings in Azerbaijani; format money as `¥{value:N0}`.

## Economy / reputation / energy conventions

- Rewards run through `EconomySystem.CalculateReward`: priority multiplier
  (`High` 1.5×, `Vip` 2.0×) × time bonus (early 1.2×, on-time 1.0×, late 0.5×).
- Reputation is clamped 0–100; success gives small `+`, failure `-5` (see
  EconomySystem for the exact deltas). Energy is clamped 0–100.
- Starting state: Money `500`, Reputation `50`, Energy `100`, Weather `"clear"`.
- Resource IDs follow `ROB-0n`, `DRN-0n`, `CR-0n`.
- Any new cost/reward path must go through `GameState.SpendMoney/AddMoney` so
  `OnMoneyChanged` fires and UI stays in sync.

## Save / load

`GameState.ToSaveData()` / `LoadFromSave()` map to the `GameSaveData` DTO
(`Assets/Scripts/Data/GameSaveData.cs`), persisted by `SaveSystem`. **When you
add a new piece of persistent state to `GameState`, you must also:** add a field
to `GameSaveData`, write it in `ToSaveData()`, and restore it in `LoadFromSave()`
(with a sane default for old saves, as done for `droneSpeedMult`/`batteryMult`).
Forgetting this is the most common bug — always check all three.

## Workflow for any change

1. **Clarify** the goal and which system/panel/data it touches.
2. **Read** the relevant existing file(s) and the nearest sibling for the pattern.
3. **Edit/Write** matching the conventions above.
4. **Wire it up:** register in `Main.cs` (new singleton), add to `EventBus`
   (new event), and update save/load (new persistent state) — check each.
5. **Verify:** this is a remote container without the Unity Editor, so you cannot
   enter Play Mode here. Sanity-check by reading the diff for the three wiring
   points above and confirming no direct `GameState` property writes. If the user
   is on their local machine with the Unity MCP server
   (`com.coplaydev.unity-mcp`), suggest they refresh/enter Play Mode to confirm;
   otherwise describe what to look for in the Console (the `[EVT]`/`[System]`
   logs).

## Do NOT use this skill when

- The task is a non-SilkRoute or non-Unity codebase.
- The user explicitly wants a DOTS/ECS, multiplayer, or 3D/HDRP rewrite — that
  contradicts this project's architecture; flag the mismatch first.

For a deeper architecture reference, see `resources/architecture-map.md`.
