using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : MonoBehaviour
{
    public static MainMenuPanel Instance { get; private set; }

    private GameObject _root;
    private Button     _continueBtn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build(UIManager.Instance.CanvasRoot);
    }

    public void Show(bool hasSave)
    {
        if (_continueBtn != null)
        {
            _continueBtn.interactable = hasSave;
            var col = _continueBtn.GetComponentInChildren<Text>().color;
            _continueBtn.GetComponentInChildren<Text>().color = hasSave
                ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
        UIManager.PromoteToOverlay(_root, 100); // hər şeyin üstündə
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    public void Hide() => _root.SetActive(false);

    private void Build(Transform canvas)
    {
        _root = new GameObject("MainMenuPanel");
        _root.transform.SetParent(canvas, false);

        // Tam ekran tünd arxa fon
        var bg = _root.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.06f, 0.98f);
        var bgRT = _root.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Başlıq
        MakeText(_root.transform, "SILKROUTE", 52, new Vector2(0.5f, 0.72f),
                 new Color(0.9f, 0.75f, 0.25f), FontStyle.Bold);
        MakeText(_root.transform, "Logistics", 22, new Vector2(0.5f, 0.64f),
                 new Color(0.65f, 0.75f, 0.65f), FontStyle.Italic);
        MakeText(_root.transform, "Şenjen Logistika Simulyasiyası", 14, new Vector2(0.5f, 0.57f),
                 new Color(0.55f, 0.55f, 0.55f));

        // Düymələr
        MakeButton(_root.transform, "Yeni Oyun",    new Vector2(0.5f, 0.44f), new Color(0.15f, 0.45f, 0.20f),
                   () => { Hide(); CampaignManager.Instance.StartNewGame(); });

        _continueBtn = MakeButton(_root.transform, "Davam et", new Vector2(0.5f, 0.34f), new Color(0.10f, 0.25f, 0.35f),
                   () => { Hide(); CampaignManager.Instance.ContinueGame(); });

        MakeButton(_root.transform, "Çıxış", new Vector2(0.5f, 0.24f), new Color(0.25f, 0.10f, 0.10f),
                   () => Application.Quit());

        // Version
        MakeText(_root.transform, "v0.5 — MVP", 11, new Vector2(0.5f, 0.06f),
                 new Color(0.35f, 0.35f, 0.35f));

        _root.SetActive(false);
    }

    private static void MakeText(Transform parent, string content, int size,
                                  Vector2 anchorPos, Color color, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = color;
        t.fontStyle = style;
        t.font      = GetFont();
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchorPos;
        rt.sizeDelta = new Vector2(600f, size + 16f);
        rt.anchoredPosition = Vector2.zero;
    }

    private static Button MakeButton(Transform parent, string label, Vector2 anchorPos,
                                      Color color, UnityEngine.Events.UnityAction onClick)
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
        btn.onClick.AddListener(onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPos;
        rt.sizeDelta = new Vector2(360f, 60f);
        rt.anchoredPosition = Vector2.zero;

        var tGO = new GameObject("Lbl");
        tGO.transform.SetParent(go.transform, false);
        var t = tGO.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = 20;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = Color.white;
        t.font      = GetFont();
        var tRT = t.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        return btn;
    }

    private static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
