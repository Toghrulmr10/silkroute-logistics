using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public Transform CanvasRoot { get; private set; }

    GameObject _hudRoot;
    Text _moneyText;
    Text _dayText;
    Text _repText;
    Text _energyText;
    Transform _orderContent;
    readonly List<GameObject> _orderRows = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Start()
    {
        EventBus.OnMoneyChanged   += m => _moneyText.text = $"¥{m:N0}";
        EventBus.OnDayChanged     += d => _dayText.text   = $"Day {d}";
        EventBus.OnOrderCreated   += _ => RefreshOrders();
        EventBus.OnOrderDelivered += _ => RefreshOrders();
        EventBus.OnOrderFailed    += _ => RefreshOrders();

        // HUD yalnız oyun gedişində görünsün — menyu/oyun sonu zamanı gizlən
        EventBus.OnDayStarted     += (_, __) => SetHudVisible(true);
        EventBus.OnGameWon        += ()      => SetHudVisible(false);
        EventBus.OnGameLost       += _       => SetHudVisible(false);

        _moneyText.text = $"¥{GameState.Instance.Money:N0}";
        _dayText.text   = $"Day {GameState.Instance.Day}";
    }

    void SetHudVisible(bool visible)
    {
        if (_hudRoot != null) _hudRoot.SetActive(visible);
    }

    void RefreshOrders()
    {
        foreach (var r in _orderRows) Destroy(r);
        _orderRows.Clear();

        foreach (var o in OrderManager.Instance.ActiveOrders)
        {
            var row = new GameObject($"Order_{o.Id}");
            row.transform.SetParent(_orderContent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 26;
            var t = row.AddComponent<Text>();
            t.text      = $"#{o.Id}  {o.CustomerName}  |  {o.ProductName}  |  ¥{o.Revenue:N0}  |  {o.Status}";
            t.fontSize  = 13;
            t.alignment = TextAnchor.MiddleLeft;
            t.color     = o.Status switch
            {
                OrderStatus.Delivered   => new Color(0.4f, 1f,   0.4f),
                OrderStatus.Failed      => new Color(1f,   0.4f, 0.4f),
                OrderStatus.InDelivery  => new Color(0.95f, 0.85f, 0.3f),
                OrderStatus.Preparing   => new Color(0.7f,  0.85f, 1f),
                OrderStatus.Ready       => new Color(0.5f,  0.9f,  1f),
                _                       => new Color(0.75f, 0.75f, 0.75f)
            };
            t.font = GetBuiltinFont();
            _orderRows.Add(row);
        }
    }

    void BuildUI()
    {
        // UI inputu üçün EventSystem (səhnədə yoxdursa yarat) — düymələrin işləməsi üçün şərt
        EnsureEventSystem();

        // Canvas
        var cvsGO = new GameObject("GameCanvas");
        DontDestroyOnLoad(cvsGO);
        var canvas = cvsGO.AddComponent<Canvas>(); // Canvas əvvəl → RectTransform yaranır
        CanvasRoot = cvsGO.transform;              // İndi referans RectTransform-ə işarə edir
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = cvsGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 1f;
        cvsGO.AddComponent<GraphicRaycaster>();

        // HUD konteyneri — başlanğıcda gizli, yalnız oyun başlayanda görünür
        _hudRoot = new GameObject("HUDRoot");
        _hudRoot.transform.SetParent(cvsGO.transform, false);
        var hudRT = _hudRoot.AddComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero; hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = hudRT.offsetMax = Vector2.zero;
        var root = _hudRoot.transform;

        // ── Top HUD bar ─────────────────────────────────────────────────────────
        var topBar = MakePanel(root, "TopBar", new Color(0.08f, 0.08f, 0.12f, 0.92f));
        topBar.anchorMin        = new Vector2(0, 1);
        topBar.anchorMax        = new Vector2(1, 1);
        topBar.pivot            = new Vector2(0.5f, 1);
        topBar.anchoredPosition = Vector2.zero;
        topBar.sizeDelta        = new Vector2(0, 56);

        // Day label — left half
        _dayText = MakeText(topBar, "Day 1", 22, TextAnchor.MiddleLeft);
        var dt = _dayText.rectTransform;
        dt.anchorMin = new Vector2(0, 0); dt.anchorMax = new Vector2(0.5f, 1);
        dt.offsetMin = new Vector2(24, 0); dt.offsetMax = Vector2.zero;

        // Money label — right half
        _moneyText = MakeText(topBar, "¥5,000", 22, TextAnchor.MiddleRight);
        var mt = _moneyText.rectTransform;
        mt.anchorMin = new Vector2(0.5f, 0); mt.anchorMax = Vector2.one;
        mt.offsetMin = Vector2.zero; mt.offsetMax = new Vector2(-24, 0);

        // ── Orders panel (right side) ────────────────────────────────────────────
        var orders = MakePanel(root, "OrdersPanel", new Color(0.05f, 0.05f, 0.08f, 0.88f));
        orders.anchorMin        = new Vector2(1, 0);
        orders.anchorMax        = new Vector2(1, 1);
        orders.pivot            = new Vector2(1, 1);
        orders.anchoredPosition = new Vector2(0, -56);
        orders.sizeDelta        = new Vector2(380, -56);

        // Header label
        var hdr = MakeText(orders, "ACTIVE ORDERS", 15, TextAnchor.MiddleCenter);
        hdr.color = new Color(0.85f, 0.75f, 0.3f);
        hdr.rectTransform.anchorMin        = new Vector2(0, 1);
        hdr.rectTransform.anchorMax        = new Vector2(1, 1);
        hdr.rectTransform.pivot            = new Vector2(0.5f, 1);
        hdr.rectTransform.anchoredPosition = Vector2.zero;
        hdr.rectTransform.sizeDelta        = new Vector2(0, 36);

        // Separator line
        var sep = new GameObject("Separator");
        sep.transform.SetParent(orders, false);
        sep.AddComponent<Image>().color = new Color(0.85f, 0.75f, 0.3f, 0.4f);
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin        = new Vector2(0, 1); sepRT.anchorMax = new Vector2(1, 1);
        sepRT.pivot            = new Vector2(0.5f, 1);
        sepRT.anchoredPosition = new Vector2(0, -36);
        sepRT.sizeDelta        = new Vector2(-16, 1);

        // Order rows container
        var contentGO = new GameObject("OrderContent");
        contentGO.transform.SetParent(orders, false);
        var cRT = contentGO.AddComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
        cRT.offsetMin = new Vector2(8, 8);
        cRT.offsetMax = new Vector2(-8, -40);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment      = TextAnchor.UpperLeft;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing             = 4;
        _orderContent = contentGO.transform;

        _hudRoot.SetActive(false); // menyu açılışında gizli qalsın
    }

    // Modal paneli öz Canvas-ı ilə ən üstə qaldır — MapPanel/HUD sibling sırasından asılı olmasın
    public static void PromoteToOverlay(GameObject root, int sortingOrder)
    {
        if (root == null) return;
        var c = root.GetComponent<Canvas>();
        if (c == null) c = root.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder    = sortingOrder;
        if (root.GetComponent<GraphicRaycaster>() == null)
            root.AddComponent<GraphicRaycaster>();
    }

    // Səhnədə EventSystem yoxdursa yarat — yeni Input System üçün düzgün modul ilə
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        DontDestroyOnLoad(es);
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static RectTransform MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    static Text MakeText(Transform parent, string content, int fontSize, TextAnchor align)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = fontSize;
        t.alignment = align;
        t.color     = Color.white;
        t.font      = GetBuiltinFont();
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    static Font GetBuiltinFont() => UIFont.Get();
}
