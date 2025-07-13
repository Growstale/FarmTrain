using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class StartingItemInfo
{
    public ItemData itemData;
    [Min(1)] public int quantity = 1;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Data")]
    [field: SerializeField] public int hotbarSize { get; private set; } = 7;
    [field: SerializeField] public int maxMainInventoryRows { get; private set; } = 3;
    [field: SerializeField] public int columns { get; private set; } = 7;

    public int currentMainInventoryRows { get; private set; } = 1;
    private List<InventoryItem> inventoryItems;

    [Header("Data References")]
    [Tooltip("Ссылка на ItemData для улучшения склада.")]
    [SerializeField] private ItemData storageUpgradeData;
    public ItemData StorageUpgradeData => storageUpgradeData;

    [Header("State")]
    [SerializeField] private int selectedSlotIndex = 0;
    public int SelectedSlotIndex => selectedSlotIndex;

    [Header("Starting Inventory")]
    [SerializeField] private List<StartingItemInfo> startingItems = new List<StartingItemInfo>();

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;
    public event Action<ItemData, int> OnItemAdded;

    #region Unity Lifecycle & Initialization

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeInventory();
    }

    private void Update()
    {
        HandleHotbarInput();
        UpdateInventorySizeBasedOnUpgrades();
    }

    private void InitializeInventory()
    {
        int totalSlots = hotbarSize + (maxMainInventoryRows * columns);
        inventoryItems = new List<InventoryItem>(totalSlots);
        for (int i = 0; i < totalSlots; i++)
        {
            inventoryItems.Add(null);
        }
    }

    private void AddStartingItems()
    {
        if (startingItems == null || startingItems.Count == 0) return;
        foreach (var itemInfo in startingItems)
        {
            if (itemInfo.itemData != null && itemInfo.quantity > 0)
            {
                AddItem(itemInfo.itemData, itemInfo.quantity);
            }
        }
    }

    #endregion

    #region Data & State Management

    private void HandleHotbarInput()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { SelectSlot(i); break; }
        }
    }

    private void UpdateInventorySizeBasedOnUpgrades()
    {
        int targetRows = 1;
        if (TrainUpgradeManager.Instance != null && storageUpgradeData != null)
        {
            if (TrainUpgradeManager.Instance.HasUpgrade(storageUpgradeData)) targetRows = 3;
        }
        if (targetRows != currentMainInventoryRows) SetMainInventorySize(targetRows);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        selectedSlotIndex = index;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
    }

    public void SetMainInventorySize(int numberOfRows)
    {
        currentMainInventoryRows = Mathf.Clamp(numberOfRows, 1, maxMainInventoryRows);
        ClearInactiveSlots();
        OnInventoryChanged?.Invoke();
    }

    private void ClearInactiveSlots()
    {
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (!IsSlotActive(i) && inventoryItems[i] != null && !inventoryItems[i].IsEmpty)
            {
                inventoryItems[i] = null;
            }
        }
    }

    #endregion

    #region Public API for Item Manipulation

    // Метод AddItem остается без изменений
    public bool AddItem(ItemData itemToAdd, int quantity = 1)
    {
        if (itemToAdd == null || quantity <= 0) return false;
        int currentSize = GetCurrentInventorySize();
        if (itemToAdd.isStackable)
        {
            for (int i = 0; i < currentSize; i++)
            {
                InventoryItem currentItem = inventoryItems[i];
                if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToAdd)
                {
                    int spaceAvailable = itemToAdd.maxStackSize - currentItem.quantity;
                    if (spaceAvailable >= quantity) { currentItem.AddQuantity(quantity); OnInventoryChanged?.Invoke(); OnItemAdded?.Invoke(itemToAdd, quantity); return true; }
                    else if (spaceAvailable > 0) { currentItem.AddQuantity(spaceAvailable); quantity -= spaceAvailable; }
                }
            }
        }
        int firstEmptySlot = FindFirstEmptySlot();
        if (firstEmptySlot != -1) { inventoryItems[firstEmptySlot] = new InventoryItem(itemToAdd, quantity); OnInventoryChanged?.Invoke(); OnItemAdded?.Invoke(itemToAdd, quantity); return true; }
        return false;
    }

    // Метод RemoveItem остается без изменений
    public void RemoveItem(int index, int quantity = 1)
    {
        if (index < 0 || index >= inventoryItems.Count || inventoryItems[index] == null || inventoryItems[index].IsEmpty) return;
        inventoryItems[index].RemoveQuantity(quantity);
        if (inventoryItems[index].quantity <= 0) inventoryItems[index] = null;
        OnInventoryChanged?.Invoke();
    }

    // Метод MoveItem остается без изменений
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (!IsSlotActive(fromIndex) || !IsSlotActive(toIndex) || fromIndex == toIndex) return;
        InventoryItem itemFrom = inventoryItems[fromIndex]; InventoryItem itemTo = inventoryItems[toIndex];
        if (itemFrom != null && !itemFrom.IsEmpty && itemTo != null && !itemTo.IsEmpty && itemFrom.itemData == itemTo.itemData && itemFrom.itemData.isStackable)
        {
            int spaceInTarget = itemTo.itemData.maxStackSize - itemTo.quantity;
            if (spaceInTarget > 0) { int amountToMove = Mathf.Min(itemFrom.quantity, spaceInTarget); itemTo.AddQuantity(amountToMove); itemFrom.RemoveQuantity(amountToMove); if (itemFrom.quantity <= 0) inventoryItems[fromIndex] = null; }
            else { (inventoryItems[toIndex], inventoryItems[fromIndex]) = (itemFrom, itemTo); }
        }
        else { (inventoryItems[toIndex], inventoryItems[fromIndex]) = (itemFrom, itemTo); }
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItemByType(ItemData itemToRemove, int quantityToRemove)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return;
        int quantityLeftToRemove = quantityToRemove;

        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            if (!IsSlotActive(i)) continue;

            InventoryItem currentItem = inventoryItems[i];
            if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToRemove)
            {
                if (currentItem.quantity > quantityLeftToRemove)
                {
                    currentItem.RemoveQuantity(quantityLeftToRemove);
                    quantityLeftToRemove = 0;
                    // УДАЛЕНА СТРОКА: UpdateSlotUI(i);
                    break;
                }
                else
                {
                    quantityLeftToRemove -= currentItem.quantity;
                    inventoryItems[i] = null;
                    // УДАЛЕНА СТРОКА: UpdateSlotUI(i);
                }
            }
            if (quantityLeftToRemove <= 0) break;
        }

        // В конце вызываем ОДНО общее событие, которое обновит весь UI
        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region Getters & Checks

    // Все методы в этом регионе остаются без изменений
    public InventoryItem GetItemInSlot(int index) { if (index < 0 || index >= inventoryItems.Count) return null; return inventoryItems[index]; }
    public InventoryItem GetSelectedItem() { return GetItemInSlot(selectedSlotIndex); }
    private int FindFirstEmptySlot() { for (int i = 0; i < GetCurrentInventorySize(); i++) { if (inventoryItems[i] == null || inventoryItems[i].IsEmpty) return i; } return -1; }
    public int GetCurrentInventorySize() { return hotbarSize + (currentMainInventoryRows * columns); }
    private bool IsSlotActive(int index) { return index >= 0 && index < GetCurrentInventorySize(); }
    public int GetTotalItemQuantity(ItemData itemToCount) { int total = 0; if (itemToCount == null) return 0; for (int i = 0; i < GetCurrentInventorySize(); i++) { if (IsSlotActive(i)) { InventoryItem currentItem = inventoryItems[i]; if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToCount) { total += currentItem.quantity; } } } return total; }
    public bool CheckForSpace(ItemData itemToAdd, int quantity) { int currentSize = GetCurrentInventorySize(); if (itemToAdd == null || quantity <= 0) return false; int quantityLeft = quantity; if (itemToAdd.isStackable) { for (int i = 0; i < currentSize; i++) { if (!IsSlotActive(i)) continue; InventoryItem currentItem = inventoryItems[i]; if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToAdd) { int space = itemToAdd.maxStackSize - currentItem.quantity; quantityLeft -= space; if (quantityLeft <= 0) return true; } } } for (int i = 0; i < currentSize; i++) { if (!IsSlotActive(i)) continue; if (inventoryItems[i] == null || inventoryItems[i].IsEmpty) { quantityLeft -= itemToAdd.maxStackSize; if (quantityLeft <= 0) return true; } } return false; }

    #endregion

    #region Save & Load System

    // Методы GetSaveData и ApplySaveData остаются без изменений
    public List<InventoryItemSaveData> GetSaveData()
    {
        var saveData = new List<InventoryItemSaveData>();
        for (int i = 0; i < inventoryItems.Count; i++) { if (inventoryItems[i] != null && !inventoryItems[i].IsEmpty) { saveData.Add(new InventoryItemSaveData { itemName = inventoryItems[i].itemData.name, quantity = inventoryItems[i].quantity, slotIndex = i }); } }
        return saveData;
    }
    public void ApplySaveData(List<InventoryItemSaveData> data)
    {
        for (int i = 0; i < inventoryItems.Count; i++) { inventoryItems[i] = null; }
        if (data == null || data.Count == 0) { AddStartingItems(); }
        else { foreach (var itemData in data) { ItemData itemAsset = Resources.Load<ItemData>($"Data/{itemData.itemName}"); if (itemAsset != null) { if (itemData.slotIndex < inventoryItems.Count) { inventoryItems[itemData.slotIndex] = new InventoryItem(itemAsset, itemData.quantity); } } else { Debug.LogWarning($"Не удалось найти ItemData с именем '{itemData.itemName}' в папке Resources/Data"); } } }
        OnInventoryChanged?.Invoke();
    }

    #endregion
}