using UnityEngine;
using System.Collections.Generic;

public class ShopDataManager : MonoBehaviour
{
    public static ShopDataManager Instance { get; private set; }

    private Dictionary<ShopInventoryData, Dictionary<ItemData, int>> runtimeShopStock = new Dictionary<ShopInventoryData, Dictionary<ItemData, int>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void InitializeShop(ShopInventoryData shopData)
    {
        if (runtimeShopStock.ContainsKey(shopData)) return;

        var itemStock = new Dictionary<ItemData, int>();
        foreach (var shopItem in shopData.shopItems)
        {
            if (!shopItem.isInfiniteStock)
            {
                itemStock[shopItem.itemData] = shopItem.initialStock;
            }
        }
        runtimeShopStock[shopData] = itemStock;
        Debug.Log($"Инициализирован магазин '{shopData.name}'");
    }

    public int GetCurrentStock(ShopInventoryData shopData, ItemData itemData)
    {
        var shopItem = shopData.shopItems.Find(x => x.itemData == itemData);
        if (shopItem != null && shopItem.isInfiniteStock) return 999;

        if (runtimeShopStock.TryGetValue(shopData, out var stock) && stock.TryGetValue(itemData, out int currentStock))
        {
            return currentStock;
        }
        return 0;
    }

    public void DecreaseStock(ShopInventoryData shopData, ItemData itemData, int quantity)
    {
        if (runtimeShopStock.TryGetValue(shopData, out var stock) && stock.ContainsKey(itemData))
        {
            stock[itemData] -= quantity;
            if (stock[itemData] < 0) stock[itemData] = 0;
        }
    }

    public void IncreaseStock(ShopInventoryData shopData, ItemData itemData, int quantity)
    {
        if (runtimeShopStock.TryGetValue(shopData, out var stock))
        {
            if (stock.ContainsKey(itemData))
            {
                stock[itemData] += quantity;
            }
            else
            {
                stock[itemData] = quantity;
            }
        }
    }
}