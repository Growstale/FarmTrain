using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public ItemData itemData; // Ссылка на основной ScriptableObject предмета

    [Header("Trading Properties")]
    public int buyPrice = 10;
    public int sellPrice = 5;

    // true - этот товар можно купить у торговца
    public bool forSale = true;
    // true - этот товар можно продать торговцу
    public bool willBuy = true;

    [Header("Stock")]
    // true - у торговца бесконечный запас этого товара
    public bool isInfiniteStock = false;
    // Начальное количество товара, если он не бесконечный
    [Min(0)] public int initialStock = 10;
}