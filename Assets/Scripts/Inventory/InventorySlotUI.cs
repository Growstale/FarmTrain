using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// Добавляем интерфейсы для Drag & Drop
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler,
                                  IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightBorder;

    public int SlotIndex { get; private set; }

    private InventoryItem currentItem = null;
    private System.Action<int> onClickCallback; // Для обычного клика
    // --- НОВЫЕ ПЕРЕМЕННЫЕ ДЛЯ DRAG & DROP ---
    private Canvas rootCanvas; // Корневой Canvas для правильного позиционирования
    private GameObject draggedIconObject; // Объект, который будем перетаскивать
    private Image draggedIconImage;    // Иконка этого объекта
    private RectTransform draggedIconRectTransform; // Для перемещения иконки
    private static InventorySlotUI currentlyDraggedSlot = null; // Статическая переменная, чтобы знать, какой слот перетаскивается СЕЙЧАС

    // --- ССЫЛКА НА ИНВЕНТАРЬ (для операции обмена/перемещения) ---
    // Ленивая инициализация, чтобы не искать каждый раз
    private InventoryManager _inventoryManager;
    private InventoryManager InventoryManagerInstance => _inventoryManager ?? (_inventoryManager = InventoryManager.Instance);


    public void Setup(int index, System.Action<int> clickCallback)
    {
        SlotIndex = index;
        this.onClickCallback = clickCallback;
        // Находим корневой Canvas один раз при настройке
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("InventorySlotUI не может найти родительский Canvas!");
        }
        ClearSlot();
    }

    // ... (UpdateSlot, ClearSlot, SetHighlight - без изменений) ...
    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item;

        if (item == null || item.IsEmpty)
        {
            ClearSlot();
            itemIcon.raycastTarget = false; // Нельзя начать перетаскивание пустого слота
        }
        else
        {
            itemIcon.sprite = item.itemData.itemIcon;
            itemIcon.enabled = true;
            itemIcon.raycastTarget = true; // Можно начать перетаскивание с этого слота

            if (item.itemData.isStackable && item.quantity > 1) // Показываем количество только если > 1 (опционально)
            {
                quantityText.text = item.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
        // Убираем raycast target у фона, чтобы клики проходили на иконку, если она есть
        // Image backgroundImage = GetComponent<Image>();
        // if (backgroundImage != null)
        // {
        //     backgroundImage.raycastTarget = (item == null || item.IsEmpty); // Фон кликабелен только если слот пуст
        // }
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.enabled = false;
        itemIcon.raycastTarget = false; // Пустой слот не кликабелен для перетаскивания
        // Image backgroundImage = GetComponent<Image>();
        // if (backgroundImage != null) backgroundImage.raycastTarget = true; // Фон кликабелен
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isActive);
        }
    }


    // --- ОБРАБОТКА КЛИКА (ОСТАЕТСЯ) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // Предотвращаем клик, если мы только что закончили перетаскивание на этот слот
        if (currentlyDraggedSlot != null) return;

        // Обрабатываем только левый клик
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Вызываем колбэк, если он назначен
            onClickCallback?.Invoke(SlotIndex);
        }
        // Можно добавить обработку правого клика здесь (eventData.button == PointerEventData.InputButton.Right)
        // Например, для разделения стака или вызова контекстного меню
    }

    // --- РЕАЛИЗАЦИЯ ИНТЕРФЕЙСОВ DRAG & DROP ---

    // Вызывается в начале перетаскивания
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Можно перетаскивать только левой кнопкой и если слот не пуст
        if (eventData.button != PointerEventData.InputButton.Left || currentItem == null || currentItem.IsEmpty)
        {
            eventData.pointerDrag = null; // Отменяем перетаскивание
            return;
        }

        Debug.Log($"Begin Drag on Slot: {SlotIndex}, Item: {currentItem.itemData.itemName}");

        // Создаем временный объект для перетаскиваемой иконки
        if (draggedIconObject == null)
        {
            // Создаем объект один раз
            draggedIconObject = new GameObject("Dragged Item Icon");
            // Делаем его дочерним корневому Canvas, чтобы он рисовался поверх всего UI
            draggedIconObject.transform.SetParent(rootCanvas.transform, false);
            draggedIconObject.transform.SetAsLastSibling(); // Рисовать поверх
            draggedIconObject.AddComponent<CanvasGroup>().blocksRaycasts = false; // Чтобы не блокировал Raycast для IDropHandler
            draggedIconImage = draggedIconObject.AddComponent<Image>();
            draggedIconRectTransform = draggedIconObject.GetComponent<RectTransform>();
            // Можно настроить размер иконки
            draggedIconRectTransform.sizeDelta = itemIcon.rectTransform.sizeDelta * 0.8f; // Чуть меньше оригинала
        }

        // Настраиваем и показываем перетаскиваемую иконку
        draggedIconImage.sprite = itemIcon.sprite;
        draggedIconImage.color = new Color(1, 1, 1, 0.7f); // Полупрозрачная
        draggedIconObject.SetActive(true);
        draggedIconRectTransform.position = eventData.position; // Начальная позиция под курсором

        // Скрываем иконку в самом слоте на время перетаскивания
        itemIcon.enabled = false;
        quantityText.enabled = false;

        // Запоминаем, какой слот мы перетаскиваем
        currentlyDraggedSlot = this;
    }

    // Вызывается каждый кадр во время перетаскивания
    public void OnDrag(PointerEventData eventData)
    {
        // Можно перетаскивать только левой кнопкой
        if (eventData.button != PointerEventData.InputButton.Left || currentlyDraggedSlot != this) return;

        // Обновляем позицию перетаскиваемой иконки, чтобы она следовала за курсором
        if (draggedIconRectTransform != null)
        {
            // Преобразуем позицию мыши в локальные координаты Canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                rootCanvas.worldCamera, // Используем камеру Canvas (обычно основная)
                out Vector2 localPoint
            );
            draggedIconRectTransform.localPosition = localPoint;

            // Или проще, если Canvas в режиме Screen Space - Overlay:
            // draggedIconRectTransform.position = eventData.position;
        }
    }

    // Вызывается в конце перетаскивания (когда кнопку мыши отпустили)
    public void OnEndDrag(PointerEventData eventData)
    {
        // Можно завершать перетаскивание только левой кнопкой
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Debug.Log($"End Drag from Slot: {SlotIndex}");

        // Уничтожаем или скрываем перетаскиваемую иконку
        if (draggedIconObject != null)
        {
            // Destroy(draggedIconObject); // Можно уничтожать
            draggedIconObject.SetActive(false); // Или просто скрывать для повторного использования
        }

        // Если перетаскивание не завершилось над другим слотом (не сработал OnDrop)
        // то просто возвращаем видимость иконки в исходном слоте.
        // OnDrop сработает раньше, чем OnEndDrag, если перетащили на валидный слот.
        // Если currentlyDraggedSlot все еще равен this, значит OnDrop не сработал.
        if (currentlyDraggedSlot == this)
        {
            // Восстанавливаем вид исходного слота (если он не был очищен в OnDrop)
            UpdateSlot(currentItem);
            Debug.Log("Drag ended outside a valid drop zone.");
        }


        // Сбрасываем статическую переменную
        currentlyDraggedSlot = null;
    }

    // Вызывается, когда перетаскиваемый объект "бросают" НА ЭТОТ слот
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"Drop detected on Slot: {SlotIndex}");

        // Получаем информацию о слоте, который перетаскивали
        InventorySlotUI draggedSlot = currentlyDraggedSlot;

        // Проверяем, что перетаскивание было начато (draggedSlot не null)
        // и что мы не бросаем слот сам на себя
        if (draggedSlot != null && draggedSlot != this)
        {
            Debug.Log($"Item '{draggedSlot.currentItem.itemData.itemName}' from slot {draggedSlot.SlotIndex} dropped onto slot {this.SlotIndex}");

            // --- ЛОГИКА ПЕРЕМЕЩЕНИЯ/ОБМЕНА В ИНВЕНТАРЕ ---
            // Вызываем метод в InventoryManager для обработки перемещения
            InventoryManagerInstance.MoveItem(draggedSlot.SlotIndex, this.SlotIndex);

            // Важно: После вызова MoveItem, InventoryManager должен сам обновить
            // UI обоих слотов (draggedSlot и this). Нам не нужно здесь явно
            // обновлять currentItem или вызывать UpdateSlot.

            // Сбрасываем currentlyDraggedSlot здесь, чтобы OnEndDrag понял, что дроп был успешным
            currentlyDraggedSlot = null;
        }
        else if (draggedSlot == this)
        {
            Debug.Log("Item dropped onto its own slot.");
            // Можно просто вернуть иконку на место
            UpdateSlot(currentItem);
        }
    }
}