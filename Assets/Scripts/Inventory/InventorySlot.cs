using UnityEngine;
using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData; 
    public int quantity;    

    public InventorySlot(ItemData item, int amount)
    {
        itemData = item;
        quantity = amount;
    }

    public InventorySlot()
    {
        itemData = null;
        quantity = 0;
    }

    public int AddQuantity(int amountToAdd)
    {
        if (itemData == null || !itemData.isStackable) return amountToAdd;

        int maxCanAdd = itemData.maxStackSize - quantity;
        int actualAmountToAdd = Mathf.Min(amountToAdd, maxCanAdd);

        quantity += actualAmountToAdd;
        return amountToAdd - actualAmountToAdd; // Возвращаем остаток, который не влез
    }

    public void Clear()
    {
        itemData = null;
        quantity = 0;
    }
}