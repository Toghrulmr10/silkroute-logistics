using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    public static GameOverPanel Instance { get; private set; }

    private GameObject _root;
    private Text       _headerText;
    private Text       _bodyText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build(UIManager.Instance.CanvasRoot);
    }

    void Start()
    {
        EventBus.OnGameWon  += ShowWin;
        EventBus.OnGameLost += ShowLose;
    }

    private void ShowWin()
    {
        _headerText.text = "TƏBRİKLƏR!";
        _headerText.color = new Color(0.9f, 0.75f, 0.25f);
        _bodyText.text =
            "7 günü uğurla başa vurdunuz!\n\n" +
            $"Son kredit:     ¥{GameState.Instance.Money:N0}\n" +
            $"Reputasiya:    {GameState.Instance.Reputation}/100\n\n" +
            "SilkRoute Logistics — tam avtomatlaşma\nmüasir dövrün tələbidir.";
        UIManager.PromoteToOverlay(_root, 70);
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    private void ShowLose(string reason)
    {
        _headerText.text  = "OYUN BİTDİ";
        _headerText.color = new Color(0.9f, 0.25f, 0.25f);
        _bodyText.text =
            $"{reason}\n\n" +
            $"Gün:            {GameState.Instance.Day}/7\n" +
            $"Kredit:         ¥{GameState.Instance.Money:N0}\n" +
            $"Reputasiya:    {GameState.Instance.Reputation}/100\n\n" +
            "Uyğunlaşmayanlar\nbazarda sağ qala bilmir.";
        UIManager.PromoteToOverlay(_root, 70);
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    private void Build(Transform canvas)
    {
        _root = new GameObject("GameOverPanel");
        _root.transform.SetParent(canvas, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.90f);
        var oRT = _root.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;

        var card = new GameObject("Card");
        card.transform.SetParent(_root.transform, false);
        card.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.07f, 0.98f);
        var cRT = card.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(580f, 420f);

        // Başlıq
        var hGO = new GameObject("Header"); hGO.transform.SetParent(card.transform, false);
        _headerText = hGO.AddComponent<Text>();
        _headerText.fontSize = 36; _headerText.alignment = TextAnchor.MiddleCenter;
        _headerText.fontStyle = FontStyle.Bold; _headerText.font = GetFont();
        var hRT = _headerText.rectTransform;
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(0.5f, 1);
        hRT.anchoredPosition = new Vector2(0, -24f); hRT.sizeDelta = new Vector2(0, 50f);

        // Gövdə mətn
        var bGO = new GameObject("Body"); bGO.transform.SetParent(card.transform, false);
        _bodyText = bGO.AddComponent<Text>();
        _bodyText.fontSize = 16; _bodyText.alignment = TextAnchor.MiddleCenter;
        _bodyText.color = new Color(0.80f, 0.80f, 0.80f); _bodyText.font = GetFont();
        var bRT = _bodyText.rectTransform;
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(24f, 70f); bRT.offsetMax = new Vector2(-24f, -80f);

        // Yenidən oyna
        var btnGO = new GameObject("RestartBtn"); btnGO.transform.SetParent(card.transform, false);
        btnGO.AddComponent<Image>().color = new Color(0.15f, 0.38f, 0.20f);
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            _root.SetActive(false);
            CampaignManager.Instance.StartNewGame();
        });
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.1f, 0); btnRT.anchorMax = new Vector2(0.9f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 20f); btnRT.sizeDelta = new Vector2(0, 56f);

        var lGO = new GameObject("L"); lGO.transform.SetParent(btnGO.transform, false);
        var lTxt = lGO.AddComponent<Text>();
        lTxt.text = "Yenidən oyna"; lTxt.fontSize = 20;
        lTxt.alignment = TextAnchor.MiddleCenter; lTxt.color = Color.white; lTxt.font = GetFont();
        var lRT = lTxt.rectTransform;
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        _root.SetActive(false);
    }

    private static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
