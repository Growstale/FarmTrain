using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Shop Inventory", menuName = "Shop/Shop Inventory")]
public class ShopInventoryData : ScriptableObject
{
    public string shopName;
    public List<ShopItem> shopItems = new List<ShopItem>();
}