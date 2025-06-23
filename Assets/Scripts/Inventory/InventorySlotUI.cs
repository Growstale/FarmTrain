using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler,
                                  IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightBorder;

    public int SlotIndex { get; private set; }
    private bool isHotbarSlot;

    private InventoryItem currentItem = null;
    private System.Action<int> onClickCallback;
    private Canvas rootCanvas;
    private GameObject draggedIconObject;
    private Image draggedIconImage;
    private RectTransform draggedIconRectTransform;
    private static InventorySlotUI currentlyDraggedSlot = null;

    private InventoryManager _inventoryManager;
    private InventoryManager InventoryManagerInstance => _inventoryManager ?? (_inventoryManager = InventoryManager.Instance);


    public void Setup(int index, bool isHotbar, System.Action<int> clickCallback)
    {
        SlotIndex = index;
        this.isHotbarSlot = isHotbar;
        this.onClickCallback = clickCallback;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("InventorySlotUI не может найти родительский Canvas!");
        }
        ClearSlot();
    }

    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item;

        if (item == null || item.IsEmpty)
        {
            ClearSlot();
            itemIcon.raycastTarget = false;
        }
        else
        {
            itemIcon.sprite = item.itemData.itemIcon;
            itemIcon.enabled = true;
            itemIcon.raycastTarget = true;

            if (item.itemData.isStackable && item.quantity > 1)
            {
                quantityText.text = item.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.enabled = false;
        itemIcon.raycastTarget = false;
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isActive);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentlyDraggedSlot != null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            onClickCallback?.Invoke(SlotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || currentItem == null || currentItem.IsEmpty)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (isHotbarSlot && !InventoryManagerInstance.IsMainInventoryPanelActive())
        {
            Debug.Log($"Drag blocked on Hotbar Slot {SlotIndex}: Inventory panel is closed.");
            eventData.pointerDrag = null;
            return;
        }

        Debug.Log($"Begin Drag on Slot: {SlotIndex}, Item: {currentItem.itemData.itemName}");

        if (draggedIconObject == null)
        {
            draggedIconObject = new GameObject("Dragged Item Icon");
            draggedIconObject.transform.SetParent(rootCanvas.transform, false);
            draggedIconObject.transform.SetAsLastSibling();
            draggedIconObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            draggedIconImage = draggedIconObject.AddComponent<Image>();
            draggedIconRectTransform = draggedIconObject.GetComponent<RectTransform>();
            draggedIconRectTransform.sizeDelta = itemIcon.rectTransform.sizeDelta * 0.8f;
        }

        draggedIconImage.sprite = itemIcon.sprite;
        draggedIconImage.color = new Color(1, 1, 1, 0.7f);
        draggedIconObject.SetActive(true);
        draggedIconRectTransform.position = eventData.position;

        itemIcon.enabled = false;
        quantityText.enabled = false;

        currentlyDraggedSlot = this;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || currentlyDraggedSlot != this) return;

        if (draggedIconRectTransform != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                rootCanvas.worldCamera,
                out Vector2 localPoint
            );
            draggedIconRectTransform.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Debug.Log($"End Drag from Slot: {SlotIndex}");

        if (draggedIconObject != null)
        {
            draggedIconObject.SetActive(false);
        }

        if (currentlyDraggedSlot == this)
        {
            UpdateSlot(currentItem);
            Debug.Log("Drag ended outside a valid drop zone.");
        }

        currentlyDraggedSlot = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"Drop detected on Slot: {SlotIndex}");

        InventorySlotUI draggedSlot = currentlyDraggedSlot;

        if (draggedSlot != null && draggedSlot != this)
        {
            Debug.Log($"Item '{draggedSlot.currentItem.itemData.itemName}' from slot {draggedSlot.SlotIndex} dropped onto slot {this.SlotIndex}");

            InventoryManagerInstance.MoveItem(draggedSlot.SlotIndex, this.SlotIndex);

            currentlyDraggedSlot = null;
        }
        else if (draggedSlot == this)
        {
            Debug.Log("Item dropped onto its own slot.");
            UpdateSlot(currentItem);
        }
    }
}