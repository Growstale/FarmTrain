using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

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
    [SerializeField] private int hotbarSize = 7;
    [SerializeField] private int maxMainInventoryRows = 3;
    [SerializeField] private int columns = 7;
    private int currentMainInventoryRows = 3;
    private List<InventoryItem> inventoryItems;

    [Header("UI References")]
    [SerializeField] private GameObject hotbarPanel;
    [SerializeField] private GameObject mainInventoryPanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button inventoryToggleButton;
    [SerializeField] private Button CloseButton;
    [SerializeField] private GameObject inventoryBackgroundPanel;

    [Header("Storage Upgrade")]
    [Tooltip("Ссылка на ItemData, который представляет собой улучшение для большого склада.")]
    [SerializeField] private ItemData storageUpgradeData;

    // --- ДОБАВЬТЕ ЭТУ СТРОКУ ---
    public ItemData StorageUpgradeData => storageUpgradeData;

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex = 0;

    [Header("BedManager")]
    [SerializeField] GameObject BedManager;
    public int SelectedSlotIndex => selectedSlotIndex;

    private List<InventorySlotUI> hotbarSlotsUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> mainInventorySlotsUI = new List<InventorySlotUI>();

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;
    public event Action<ItemData, int> OnItemAdded;

    private bool isStorageUnlocked = false; // Флаг, который отслеживает, куплен ли склад

    [Header("Starting Inventory")]
    [Tooltip("Список предметов, которые будут в инвентаре при старте игры")]
    [SerializeField] private List<StartingItemInfo> startingItems = new List<StartingItemInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInventory();
    }

    private void Start()
    {
        CreateSlotsUI();
        // --- ИЗМЕНЕНИЕ: мы больше не настраиваем кнопку напрямую здесь ---
        // SetupToggleButton(); 
        UpdateAllSlotsUI();
        SelectSlot(selectedSlotIndex);
        AddStartingItems();

        // --- НАЧАЛО ИЗМЕНЕНИЙ ---
        // Проверяем статус улучшения при старте
        CheckStorageUpgradeStatus();
        // Настраиваем слушателя для кнопки
        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.onClick.AddListener(ToggleMainInventory);
        }
        if (CloseButton != null) // Добавляем обработчик для кнопки закрытия
        {
            CloseButton.onClick.AddListener(CloseInventory);
        }

        // Подписываемся на событие покупки улучшений
        // Это более гибкий подход, чем прямая ссылка на ShopUIManager
        // Предположим, что TrainUpgradeManager может сообщить об изменениях
        // Если нет, мы можем это добавить. А пока сделаем проверку в Update.
        // --- КОНЕЦ ИЗМЕНЕНИЙ ---
    }

    private void AddStartingItems()
    {
        if (startingItems == null || startingItems.Count == 0)
        {
            return; // Если список пуст, ничего не делаем
        }

        Debug.Log("Adding starting items to inventory...");
        foreach (var itemInfo in startingItems)
        {
            if (itemInfo.itemData != null && itemInfo.quantity > 0)
            {
                // Используем уже существующий метод для добавления
                AddItem(itemInfo.itemData, itemInfo.quantity);
            }
        }
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

    private void CreateSlotsUI()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, hotbarPanel.transform);
            slotGO.name = $"HotbarSlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(i, true, OnSlotClicked);
                hotbarSlotsUI.Add(slotUI);
            }
            else
            {
                Debug.LogError($"Slot Prefab '{slotPrefab.name}' is missing InventorySlotUI script!");
                return;
            }
        }

        int mainInventoryStartIndex = hotbarSize;
        for (int i = 0; i < maxMainInventoryRows * columns; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, mainInventoryPanel.transform);
            slotGO.name = $"MainInventorySlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(mainInventoryStartIndex + i, false, OnSlotClicked);
                mainInventorySlotsUI.Add(slotUI);
            }
            else
            {
                Debug.LogError($"Slot Prefab '{slotPrefab.name}' is missing InventorySlotUI script!");
                return;
            }
        }
    }

    

    private void Update()
    {
        HandleHotbarInput();
        CheckStorageUpgradeStatus();
    }

    private void HandleHotbarInput()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                break;
            }
        }
    }

    private void CheckStorageUpgradeStatus()
    {
        if (storageUpgradeData == null)
        {
            // Если данные об улучшении не указаны, считаем склад всегда доступным.
            isStorageUnlocked = true;
        }
        else
        {
            // Проверяем через менеджер улучшений, куплен ли предмет
            isStorageUnlocked = TrainUpgradeManager.Instance.HasUpgrade(storageUpgradeData);
        }

        // Обновляем состояние кнопки
        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.interactable = isStorageUnlocked;
        }

        // Если склад заблокирован, он должен быть закрыт
        if (!isStorageUnlocked && mainInventoryPanel.activeSelf)
        {
            CloseInventory();
        }
    }

    private void OnSlotClicked(int index)
    {
        Debug.Log($"Clicked on slot index: {index}");
        if (index < hotbarSize)
        {
            SelectSlot(index);
        }
        else
        {
            Debug.Log($"Clicked on main inventory slot. Item: {GetItemInSlot(index)?.itemData?.itemName ?? "Empty"}");
        }
    }

    public bool AddItem(ItemData itemToAdd, int quantity = 1)
    {
        if (itemToAdd == null || quantity <= 0) return false;

        // Получаем текущий доступный размер инвентаря
        int currentSize = GetCurrentInventorySize();

        if (itemToAdd.isStackable)
        {
            // --- ИЗМЕНЕНИЕ: Цикл идет только до currentSize ---
            for (int i = 0; i < currentSize; i++)
            {
                // IsSlotActive(i) нам больше не нужна, т.к. currentSize уже учитывает все
                InventoryItem currentItem = inventoryItems[i];
                if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToAdd)
                {
                    int spaceAvailable = itemToAdd.maxStackSize - currentItem.quantity;
                    if (spaceAvailable >= quantity)
                    {
                        currentItem.AddQuantity(quantity);
                        UpdateSlotUI(i);
                        OnInventoryChanged?.Invoke();
                        OnItemAdded?.Invoke(itemToAdd, quantity);
                        return true;
                    }
                    else if (spaceAvailable > 0)
                    {
                        currentItem.AddQuantity(spaceAvailable);
                        UpdateSlotUI(i);
                        quantity -= spaceAvailable;
                    }
                }
                if (quantity <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnItemAdded?.Invoke(itemToAdd, quantity); // quantity здесь будет остатком
                    return true;
                }
            }
        }

        int firstEmptySlot = FindFirstEmptySlot();
        if (firstEmptySlot != -1)
        {
            inventoryItems[firstEmptySlot] = new InventoryItem(itemToAdd, quantity);
            UpdateSlotUI(firstEmptySlot);
            OnInventoryChanged?.Invoke();
            OnItemAdded?.Invoke(itemToAdd, quantity);
            return true;
        }

        Debug.Log("Inventory is full!");
        // Здесь можно добавить всплывающее уведомление для игрока
        return false;
    }

    private int FindFirstEmptySlot()
    {
        // --- ИЗМЕНЕНИЕ: Цикл идет только до currentSize ---
        int currentSize = GetCurrentInventorySize();
        for (int i = 0; i < currentSize; i++)
        {
            if (inventoryItems[i] == null || inventoryItems[i].IsEmpty)
            {
                return i;
            }
        }
        return -1; // Свободных слотов в доступной части инвентаря нет
    }


    public void RemoveItem(int index, int quantity = 1)
    {
        if (index < 0 || index >= inventoryItems.Count || inventoryItems[index] == null || inventoryItems[index].IsEmpty)
        {
            return;
        }

        inventoryItems[index].RemoveQuantity(quantity);

        if (inventoryItems[index].quantity <= 0)
        {
            inventoryItems[index] = null;
        }

        UpdateSlotUI(index);
        OnInventoryChanged?.Invoke();
    }

    public InventoryItem GetItemInSlot(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return null;
        return inventoryItems[index];
    }

    public InventoryItem GetSelectedItem()
    {
        return GetItemInSlot(selectedSlotIndex);
    }

    public void UpdateAllSlotsUI()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            UpdateSlotUI(i);
        }
        for (int i = 0; i < currentMainInventoryRows * columns; i++)
        {
            int actualIndex = hotbarSize + i;
            UpdateSlotUI(actualIndex);
        }
    }

    private void UpdateSlotUI(int index)
    {
        InventorySlotUI slotUI = GetSlotUIByIndex(index);
        if (slotUI != null)
        {
            slotUI.UpdateSlot(inventoryItems[index]);
        }
    }

    private InventorySlotUI GetSlotUIByIndex(int index)
    {
        if (index >= 0 && index < hotbarSize)
        {
            return hotbarSlotsUI[index];
        }
        else if (index >= hotbarSize && index < hotbarSize + mainInventorySlotsUI.Count)
        {
            return mainInventorySlotsUI[index - hotbarSize];
        }
        return null;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;

        InventorySlotUI previousSlotUI = GetSlotUIByIndex(selectedSlotIndex);
        if (previousSlotUI != null)
        {
            previousSlotUI.SetHighlight(false);
        }

        selectedSlotIndex = index;
        InventorySlotUI currentSlotUI = GetSlotUIByIndex(selectedSlotIndex);
        if (currentSlotUI != null)
        {
            currentSlotUI.SetHighlight(true);
        }

        Debug.Log($"Selected hotbar slot: {selectedSlotIndex}");
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
    }

    public int GetCurrentInventorySize()
    {
        // Если склад разблокирован, доступны все слоты.
        // Если нет - только хотбар.
        if (isStorageUnlocked)
        {
            return hotbarSize + (currentMainInventoryRows * columns);
        }
        else
        {
            return hotbarSize;
        }
    }

    public void OpenInventory()
    {
        if (!isStorageUnlocked) return;

        if (!mainInventoryPanel.activeSelf)
        {
            mainInventoryPanel.SetActive(true);

            if (inventoryBackgroundPanel != null)
            {
                inventoryBackgroundPanel.SetActive(true);
            }

            UpdateMainInventoryUIVisibility();
        }
    }

    public void CloseInventory()
    {
        if (mainInventoryPanel.activeSelf)
        {
            mainInventoryPanel.SetActive(false);

            if (inventoryBackgroundPanel != null)
            {
                inventoryBackgroundPanel.SetActive(false);
            }
        }
    }

    public bool IsMainInventoryPanelActive()
    {
        return mainInventoryPanel != null && mainInventoryPanel.activeSelf;
    }

    public void ToggleMainInventory()
    {
        if (!isStorageUnlocked)
        {
            Debug.Log("Нельзя открыть склад, он не куплен.");
            return;
        }

        if (mainInventoryPanel.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void SetMainInventorySize(int numberOfRows)
    {
        currentMainInventoryRows = Mathf.Clamp(numberOfRows, 1, maxMainInventoryRows);
        Debug.Log($"Main inventory size set to {currentMainInventoryRows} rows.");
        UpdateMainInventoryUIVisibility();
        ClearInactiveSlots();
        OnInventoryChanged?.Invoke();
    }

    private void UpdateMainInventoryUIVisibility()
    {
        if (!mainInventoryPanel.activeSelf)
        {
            return;
        }

        int totalVisibleMainSlots = currentMainInventoryRows * columns;
        for (int i = 0; i < mainInventorySlotsUI.Count; i++)
        {
            bool shouldBeActive = i < totalVisibleMainSlots;
            if (mainInventorySlotsUI[i].gameObject.activeSelf != shouldBeActive)
            {
                mainInventorySlotsUI[i].gameObject.SetActive(shouldBeActive);
            }
            if (shouldBeActive)
            {
                UpdateSlotUI(hotbarSize + i);
            }
        }
    }

    private bool IsSlotActive(int index)
    {
        if (index >= 0 && index < hotbarSize)
        {
            return true;
        }
        else if (index >= hotbarSize && index < hotbarSize + (currentMainInventoryRows * columns))
        {
            return true;
        }
        return false;
    }

    private void ClearInactiveSlots()
    {
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (!IsSlotActive(i) && inventoryItems[i] != null && !inventoryItems[i].IsEmpty)
            {
                Debug.LogWarning($"Clearing item '{inventoryItems[i].itemData.itemName}' from inactive slot {i}");
                inventoryItems[i] = null;
            }
        }
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        int currentSize = GetCurrentInventorySize();
        if (fromIndex < 0 || fromIndex >= currentSize ||
            toIndex < 0 || toIndex >= currentSize ||
            fromIndex == toIndex)
        {

            Debug.LogWarning($"Invalid move operation: from {fromIndex} to {toIndex}");
            UpdateSlotUI(fromIndex);
            return;
        }

        if (!IsSlotActive(fromIndex) || !IsSlotActive(toIndex))
        {
            Debug.LogWarning($"Cannot move item: one or both slots are inactive (from: {IsSlotActive(fromIndex)}, to: {IsSlotActive(toIndex)})");
            UpdateSlotUI(fromIndex);
            return;
        }

        InventoryItem itemFrom = inventoryItems[fromIndex];
        InventoryItem itemTo = inventoryItems[toIndex];

        Debug.Log($"Attempting to move item from {fromIndex} ({itemFrom?.itemData?.itemName ?? "Empty"}) to {toIndex} ({itemTo?.itemData?.itemName ?? "Empty"})");

        if (itemFrom != null && !itemFrom.IsEmpty && itemTo != null && !itemTo.IsEmpty &&
            itemFrom.itemData == itemTo.itemData && itemFrom.itemData.isStackable)
        {
            int spaceInTarget = itemTo.itemData.maxStackSize - itemTo.quantity;
            if (spaceInTarget > 0)
            {
                int amountToMove = Mathf.Min(itemFrom.quantity, spaceInTarget);
                itemTo.AddQuantity(amountToMove);
                itemFrom.RemoveQuantity(amountToMove);

                if (itemFrom.quantity <= 0)
                {
                    inventoryItems[fromIndex] = null;
                }
                Debug.Log($"Merged stacks: Moved {amountToMove} of {itemFrom.itemData.itemName}. From Qty: {itemFrom.quantity}, To Qty: {itemTo.quantity}");
            }
            else
            {
                Debug.Log("Merge failed: Target stack is full. Swapping instead.");
                inventoryItems[toIndex] = itemFrom;
                inventoryItems[fromIndex] = itemTo;
            }
        }
        else
        {
            inventoryItems[toIndex] = itemFrom;
            inventoryItems[fromIndex] = itemTo;
            Debug.Log("Swapped items between slots.");
        }

        UpdateSlotUI(fromIndex);
        UpdateSlotUI(toIndex);

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
                    UpdateSlotUI(i);
                    break;
                }
                else
                {
                    quantityLeftToRemove -= currentItem.quantity;
                    inventoryItems[i] = null;
                    UpdateSlotUI(i);
                }
            }

            if (quantityLeftToRemove <= 0)
            {
                break;
            }
        }

        if (quantityLeftToRemove > 0)
        {
            Debug.LogWarning($"Could not remove all items. {quantityLeftToRemove} of {itemToRemove.itemName} remained.");
        }

        OnInventoryChanged?.Invoke();
    }

    public int GetTotalItemQuantity(ItemData itemToCount)
    {
        int totalQuantity = 0;
        if (itemToCount == null) return 0;

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (IsSlotActive(i))
            {
                InventoryItem currentItem = inventoryItems[i];
                if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToCount)
                {
                    totalQuantity += currentItem.quantity;
                }
            }
        }
        return totalQuantity;
    }

    public bool CheckForSpace(ItemData itemToAdd, int quantity)
    {
        int currentSize = GetCurrentInventorySize();

        if (itemToAdd == null || quantity <= 0) return false;

        int quantityLeftToPlace = quantity;

        if (itemToAdd.isStackable)
        {
            for (int i = 0; i < currentSize; i++)
            {
                if (!IsSlotActive(i)) continue;

                InventoryItem currentItem = inventoryItems[i];
                if (currentItem != null && !currentItem.IsEmpty && currentItem.itemData == itemToAdd)
                {
                    int spaceAvailable = itemToAdd.maxStackSize - currentItem.quantity;
                    quantityLeftToPlace -= spaceAvailable;

                    if (quantityLeftToPlace <= 0) return true;
                }
            }
        }

        for (int i = 0; i < currentSize; i++)
        {
            if (!IsSlotActive(i)) continue;

            if (inventoryItems[i] == null || inventoryItems[i].IsEmpty)
            {
                quantityLeftToPlace -= itemToAdd.maxStackSize;
                if (quantityLeftToPlace <= 0) return true;
            }
        }

        return false;
    }
}