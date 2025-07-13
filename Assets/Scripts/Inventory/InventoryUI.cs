using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Этот скрипт будет отвечать ТОЛЬКО за отображение инвентаря.
public class InventoryUI : MonoBehaviour, IUIManageable
{
    // --- Поля, которые мы перенесли из InventoryManager ---
    [Header("UI References")]
    [SerializeField] private GameObject hotbarPanel;
    [SerializeField] private GameObject mainInventoryPanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button inventoryToggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject inventoryBackgroundPanel;

    private List<InventorySlotUI> hotbarSlotsUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> mainInventorySlotsUI = new List<InventorySlotUI>();

    // Ссылка на менеджер данных
    private InventoryManager inventoryManager;


    void Start()
    {
        // Находим наш вечный менеджер данных
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager не найден! UI не будет работать.");
            gameObject.SetActive(false);
            return;
        }

        // --- Логика, которую мы перенесли ---
        CreateSlotsUI();
        UpdateAllSlotsUI();
        // Устанавливаем начальное выделение слота
        OnSelectedSlotChanged(inventoryManager.SelectedSlotIndex);

        // Подписываемся на события от менеджера данных
        inventoryManager.OnInventoryChanged += UpdateAllSlotsUI;
        inventoryManager.OnSelectedSlotChanged += OnSelectedSlotChanged;

        // Назначаем действия кнопкам
        inventoryToggleButton.onClick.AddListener(ToggleMainInventory);
        closeButton.onClick.AddListener(CloseInventory);

        // Регистрируемся в ExclusiveUIManager
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        mainInventoryPanel.SetActive(false);
        inventoryBackgroundPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // ОБЯЗАТЕЛЬНО отписываемся от событий, чтобы не было утечек памяти
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= UpdateAllSlotsUI;
            inventoryManager.OnSelectedSlotChanged -= OnSelectedSlotChanged;
        }
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }

    // --- Все методы для работы с UI, которые мы перенесли ---

    private void CreateSlotsUI()
    {
        // Создаем слоты для хотбара
        for (int i = 0; i < inventoryManager.hotbarSize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, hotbarPanel.transform);
            slotGO.name = $"HotbarSlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(i, true, OnSlotClicked);
            hotbarSlotsUI.Add(slotUI);
        }

        // Создаем слоты для основного инвентаря
        int mainInvSize = inventoryManager.maxMainInventoryRows * inventoryManager.columns;
        for (int i = 0; i < mainInvSize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, mainInventoryPanel.transform);
            slotGO.name = $"MainInventorySlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(inventoryManager.hotbarSize + i, false, OnSlotClicked);
            mainInventorySlotsUI.Add(slotUI);
        }
    }

    // Этот метод вызывается, когда игрок кликает на слот в UI
    private void OnSlotClicked(int index)
    {
        // Мы просто передаем команду менеджеру данных
        if (index < inventoryManager.hotbarSize)
        {
            inventoryManager.SelectSlot(index);
            // Можно добавить звук клика здесь
        }
        else
        {
            // Логика клика по основному инвентарю, если нужна
        }
    }

    private void OnSelectedSlotChanged(int newIndex)
    {
        // Убираем подсветку со всех слотов
        foreach (var slot in hotbarSlotsUI)
        {
            slot.SetHighlight(false);
        }
        // Включаем подсветку на нужном
        if (newIndex >= 0 && newIndex < hotbarSlotsUI.Count)
        {
            hotbarSlotsUI[newIndex].SetHighlight(true);
        }
    }

    private void UpdateAllSlotsUI()
    {
        for (int i = 0; i < hotbarSlotsUI.Count; i++)
        {
            hotbarSlotsUI[i].UpdateSlot(inventoryManager.GetItemInSlot(i));
        }
        for (int i = 0; i < mainInventorySlotsUI.Count; i++)
        {
            int actualIndex = inventoryManager.hotbarSize + i;
            mainInventorySlotsUI[i].UpdateSlot(inventoryManager.GetItemInSlot(actualIndex));
        }
        UpdateMainInventoryUIVisibility();
    }

    private void UpdateMainInventoryUIVisibility()
    {
        if (!mainInventoryPanel.activeSelf) return;

        int totalVisibleMainSlots = inventoryManager.currentMainInventoryRows * inventoryManager.columns;
        for (int i = 0; i < mainInventorySlotsUI.Count; i++)
        {
            mainInventorySlotsUI[i].gameObject.SetActive(i < totalVisibleMainSlots);
        }
    }

    // --- Реализация интерфейса IUIManageable ---
    public bool IsExclusive => true;

    public void ToggleMainInventory()
    {
        if (mainInventoryPanel.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        if (!mainInventoryPanel.activeSelf)
        {
            ExclusiveUIManager.Instance.NotifyPanelOpening(this);
            mainInventoryPanel.SetActive(true);
            inventoryBackgroundPanel.SetActive(true);
            UpdateMainInventoryUIVisibility();
        }
    }

    public void CloseInventory()
    {
        if (mainInventoryPanel.activeSelf)
        {
            mainInventoryPanel.SetActive(false);
            inventoryBackgroundPanel.SetActive(false);
        }
    }

    public void CloseUI()
    {
        CloseInventory();
    }

    public bool IsOpen()
    {
        return mainInventoryPanel != null && mainInventoryPanel.activeSelf;
    }
}