using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ���� ������ ����� �������� ������ �� ����������� ���������.
public class InventoryUI : MonoBehaviour, IUIManageable
{
    // --- ����, ������� �� ��������� �� InventoryManager ---
    [Header("UI References")]
    [SerializeField] private GameObject hotbarPanel;
    [SerializeField] private GameObject mainInventoryPanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button inventoryToggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject inventoryBackgroundPanel;

    private List<InventorySlotUI> hotbarSlotsUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> mainInventorySlotsUI = new List<InventorySlotUI>();

    // ������ �� �������� ������
    private InventoryManager inventoryManager;


    void Start()
    {
        // ������� ��� ������ �������� ������
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager �� ������! UI �� ����� ��������.");
            gameObject.SetActive(false);
            return;
        }

        // --- ������, ������� �� ��������� ---
        CreateSlotsUI();
        UpdateAllSlotsUI();
        // ������������� ��������� ��������� �����
        OnSelectedSlotChanged(inventoryManager.SelectedSlotIndex);

        // ������������� �� ������� �� ��������� ������
        inventoryManager.OnInventoryChanged += UpdateAllSlotsUI;
        inventoryManager.OnSelectedSlotChanged += OnSelectedSlotChanged;

        // ��������� �������� �������
        inventoryToggleButton.onClick.AddListener(ToggleMainInventory);
        closeButton.onClick.AddListener(CloseInventory);

        // �������������� � ExclusiveUIManager
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        mainInventoryPanel.SetActive(false);
        inventoryBackgroundPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // ����������� ������������ �� �������, ����� �� ���� ������ ������
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

    // --- ��� ������ ��� ������ � UI, ������� �� ��������� ---

    private void CreateSlotsUI()
    {
        // ������� ����� ��� �������
        for (int i = 0; i < inventoryManager.hotbarSize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, hotbarPanel.transform);
            slotGO.name = $"HotbarSlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(i, true, OnSlotClicked);
            hotbarSlotsUI.Add(slotUI);
        }

        // ������� ����� ��� ��������� ���������
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

    // ���� ����� ����������, ����� ����� ������� �� ���� � UI
    private void OnSlotClicked(int index)
    {
        // �� ������ �������� ������� ��������� ������
        if (index < inventoryManager.hotbarSize)
        {
            inventoryManager.SelectSlot(index);
            // ����� �������� ���� ����� �����
        }
        else
        {
            // ������ ����� �� ��������� ���������, ���� �����
        }
    }

    private void OnSelectedSlotChanged(int newIndex)
    {
        // ������� ��������� �� ���� ������
        foreach (var slot in hotbarSlotsUI)
        {
            slot.SetHighlight(false);
        }
        // �������� ��������� �� ������
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

    // --- ���������� ���������� IUIManageable ---
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