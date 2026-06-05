using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecisionPanel : MonoBehaviour
{
    public static DecisionPanel Instance { get; private set; }

    private GameObject           _root;
    private Text                 _titleText;
    private Text                 _infoText;
    private Text                 _routeLabel;
    private Transform            _buttonContainer;

    private readonly Queue<Recommendation> _queue = new();
    private bool _showing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildPanel(UIManager.Instance.CanvasRoot);
        EventBus.OnRecommendationReady += OnRecommendationReady;
    }

    // ── Panel görünürlüyü ─────────────────────────────────────────────────────
    private void OnRecommendationReady(Recommendation rec)
    {
        _queue.Enqueue(rec);
        if (!_showing) ShowNext();
    }

    private void ShowNext()
    {
        if (_queue.Count == 0) { _showing = false; _root.SetActive(false); return; }
        _showing = true;
        Populate(_queue.Dequeue());
        UIManager.PromoteToOverlay(_root, 50); // xəritənin üstündə
        _root.transform.SetAsLastSibling(); // hər zaman ən üstdə olsun
        _root.SetActive(true);
    }

    private void OnPlanChosen(Recommendation rec, RecommendationPlan plan)
    {
        EventBus.PlayerDecided(rec.OrderId, plan);
        Debug.Log($"[Decision] ORD-{rec.OrderId} → oyunçu seçdi: {plan.MethodLabel}");
        ShowNext();
    }

    // ── Paneli doldur ─────────────────────────────────────────────────────────
    private void Populate(Recommendation rec)
    {
        var o = rec.Order;
        _titleText.text = $"SİFARİŞ #{o.Id}  —  {o.CustomerName}";
        _infoText.text  = $"{o.ProductName}  |  {o.WeightKg:F1} kq  |  {o.DistanceKm:F1} km  |  " +
                          $"⏱ {o.DeadlineSeconds:F0}s  |  ¥{o.Revenue}  [{o.Priority}]";

        // Başlıq: ROUTE AI yalnız aktiv olan günlərdə tövsiyə kimi təqdim olunur
        if (rec.AiActive)
        {
            _routeLabel.text  = "★  ROUTE AI tövsiyəsi — plan seç:";
            _routeLabel.color = new Color(0.85f, 0.75f, 0.3f);
        }
        else
        {
            _routeLabel.text  = "Çatdırılma üsulunu seç:  (ROUTE AI hələ aktiv deyil)";
            _routeLabel.color = new Color(0.7f, 0.7f, 0.7f);
        }

        // Köhnə düymələri sil
        foreach (Transform child in _buttonContainer)
            Destroy(child.gameObject);

        // Tövsiyə düyməsi — yalnız AI aktivdirsə vurğulanır
        if (rec.Recommended != null)
            AddPlanButton(rec, rec.Recommended, isRecommended: rec.AiActive);

        // Alternativlər
        foreach (var alt in rec.Alternatives)
            AddPlanButton(rec, alt, isRecommended: false);
    }

    private void AddPlanButton(Recommendation rec, RecommendationPlan plan, bool isRecommended)
    {
        var go = new GameObject("PlanBtn");
        go.transform.SetParent(_buttonContainer, false);

        var img = go.AddComponent<Image>();
        img.color = isRecommended
            ? new Color(0.15f, 0.22f, 0.12f)
            : new Color(0.10f, 0.12f, 0.14f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 100f;

        var btn = go.AddComponent<Button>();
        var captured = plan;
        btn.onClick.AddListener(() => OnPlanChosen(rec, captured));

        // Hover rengi
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.20f);
        colors.pressedColor     = new Color(0.35f, 0.50f, 0.28f);
        btn.colors = colors;

        // Mətn
        var txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.font      = GetBuiltinFont();
        txt.fontSize  = 14;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color     = Color.white;

        var riskColor = plan.Risk switch
        {
            RiskLevel.Low    => "<color=#66ff88>",
            RiskLevel.Medium => "<color=#ffdd44>",
            _                => "<color=#ff5555>"
        };

        string star = isRecommended ? "★ " : "    ";
        txt.text = $"{star}<b>{plan.MethodLabel}</b>\n" +
                   $"  ⏱ ~{plan.EstTimeSec:F0}s   💰 ¥{plan.EstCost}   " +
                   $"Risk: {riskColor}{plan.RiskLabel}</color>   " +
                   $"Uğur: {plan.SuccessProbability:P0}\n" +
                   $"  <i><color=#aaaaaa>{plan.Reasoning}</color></i>";
        txt.supportRichText = true;

        var tRT = txt.rectTransform;
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12f, 4f);
        tRT.offsetMax = new Vector2(-12f, -4f);

        // Tövsiyə üçün sol rəng şeridi
        if (isRecommended)
        {
            var stripe = new GameObject("Stripe");
            stripe.transform.SetParent(go.transform, false);
            stripe.AddComponent<Image>().color = new Color(0.4f, 0.9f, 0.3f);
            var sRT = stripe.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0);
            sRT.anchorMax = new Vector2(0, 1);
            sRT.offsetMin = Vector2.zero;
            sRT.offsetMax = new Vector2(4f, 0);
        }
    }

    // ── Panel qurulması ───────────────────────────────────────────────────────
    private void BuildPanel(Transform canvasRoot)
    {
        // Tam ekran tünd örtük
        _root = new GameObject("DecisionPanel");
        _root.transform.SetParent(canvasRoot, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        var oRT = _root.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero;
        oRT.anchorMax = Vector2.one;
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;

        // Mərkəzi kart
        var card = new GameObject("Card");
        card.transform.SetParent(_root.transform, false);
        card.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.10f, 0.98f);
        var cRT = card.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot     = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(720f, 480f);

        // Başlıq
        var titleGO = MakeText(card.transform, "", 18, TextAnchor.UpperCenter, Color.white);
        _titleText = titleGO.GetComponent<Text>();
        var tRT = _titleText.rectTransform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = Vector2.one;
        tRT.pivot     = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, -16f);
        tRT.sizeDelta = new Vector2(0, 36f);

        // ROUTE etiketi
        var routeLabel = MakeText(card.transform, "★  ROUTE AI tövsiyəsi — plan seç:", 13,
                                   TextAnchor.MiddleLeft, new Color(0.85f, 0.75f, 0.3f));
        _routeLabel = routeLabel.GetComponent<Text>();
        var rLRT = _routeLabel.rectTransform;
        rLRT.anchorMin = new Vector2(0, 1); rLRT.anchorMax = Vector2.one;
        rLRT.pivot     = new Vector2(0.5f, 1);
        rLRT.anchoredPosition = new Vector2(0, -58f);
        rLRT.sizeDelta = new Vector2(-24f, 28f);

        // Sifariş məlumatı
        var infoGO = MakeText(card.transform, "", 12, TextAnchor.MiddleLeft,
                               new Color(0.75f, 0.75f, 0.75f));
        _infoText = infoGO.GetComponent<Text>();
        var iRT = _infoText.rectTransform;
        iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = Vector2.one;
        iRT.pivot     = new Vector2(0.5f, 1);
        iRT.anchoredPosition = new Vector2(0, -90f);
        iRT.sizeDelta = new Vector2(-24f, 28f);

        // Separator xətt
        var sep = new GameObject("Sep");
        sep.transform.SetParent(card.transform, false);
        sep.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 0.5f);
        var sRT2 = sep.GetComponent<RectTransform>();
        sRT2.anchorMin = new Vector2(0, 1); sRT2.anchorMax = Vector2.one;
        sRT2.pivot     = new Vector2(0.5f, 1);
        sRT2.anchoredPosition = new Vector2(0, -120f);
        sRT2.sizeDelta = new Vector2(-16f, 1f);

        // Düymə konteyneri (VerticalLayoutGroup)
        var btnArea = new GameObject("BtnArea");
        btnArea.transform.SetParent(card.transform, false);
        var baRT = btnArea.AddComponent<RectTransform>();
        baRT.anchorMin = Vector2.zero; baRT.anchorMax = Vector2.one;
        baRT.offsetMin = new Vector2(12f, 12f);
        baRT.offsetMax = new Vector2(-12f, -126f);
        var vlg = btnArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 8f;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment      = TextAnchor.UpperCenter;
        _buttonContainer = btnArea.transform;

        _root.SetActive(false);
    }

    private static GameObject MakeText(Transform parent, string content, int size,
                                       TextAnchor align, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.alignment = align;
        t.color     = color;
        t.font      = GetBuiltinFont();
        return go;
    }

    private static Font GetBuiltinFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
