using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int quantity;

    public InventoryItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    public void AddQuantity(int amountToAdd)
    {
        quantity += amountToAdd;
        if (itemData != null && itemData.isStackable && quantity > itemData.maxStackSize)
        {
            quantity = itemData.maxStackSize;
        }
    }

    public void RemoveQuantity(int amountToRemove)
    {
        quantity -= amountToRemove;
    }

    public bool IsEmpty => itemData == null || quantity <= 0;
}