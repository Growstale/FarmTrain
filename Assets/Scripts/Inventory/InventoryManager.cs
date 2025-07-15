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

public class InventoryManager : MonoBehaviour, IUIManageable
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Data")]
    [SerializeField] private int hotbarSize = 7;
    [SerializeField] private int maxMainInventoryRows = 3; // <<< Это остается 3 (максимально возможный размер)
    [SerializeField] private int columns = 7;
    private int currentMainInventoryRows = 1; // <<< ИЗМЕНЕНИЕ: Начинаем с 1 строки по умолчанию
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
    public ItemData StorageUpgradeData => storageUpgradeData;

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex = 0;
    public int SelectedSlotIndex => selectedSlotIndex;

    private List<InventorySlotUI> hotbarSlotsUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> mainInventorySlotsUI = new List<InventorySlotUI>();

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;
    public event Action<ItemData, int> OnItemAdded;

    // --- УДАЛЕНА ПЕРЕМЕННАЯ isStorageUnlocked ---
    // Она больше не нужна, так как у нас теперь более гибкая система.

    [Header("Starting Inventory")]
    [Tooltip("Список предметов, которые будут в инвентаре при старте игры")]
    [SerializeField] private List<StartingItemInfo> startingItems = new List<StartingItemInfo>();
    [SerializeField] private AudioClip slotClickSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        InitializeInventory();
    }

    private void Start()
    {
        CreateSlotsUI();
        UpdateAllSlotsUI();
        SelectSlot(selectedSlotIndex);
        AddStartingItems();

        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.onClick.AddListener(ToggleMainInventory);
        }
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(CloseInventory);
        }

        // <<< ИЗМЕНЕНИЕ: При старте проверяем улучшения и устанавливаем правильный размер
        UpdateInventorySizeBasedOnUpgrades();
    }

    private void AddStartingItems()
    {
        if (startingItems == null || startingItems.Count == 0) return;

        Debug.Log("Adding starting items to inventory...");
        foreach (var itemInfo in startingItems)
        {
            if (itemInfo.itemData != null && itemInfo.quantity > 0)
            {
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
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleMainInventory();
        }

        HandleHotbarInput();
        UpdateInventorySizeBasedOnUpgrades();
    }

    // --- НОВЫЙ МЕТОД: "Мозг" нашей новой логики ---
    private void UpdateInventorySizeBasedOnUpgrades()
    {
        int targetRows = 1; // По умолчанию у нас одна строка

        // Проверяем, существует ли менеджер улучшений и есть ли у нас данные об улучшении
        if (TrainUpgradeManager.Instance != null && storageUpgradeData != null)
        {
            // Если улучшение куплено, наша цель - 3 строки
            if (TrainUpgradeManager.Instance.HasUpgrade(storageUpgradeData))
            {
                targetRows = 3;
            }
        }

        // Устанавливаем новый размер, только если он отличается от текущего
        if (targetRows != currentMainInventoryRows)
        {
            SetMainInventorySize(targetRows);
        }
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

    // --- УДАЛЕН СТАРЫЙ МЕТОД CheckStorageUpgradeStatus() ---

    private void OnSlotClicked(int index)
    {
        if (index < hotbarSize)
        {
            SelectSlot(index);
            if (slotClickSound != null)
            {
                SFXManager.Instance.PlaySFX(slotClickSound);
            }
        }

    }

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
                    OnItemAdded?.Invoke(itemToAdd, quantity);
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
        return false;
    }

    private int FindFirstEmptySlot()
    {
        int currentSize = GetCurrentInventorySize();
        for (int i = 0; i < currentSize; i++)
        {
            if (inventoryItems[i] == null || inventoryItems[i].IsEmpty)
            {
                return i;
            }
        }
        return -1;
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
        for (int i = 0; i < mainInventorySlotsUI.Count; i++)
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

        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
    }

    public int GetCurrentInventorySize()
    {
        // Размер инвентаря теперь всегда равен хотбару + текущему количеству строк
        return hotbarSize + (currentMainInventoryRows * columns);
    }

    public void OpenInventory()
    {
        if (!mainInventoryPanel.activeSelf)
        {
            ExclusiveUIManager.Instance.NotifyPanelOpening(this);
            GameStateManager.Instance.RequestPause(this);
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
            GameStateManager.Instance.RequestResume(this);
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
        if (mainInventoryPanel.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory(); // OpenInventory уже содержит вызов NotifyPanelOpening
        }
    }

    public void CloseUI()
    {
        CloseInventory();
    }

    public bool IsOpen()
    {
        // Система должна знать, открыты ли мы сейчас
        return IsMainInventoryPanelActive();
    }


    // Этот метод теперь вызывается из UpdateInventorySizeBasedOnUpgrades
    public void SetMainInventorySize(int numberOfRows)
    {
        currentMainInventoryRows = Mathf.Clamp(numberOfRows, 1, maxMainInventoryRows);
        Debug.Log($"Размер основного инвентаря установлен на {currentMainInventoryRows} строк.");
        UpdateMainInventoryUIVisibility();
        ClearInactiveSlots();
        OnInventoryChanged?.Invoke();
    }

    private void UpdateMainInventoryUIVisibility()
    {
        if (!mainInventoryPanel.activeSelf && mainInventorySlotsUI.Count > 0)
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
                Debug.LogWarning($"Очистка предмета '{inventoryItems[i].itemData.itemName}' из неактивного слота {i}");
                inventoryItems[i] = null;
            }
        }
    }

    // Остальные методы (MoveItem, RemoveItemByType, GetTotalItemQuantity, CheckForSpace) остаются без изменений.
    // ...
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
            }
            else
            {
                inventoryItems[toIndex] = itemFrom;
                inventoryItems[fromIndex] = itemTo;
            }
        }
        else
        {
            inventoryItems[toIndex] = itemFrom;
            inventoryItems[fromIndex] = itemTo;
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

    private void OnDestroy()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }
}