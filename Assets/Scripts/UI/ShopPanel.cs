using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    private GameObject _root;
    private Transform  _itemContainer;
    private Text       _moneyText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build(UIManager.Instance.CanvasRoot);
    }

    void Start()
    {
        EventBus.OnMoneyChanged      += m => { if (_moneyText) _moneyText.text = $"Kredit: ¥{m:N0}"; };
        EventBus.OnShopItemPurchased += _ => RefreshItems();
    }

    public void Show()
    {
        RefreshItems();
        UIManager.PromoteToOverlay(_root, 60);
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    public void Hide() => _root.SetActive(false);

    private void RefreshItems()
    {
        if (_moneyText)
            _moneyText.text = $"Kredit: ¥{GameState.Instance.Money:N0}";

        foreach (Transform c in _itemContainer) Destroy(c.gameObject);

        foreach (var item in ShopSystem.Instance.GetAllItems())
            AddItemCard(item);
    }

    private void AddItemCard(ShopItem item)
    {
        bool available = ShopSystem.Instance.IsAvailable(item);

        var go = new GameObject($"Item_{item.id}");
        go.transform.SetParent(_itemContainer, false);

        var img = go.AddComponent<Image>();
        img.color = available ? new Color(0.10f, 0.18f, 0.14f) : new Color(0.10f, 0.10f, 0.10f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 88f;

        // Sol rəng şeridi
        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(go.transform, false);
        stripe.AddComponent<Image>().color = available ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
        var sRT = stripe.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(0, 1);
        sRT.offsetMin = Vector2.zero; sRT.offsetMax = new Vector2(4f, 0);

        // Mətn
        var txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.font      = GetFont();
        txt.fontSize  = 14;
        txt.color     = available ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.supportRichText = true;

        string soldOut = !available ? " <color=#888888>[ALINDI]</color>" : "";
        txt.text = $"<b>{item.displayName}</b>{soldOut}\n" +
                   $"<color=#aaaaaa>{item.description}</color>";

        var tRT = txt.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(14f, 4f); tRT.offsetMax = new Vector2(-110f, -4f);

        // Qiymət + Al düyməsi
        if (available)
        {
            var btnGO = new GameObject("BuyBtn");
            btnGO.transform.SetParent(go.transform, false);
            btnGO.AddComponent<Image>().color = new Color(0.15f, 0.40f, 0.20f);
            var btn = btnGO.AddComponent<Button>();
            var captured = item;
            btn.onClick.AddListener(() => ShopSystem.Instance.TryPurchase(captured));
            var bRT = btnGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1, 0.5f); bRT.anchorMax = new Vector2(1, 0.5f);
            bRT.pivot = new Vector2(1, 0.5f);
            bRT.anchoredPosition = new Vector2(-8f, 0);
            bRT.sizeDelta = new Vector2(96f, 52f);

            var bTxtGO = new GameObject("L");
            bTxtGO.transform.SetParent(btnGO.transform, false);
            var bTxt = bTxtGO.AddComponent<Text>();
            bTxt.text = $"¥{item.price:0}"; bTxt.fontSize = 15;
            bTxt.alignment = TextAnchor.MiddleCenter; bTxt.color = Color.white;
            bTxt.font = GetFont();
            var btRT = bTxt.rectTransform;
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = btRT.offsetMax = Vector2.zero;
        }
    }

    private void Build(Transform canvas)
    {
        _root = new GameObject("ShopPanel");
        _root.transform.SetParent(canvas, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.85f);
        var oRT = _root.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;

        var card = new GameObject("Card");
        card.transform.SetParent(_root.transform, false);
        card.AddComponent<Image>().color = new Color(0.07f, 0.10f, 0.09f, 0.98f);
        var cRT = card.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(680f, 560f);

        // Başlıq
        var titleGO = new GameObject("Title"); titleGO.transform.SetParent(card.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "MAĞAZA"; title.fontSize = 22; title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.9f, 0.75f, 0.25f); title.font = GetFont();
        title.fontStyle = FontStyle.Bold;
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, -14f); tRT.sizeDelta = new Vector2(0, 36f);

        // Pul
        var moneyGO = new GameObject("Money"); moneyGO.transform.SetParent(card.transform, false);
        _moneyText = moneyGO.AddComponent<Text>();
        _moneyText.text = $"Kredit: ¥{GameState.Instance.Money:N0}";
        _moneyText.fontSize = 15; _moneyText.alignment = TextAnchor.MiddleRight;
        _moneyText.color = new Color(0.9f, 0.75f, 0.25f); _moneyText.font = GetFont();
        var mRT = _moneyText.rectTransform;
        mRT.anchorMin = new Vector2(0, 1); mRT.anchorMax = new Vector2(1, 1);
        mRT.pivot = new Vector2(0.5f, 1);
        mRT.anchoredPosition = new Vector2(0, -14f); mRT.sizeDelta = new Vector2(-16f, 36f);

        // Əşya siyahısı
        var listGO = new GameObject("ItemList"); listGO.transform.SetParent(card.transform, false);
        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(12, 12, 0, 0);
        var lRT = listGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(0, 60f); lRT.offsetMax = new Vector2(0, -56f);
        _itemContainer = listGO.transform;

        // Bağla düyməsi
        var closeGO = new GameObject("CloseBtn"); closeGO.transform.SetParent(card.transform, false);
        closeGO.AddComponent<Image>().color = new Color(0.25f, 0.10f, 0.10f);
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { Hide(); CampaignManager.Instance.ProceedToNextDay(); });
        var clRT = closeGO.GetComponent<RectTransform>();
        clRT.anchorMin = new Vector2(0, 0); clRT.anchorMax = new Vector2(1, 0);
        clRT.pivot = new Vector2(0.5f, 0);
        clRT.anchoredPosition = new Vector2(0, 10f); clRT.sizeDelta = new Vector2(-16f, 50f);
        var clTxtGO = new GameObject("L"); clTxtGO.transform.SetParent(closeGO.transform, false);
        var clTxt = clTxtGO.AddComponent<Text>();
        clTxt.text = "Növbəti günə keç →"; clTxt.fontSize = 17;
        clTxt.alignment = TextAnchor.MiddleCenter; clTxt.color = Color.white; clTxt.font = GetFont();
        var clTRT = clTxt.rectTransform;
        clTRT.anchorMin = Vector2.zero; clTRT.anchorMax = Vector2.one;
        clTRT.offsetMin = clTRT.offsetMax = Vector2.zero;

        _root.SetActive(false);
    }

    private static Font GetFont()
    {
        return UIFont.Get();
    }
}
