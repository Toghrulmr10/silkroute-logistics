[System.Serializable]
public class ShopItem
{
    public string id;
    public string displayName;
    public string description;
    public float  price;
    public bool   isOneTime;
    public string effectType;
    public float  effectValue;
}

[System.Serializable]
public class ShopItemList
{
    public ShopItem[] items;
}
