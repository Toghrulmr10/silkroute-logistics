using System;
using UnityEngine;
using UnityEngine.UI;

/// SilkRoute açılış sekansı — Yeni Oyun başlayanda dünyaya/hekayəyə giriş.
/// 4 hekayə kartı göstərir (Bao'an → rəqiblər → ROUTE → keçid), sonra Gün 1-i başladır.
/// Oyun mexanikasına toxunmur — yalnız təqdimat qatıdır.
public class IntroPanel : MonoBehaviour
{
    public static IntroPanel Instance { get; private set; }

    private struct Card
    {
        public string Title;
        public string Body;     // rich text dəstəklənir
        public Color  Bg;       // atmosfer fonu (köhnə → yeni)
        public Color  Accent;   // başlıq + xətt rəngi
    }

    // ── Hekayə kartları (ssenariyə sadiq) ──────────────────────────────────────
    private static readonly Card[] Cards =
    {
        new Card {
            Title  = "深圳 — ŞENJEN",
            Body   = "Bao'an sənaye rayonu. Sənin köhnə anbarın.\n\n" +
                     "Forkliftlər, kağız siyahılar, gecikən çatdırılmalar...\n\n" +
                     "Bu, illər öncə qurulmuş şirkətdir.\n" +
                     "<color=#cfcfcf>Amma dünya dəyişir.</color>",
            Bg     = new Color(0.10f, 0.11f, 0.12f, 1f),   // boz / köhnə
            Accent = new Color(0.78f, 0.74f, 0.55f)
        },
        new Card {
            Title  = "RƏQİBLƏR",
            Body   = "Şəhərin o biri başında — <b>Nanşan Texno-Park.</b>\n\n" +
                     "Onların anbarlarında robotlar işləyir,\nsəmasında dronlar uçur.\n\n" +
                     "Daha ucuz. Daha sürətli. Daha dəqiq.\n" +
                     "<color=#ff8a5c>Müştərilərin bir-bir onlara axışır...</color>",
            Bg     = new Color(0.10f, 0.10f, 0.15f, 1f),   // soyuq mavimtıl
            Accent = new Color(0.95f, 0.55f, 0.35f)
        },
        new Card {
            Title  = "ROUTE",
            Body   = "Ya uyğunlaşacaqsan, ya da bazardan çıxacaqsan.\n\n" +
                     "Yanında yeni bir səs var — <b>ROUTE.</b>\n\n" +
                     "<color=#5fd0ff><i>\"Mən sənə ən yaxşı planı təklif edəcəm.\n" +
                     "Amma son qərarı sən verəcəksən.\"</i></color>\n\n" +
                     "İnsan + AI. Birgə.",
            Bg     = new Color(0.06f, 0.13f, 0.16f, 1f),   // teal — texnoloji
            Accent = new Color(0.40f, 0.85f, 1.00f)
        },
        new Card {
            Title  = "KEÇİD BAŞLAYIR",
            Body   = "7 gün. Köhnə üsuldan yeni dünyaya keçid.\n\n" +
                     "<color=#9fd8a0>Bao'an yenidən parlaya bilərmi?</color>\n\n" +
                     "<b>GÜN 1 — Köhnə günlər</b>",
            Bg     = new Color(0.05f, 0.13f, 0.13f, 1f),   // ümidli — neonun başlanğıcı
            Accent = new Color(0.95f, 0.80f, 0.30f)
        },
    };

    private GameObject _root;
    private Image      _bg;
    private Image      _bgImage;   // kart fon şəkli (varsa)
    private Image      _scrim;     // mətn oxunaqlığı üçün qaranlıq pərdə
    private Text       _titleText;
    private Image      _accentLine;
    private Text       _bodyText;
    private Text       _nextLabel;
    private Text       _progressText;

    private Action _onComplete;
    private int    _index;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build(UIManager.Instance.CanvasRoot);
    }

    // ── Açılış sekansını göstər ────────────────────────────────────────────────
    public void Show(Action onComplete)
    {
        _onComplete = onComplete;
        _index      = 0;
        UIManager.PromoteToOverlay(_root, 120); // hər şeyin üstündə
        Populate();
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    public void Hide() => _root.SetActive(false);

    private void Next()
    {
        _index++;
        if (_index >= Cards.Length) { Finish(); return; }
        Populate();
    }

    private void Skip()  => Finish();

    private void Finish()
    {
        Hide();
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

    private void Populate()
    {
        var c = Cards[_index];
        _bg.color          = c.Bg;

        // Fon şəkli varsa göstər (üstünə qaranlıq pərdə); yoxdursa düz rəng qalır
        var bgSprite = SpriteLib.Get($"Sprites/Backgrounds/intro_{_index + 1}");
        if (bgSprite != null)
        {
            _bgImage.sprite  = bgSprite;
            _bgImage.color   = Color.white;
            _bgImage.enabled = true;
            _scrim.enabled   = true;
        }
        else
        {
            _bgImage.enabled = false;
            _scrim.enabled   = false;
        }

        _titleText.text    = c.Title;
        _titleText.color   = c.Accent;
        _accentLine.color  = c.Accent;
        _bodyText.text     = c.Body;
        _progressText.text = $"{_index + 1} / {Cards.Length}";
        _nextLabel.text    = _index == Cards.Length - 1 ? "Başla  ▶" : "Davam et  →";
    }

    // ── UI qurulması ───────────────────────────────────────────────────────────
    private void Build(Transform canvas)
    {
        _root = new GameObject("IntroPanel");
        _root.transform.SetParent(canvas, false);
        _bg = _root.AddComponent<Image>();
        _bg.color = Cards[0].Bg;
        var bgRT = _root.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Fon şəkli (Resources/Sprites/Backgrounds/intro_N) — düz rəngin üstündə
        _bgImage = MakeFullScreen(_root.transform, "BgImage");
        _bgImage.enabled = false;

        // Qaranlıq pərdə — şəklin üstündə, mətnin altında (oxunaqlıq üçün)
        _scrim = MakeFullScreen(_root.transform, "Scrim");
        _scrim.color   = new Color(0f, 0f, 0f, 0.5f);
        _scrim.enabled = false;

        // Başlıq
        _titleText = MakeText(_root.transform, "", 40, TextAnchor.MiddleCenter, FontStyle.Bold,
                              new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.84f));

        // Başlıq altı vurğu xətti
        var lineGO = new GameObject("AccentLine");
        lineGO.transform.SetParent(_root.transform, false);
        _accentLine = lineGO.AddComponent<Image>();
        var lRT = lineGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.35f, 0.72f);
        lRT.anchorMax = new Vector2(0.65f, 0.72f);
        lRT.offsetMin = new Vector2(0, -2f);
        lRT.offsetMax = new Vector2(0,  2f);

        // Hekayə mətni
        _bodyText = MakeText(_root.transform, "", 22, TextAnchor.MiddleCenter, FontStyle.Normal,
                             new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.68f));
        _bodyText.color           = new Color(0.88f, 0.90f, 0.90f);
        _bodyText.supportRichText = true;
        _bodyText.lineSpacing     = 1.15f;

        // "Davam et / Başla" düyməsi
        _nextLabel = MakeButton(_root.transform, "Davam et  →", new Color(0.15f, 0.40f, 0.30f),
                                new Vector2(0.28f, 0.10f), new Vector2(0.72f, 0.165f), Next);

        // "Keç" düyməsi (sağ üst)
        MakeButton(_root.transform, "Keç  ›", new Color(0.18f, 0.18f, 0.20f),
                   new Vector2(0.78f, 0.915f), new Vector2(0.96f, 0.965f), Skip);

        // Mərhələ göstəricisi (alt)
        _progressText = MakeText(_root.transform, "1 / 4", 14, TextAnchor.MiddleCenter, FontStyle.Normal,
                                 new Vector2(0.40f, 0.055f), new Vector2(0.60f, 0.085f));
        _progressText.color = new Color(0.55f, 0.55f, 0.55f);

        _root.SetActive(false);
    }

    // ── Köməkçilər ─────────────────────────────────────────────────────────────
    private static Image MakeFullScreen(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    private static Text MakeText(Transform parent, string content, int size, TextAnchor align,
                                 FontStyle style, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.alignment = align;
        t.fontStyle = style;
        t.color     = Color.white;
        t.font      = UIFont.Get();
        var rt = t.rectTransform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    private static Text MakeButton(Transform parent, string label, Color color,
                                   Vector2 anchorMin, Vector2 anchorMax, Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = color * 1.3f;
        cols.pressedColor     = color * 0.8f;
        btn.colors = cols;
        btn.onClick.AddListener(() => onClick());
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var tGO = new GameObject("Lbl");
        tGO.transform.SetParent(go.transform, false);
        var t = tGO.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = Color.white;
        t.font      = UIFont.Get();
        var tRT = t.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        return t; // etiket Text-i qaytarılır (mətnini sonra dəyişmək üçün)
    }
}
