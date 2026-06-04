using UnityEngine;
using UnityEngine.UI;

public class DayReportPanel : MonoBehaviour
{
    public static DayReportPanel Instance { get; private set; }

    private GameObject _root;
    private Text       _titleText;
    private Text       _statsText;
    private Text       _goalText;
    private Transform  _starContainer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build(UIManager.Instance.CanvasRoot);
    }

    public void Show(DaySummary s)
    {
        _titleText.text = $"GÜN {s.Day} — {s.DayTitle.ToUpper()}";

        string repSign = s.RepChange >= 0 ? "+" : "";
        _statsText.text =
            $"Çatdırıldı:   {s.Delivered}\n" +
            $"Uğursuz:      {s.Failed}\n" +
            $"Gəlir:        ¥{s.Revenue:+0;-0;0}\n" +
            $"Reputasiya:   {repSign}{s.RepChange}";

        _goalText.text  = s.GoalMet
            ? $"<color=#66ff88>✓ Hədəf yerinə yetirildi</color>\n{s.GoalDescription}"
            : $"<color=#ff5555>✗ Hədəf yerinə yetirilmədi</color>\n{s.GoalDescription}";

        RefreshStars(s.Stars);

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    public void Hide() => _root.SetActive(false);

    private void RefreshStars(int count)
    {
        foreach (Transform c in _starContainer) Destroy(c.gameObject);
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Star{i}");
            go.transform.SetParent(_starContainer, false);
            var img = go.AddComponent<Image>();
            img.color = i < count ? new Color(1f, 0.85f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 40f);
        }
    }

    private void Build(Transform canvas)
    {
        _root = new GameObject("DayReportPanel");
        _root.transform.SetParent(canvas, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.80f);
        var oRT = _root.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;

        // Kart
        var card = MakePanel(_root.transform, new Color(0.07f, 0.10f, 0.09f, 0.98f), new Vector2(640f, 500f));

        // Başlıq
        _titleText = MakeText(card, "", 20, new Color(0.9f, 0.75f, 0.25f), FontStyle.Bold);
        Pin(_titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20f), new Vector2(0, 40f));

        // Ulduzlar
        var starGO = new GameObject("Stars");
        starGO.transform.SetParent(card, false);
        var starHLG = starGO.AddComponent<HorizontalLayoutGroup>();
        starHLG.spacing           = 12f;
        starHLG.childAlignment    = TextAnchor.MiddleCenter;
        starHLG.childControlWidth = false;
        starHLG.childControlHeight = false;
        var starRT = starGO.GetComponent<RectTransform>();
        starRT.anchorMin = new Vector2(0, 1); starRT.anchorMax = new Vector2(1, 1);
        starRT.pivot = new Vector2(0.5f, 1);
        starRT.anchoredPosition = new Vector2(0, -68f);
        starRT.sizeDelta = new Vector2(0, 50f);
        _starContainer = starGO.transform;

        // Statistika
        _statsText = MakeText(card, "", 15, new Color(0.85f, 0.85f, 0.85f));
        Pin(_statsText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -128f), new Vector2(-24f, 110f));
        _statsText.alignment = TextAnchor.MiddleLeft;

        // Hədəf
        _goalText = MakeText(card, "", 14, Color.white);
        _goalText.supportRichText = true;
        Pin(_goalText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -248f), new Vector2(-24f, 60f));
        _goalText.alignment = TextAnchor.MiddleCenter;

        // Separator
        var sep = new GameObject("Sep");
        sep.transform.SetParent(card, false);
        sep.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 0.4f);
        var sRT = sep.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(0.5f, 1);
        sRT.anchoredPosition = new Vector2(0, -314f);
        sRT.sizeDelta = new Vector2(-16f, 1f);

        // Düymələr
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(card, false);
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f; hlg.childForceExpandWidth = true;
        hlg.childControlHeight = false; hlg.padding = new RectOffset(16, 16, 0, 0);
        var bRT = btnRow.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 0);
        bRT.pivot = new Vector2(0.5f, 0);
        bRT.anchoredPosition = new Vector2(0, 20f);
        bRT.sizeDelta = new Vector2(0, 56f);

        MakeRowBtn(btnRow.transform, "Mağaza", new Color(0.10f, 0.28f, 0.38f),
                   () => { Hide(); CampaignManager.Instance.ProceedToShop(); });
        MakeRowBtn(btnRow.transform, "Növbəti gün →", new Color(0.12f, 0.38f, 0.18f),
                   () => { Hide(); CampaignManager.Instance.ProceedToNextDay(); });

        _root.SetActive(false);
    }

    private static void MakeRowBtn(Transform parent, string label, Color color,
                                    UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var tGO = new GameObject("L");
        tGO.transform.SetParent(go.transform, false);
        var t = tGO.AddComponent<Text>();
        t.text = label; t.fontSize = 17; t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white; t.font = GetFont();
        var tRT = t.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
    }

    private static RectTransform MakePanel(Transform parent, Color color, Vector2 size)
    {
        var go = new GameObject("Card");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return rt;
    }

    private static Text MakeText(RectTransform parent, string content, int size, Color color,
                                  FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.font = GetFont();
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    private static void Pin(RectTransform rt, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
