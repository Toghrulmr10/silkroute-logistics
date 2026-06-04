using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapView : MonoBehaviour
{
    public static MapView Instance { get; private set; }

    static readonly (string Name, Vector2 UV)[] Cities =
    {
        ("Xi'an",          new Vector2(0.10f, 0.46f)),
        ("Dunhuang",       new Vector2(0.27f, 0.61f)),
        ("Kashgar",        new Vector2(0.43f, 0.55f)),
        ("Samarkand",      new Vector2(0.57f, 0.59f)),
        ("Merv",           new Vector2(0.67f, 0.46f)),
        ("Baghdad",        new Vector2(0.78f, 0.34f)),
        ("Constantinople", new Vector2(0.91f, 0.57f)),
    };

    static readonly Color[] TypeColors =
    {
        new Color(0.35f, 1.0f,  0.45f), // Courier  — green
        new Color(1.0f,  0.65f, 0.2f),  // Robot    — orange
        new Color(0.3f,  0.85f, 1.0f),  // Drone    — cyan
    };

    RectTransform _panel;
    Transform _gridLayer;
    Transform _staticRouteLayer;
    Transform _activeRouteLayer;
    Transform _cityLayer;
    Transform _vehicleLayer;

    readonly Dictionary<int, List<GameObject>> _routeLines = new();
    readonly Dictionary<int, GameObject>       _vehicles   = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildPanel(UIManager.Instance.CanvasRoot);

        EventBus.OnOrderCreated   += OnOrderCreated;
        EventBus.OnOrderDelivered += o => RemoveOrder(o.Id);
        EventBus.OnOrderFailed    += o => RemoveOrder(o.Id);

        StartCoroutine(DrawAfterLayout());
    }

    // ── Panel construction ───────────────────────────────────────────────────

    void BuildPanel(Transform canvasRoot)
    {
        var go = new GameObject("MapPanel");
        go.transform.SetParent(canvasRoot, false);
        go.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.07f, 1f);

        _panel = go.GetComponent<RectTransform>();
        _panel.anchorMin = Vector2.zero;
        _panel.anchorMax = Vector2.one;
        _panel.offsetMin = Vector2.zero;
        _panel.offsetMax = new Vector2(-380f, -56f);
        _panel.SetAsFirstSibling(); // render behind top bar and orders panel

        _gridLayer        = MakeLayer("Grid");
        _staticRouteLayer = MakeLayer("StaticRoutes");
        _activeRouteLayer = MakeLayer("ActiveRoutes");
        _cityLayer        = MakeLayer("Cities");
        _vehicleLayer     = MakeLayer("Vehicles");

        AddTitle();
        AddLegend();
    }

    void AddTitle()
    {
        var go = new GameObject("Title");
        go.transform.SetParent(_panel, false);
        var t = go.AddComponent<Text>();
        t.text      = "TRADE ROUTES";
        t.fontSize  = 13;
        t.fontStyle = FontStyle.Bold;
        t.color     = new Color(0.85f, 0.72f, 0.28f, 0.65f);
        t.font      = GetBuiltinFont();
        t.alignment = TextAnchor.UpperLeft;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(14f, -10f);
        rt.sizeDelta        = new Vector2(200f, 22f);
    }

    void AddLegend()
    {
        string[] labels = { "Courier", "Robot", "Drone" };
        for (int i = 0; i < 3; i++)
        {
            float yOff = -10f - i * 20f;

            var dot = new GameObject($"Legend_dot_{i}");
            dot.transform.SetParent(_panel, false);
            dot.AddComponent<Image>().color = TypeColors[i];
            var dRT = dot.GetComponent<RectTransform>();
            dRT.anchorMin = dRT.anchorMax = new Vector2(0f, 1f);
            dRT.pivot     = new Vector2(0f, 1f);
            dRT.anchoredPosition = new Vector2(14f, yOff - 30f);
            dRT.sizeDelta        = new Vector2(8f, 8f);

            var lbl = new GameObject($"Legend_lbl_{i}");
            lbl.transform.SetParent(_panel, false);
            var t = lbl.AddComponent<Text>();
            t.text      = labels[i];
            t.fontSize  = 10;
            t.color     = new Color(0.78f, 0.70f, 0.52f);
            t.font      = GetBuiltinFont();
            t.alignment = TextAnchor.MiddleLeft;
            var lRT = t.rectTransform;
            lRT.anchorMin = lRT.anchorMax = new Vector2(0f, 1f);
            lRT.pivot     = new Vector2(0f, 1f);
            lRT.anchoredPosition = new Vector2(26f, yOff - 26f);
            lRT.sizeDelta        = new Vector2(70f, 16f);
        }
    }

    // ── Static map drawing (deferred one frame for layout) ───────────────────

    IEnumerator DrawAfterLayout()
    {
        yield return null;
        DrawGrid();
        DrawStaticRoutes();
        DrawCities();
    }

    void DrawGrid()
    {
        var col = new Color(0.25f, 0.30f, 0.22f, 0.22f);
        for (int i = 1; i < 10; i++)
        {
            float f = i / 10f;
            CreateLine(_gridLayer, new Vector2(f, 0f), new Vector2(f, 1f), col, 0.5f);
            CreateLine(_gridLayer, new Vector2(0f, f), new Vector2(1f, f), col, 0.5f);
        }
    }

    void DrawStaticRoutes()
    {
        var col = new Color(0.50f, 0.35f, 0.12f, 0.55f);
        for (int i = 0; i < Cities.Length - 1; i++)
            CreateLine(_staticRouteLayer, Cities[i].UV, Cities[i + 1].UV, col, 2f);
    }

    void DrawCities()
    {
        var r = _panel.rect;
        foreach (var city in Cities)
        {
            var pos = UVToLocal(city.UV, r);

            var dot = new GameObject(city.Name);
            dot.transform.SetParent(_cityLayer, false);
            dot.AddComponent<Image>().color = new Color(0.95f, 0.82f, 0.32f);
            var dRT = dot.GetComponent<RectTransform>();
            dRT.anchorMin = dRT.anchorMax = new Vector2(0.5f, 0.5f);
            dRT.pivot     = new Vector2(0.5f, 0.5f);
            dRT.anchoredPosition = pos;
            dRT.sizeDelta        = new Vector2(9f, 9f);

            var lbl = new GameObject(city.Name + "_lbl");
            lbl.transform.SetParent(_cityLayer, false);
            var t = lbl.AddComponent<Text>();
            t.text      = city.Name;
            t.fontSize  = 10;
            t.color     = new Color(0.88f, 0.78f, 0.55f);
            t.font      = GetBuiltinFont();
            t.alignment = TextAnchor.LowerLeft;
            var lRT = t.rectTransform;
            lRT.anchorMin = lRT.anchorMax = new Vector2(0.5f, 0.5f);
            lRT.pivot     = new Vector2(0f, 0f);
            lRT.anchoredPosition = pos + new Vector2(7f, 3f);
            lRT.sizeDelta        = new Vector2(110f, 18f);
        }
    }

    // ── Order animation ──────────────────────────────────────────────────────

    void OnOrderCreated(Order order)
    {
        if (_panel.rect.width < 1f) return;

        int dest = Random.Range(1, Cities.Length);

        var lines = new List<GameObject>();
        for (int i = 0; i < dest; i++)
            lines.Add(CreateLine(_activeRouteLayer, Cities[i].UV, Cities[i + 1].UV,
                                 new Color(0.95f, 0.80f, 0.28f, 0.65f), 2.5f));
        _routeLines[order.Id] = lines;

        var vGO = new GameObject($"V{order.Id}");
        vGO.transform.SetParent(_vehicleLayer, false);
        vGO.AddComponent<Image>().color = TypeColors[(int)order.Type];
        var vRT = vGO.GetComponent<RectTransform>();
        vRT.anchorMin = vRT.anchorMax = new Vector2(0.5f, 0.5f);
        vRT.pivot     = new Vector2(0.5f, 0.5f);
        vRT.sizeDelta = new Vector2(10f, 10f);
        _vehicles[order.Id] = vGO;

        StartCoroutine(AnimateVehicle(order.Id, dest, order.DeadlineSeconds));
    }

    IEnumerator AnimateVehicle(int orderId, int destIdx, float duration)
    {
        var path    = BuildPath(destIdx);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!_vehicles.TryGetValue(orderId, out var vehicle)) yield break;

            var uv = SamplePath(path, elapsed / duration);
            vehicle.GetComponent<RectTransform>().anchoredPosition = UVToLocal(uv, _panel.rect);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void RemoveOrder(int id)
    {
        if (_vehicles.TryGetValue(id, out var v))   { Destroy(v); _vehicles.Remove(id); }
        if (_routeLines.TryGetValue(id, out var ls)) { foreach (var l in ls) Destroy(l); _routeLines.Remove(id); }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    GameObject CreateLine(Transform parent, Vector2 uvA, Vector2 uvB, Color color, float thickness)
    {
        var go = new GameObject("Ln");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var r = _panel.rect;
        var a = UVToLocal(uvA, r);
        var b = UVToLocal(uvB, r);
        var d = b - a;
        rt.anchoredPosition = (a + b) * 0.5f;
        rt.sizeDelta        = new Vector2(d.magnitude, thickness);
        rt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        return go;
    }

    Transform MakeLayer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_panel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go.transform;
    }

    static Vector2[] BuildPath(int destIdx)
    {
        var pts = new Vector2[destIdx + 1];
        for (int i = 0; i <= destIdx; i++) pts[i] = Cities[i].UV;
        return pts;
    }

    static Vector2 SamplePath(Vector2[] pts, float t)
    {
        t = Mathf.Clamp01(t);
        float total = 0f;
        for (int i = 0; i < pts.Length - 1; i++) total += Vector2.Distance(pts[i], pts[i + 1]);
        float target = t * total, walked = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            float seg = Vector2.Distance(pts[i], pts[i + 1]);
            if (walked + seg >= target)
                return Vector2.Lerp(pts[i], pts[i + 1], (target - walked) / seg);
            walked += seg;
        }
        return pts[pts.Length - 1];
    }

    static Vector2 UVToLocal(Vector2 uv, Rect r) =>
        new Vector2(uv.x * r.width - r.width * 0.5f, uv.y * r.height - r.height * 0.5f);

    static Font GetBuiltinFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
