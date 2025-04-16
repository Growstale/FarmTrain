using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class Inventory : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int capacity; // Размер инвентаря

    // Событие для оповещения UI об изменениях
    public event Action<int> OnSlotChanged; // Передаем индекс измененного слота

    public void InitializeInventory(int size)
    {
        capacity = size;
        slots = new List<InventorySlot>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    // Метод добавления предмета в свободное место
    public bool AddItem(ItemData itemToAdd, int amount = 1)
    {
        if (itemToAdd == null || amount <= 0) return false;

        int remainingAmount = amount;

        // 1. Попробовать добавить в существующие стаки (если стакается)
        if (itemToAdd.isStackable)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].itemData == itemToAdd && slots[i].quantity < itemToAdd.maxStackSize)
                {
                    remainingAmount = slots[i].AddQuantity(remainingAmount);
                    OnSlotChanged?.Invoke(i);
                    if (remainingAmount <= 0) return true;
                }
            }
        }

        // 2. Попробовать добавить в пустые слоты
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].itemData == null)
            {
                int amountToAddInSlot = Mathf.Min(remainingAmount, itemToAdd.maxStackSize);
                slots[i] = new InventorySlot(itemToAdd, amountToAddInSlot);
                remainingAmount -= amountToAddInSlot;
                OnSlotChanged?.Invoke(i);
                if (remainingAmount <= 0) return true;
            }
        }

        Debug.LogWarning($"Inventory full, could not add {remainingAmount} of {itemToAdd.itemName}");
        return false; 
    }

    // Метод удаления предмета (по индексу слота)
    public void RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].itemData == null || amount <= 0) return;

        slots[slotIndex].quantity -= amount;
        if (slots[slotIndex].quantity <= 0)
        {
            slots[slotIndex].Clear(); // Очистить слот, если количество 0 или меньше
        }
        OnSlotChanged?.Invoke(slotIndex); // Оповестить UI
    }

    // Метод для проверки, есть ли предмет и достаточное количество
    public bool ContainsItem(ItemData itemToCheck, int amount = 1)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.itemData == itemToCheck)
            {
                count += slot.quantity;
                if (count >= amount) return true;
            }
        }
        return false;
    }

    // Метод для удаления определенного предмета (не по индексу, а по типу)
    public bool RemoveItemsByType(ItemData itemToRemove, int amount = 1)
    {
        if (!ContainsItem(itemToRemove, amount)) return false; // Недостаточно предметов

        int amountToRemove = amount;
        for (int i = slots.Count - 1; i >= 0; i--) // Идем с конца, чтобы удаление не сбивало индексы
        {
            if (slots[i].itemData == itemToRemove)
            {
                if (slots[i].quantity >= amountToRemove)
                {
                    RemoveItem(i, amountToRemove);
                    return true; // Удалили нужное количество
                }
                else
                {
                    amountToRemove -= slots[i].quantity;
                    RemoveItem(i, slots[i].quantity); // Удаляем всё из этого слота
                }
            }
        }
        // Сюда не должны попасть, если ContainsItem сработал верно, но на всякий случай
        return false;
    }
}