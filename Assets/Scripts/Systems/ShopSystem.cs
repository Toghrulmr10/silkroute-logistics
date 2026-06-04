using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    private ShopItem[] _items;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        var ta = Resources.Load<TextAsset>("Data/shop_items");
        var list = JsonUtility.FromJson<ShopItemList>(ta.text);
        _items = list.items;
        Debug.Log($"[Shop] {_items.Length} əşya yükləndi.");
    }

    public ShopItem[] GetAllItems() => _items;

    public bool IsAvailable(ShopItem item) =>
        !(item.isOneTime && GameState.Instance.PurchasedUpgrades.Contains(item.id));

    public bool TryPurchase(ShopItem item)
    {
        if (!IsAvailable(item))
        {
            Debug.Log($"[Shop] {item.id} artıq alınıb.");
            return false;
        }

        if (!GameState.Instance.SpendMoney(item.price))
        {
            Debug.Log($"[Shop] {item.id} üçün kredit kifayət etmir.");
            return false;
        }

        GameState.Instance.ApplyUpgrade(item);
        EventBus.ShopItemPurchased(item.id);
        Debug.Log($"[Shop] Alındı: {item.displayName} — ¥{item.price}");
        return true;
    }
}
