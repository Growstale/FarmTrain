// QuestLogEntryUI.cs (ИСПРАВЛЕННАЯ ВЕРСИЯ)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class QuestLogEntryUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Button pinButton;
    [SerializeField] private Image pinIcon;
    [SerializeField] private Button mainButton;

    [Header("Pin Sprites")]
    [SerializeField] private Sprite pinnedSprite;
    [SerializeField] private Sprite unpinnedSprite;
    [SerializeField] private Sprite pinHighlightedSprite;

    [Header("Button Sprites")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite buttonSelectedSprite;
    [SerializeField] private Sprite buttonOnSprite;

    [Header("Text Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material disabledMaterial;

    public Quest assignedQuest { get; private set; }
    private Action<Quest> onSelectCallback;

    // <<< ИЗМЕНЕНИЕ 1: Новые переменные для четкого разделения состояний
    private bool isSelected = false;    // Эта строка выбрана? Управляется извне.
    private bool isPointerInside = false; // Находится ли курсор над этой строкой?
    private bool isPointerDown = false;   // Зажата ли ЛКМ на этой строке?

    private void Awake()
    {
        if (titleText == null) titleText = GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null) titleText.raycastTarget = false;
    }

    public void Setup(Quest quest, Action<Quest> selectCallback)
    {
        assignedQuest = quest;
        onSelectCallback = selectCallback;
        isSelected = false; // При новой установке сбрасываем состояние

        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(HandleSelection);
        pinButton.onClick.RemoveAllListeners();
        pinButton.onClick.AddListener(HandlePinning);

        UpdateVisuals(); // Обновляем внешний вид
    }

    // <<< ИЗМЕНЕНИЕ 2: Главный метод, который устанавливает состояние выбора извне
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals(); // После изменения состояния, обновляем внешний вид
    }

    // <<< ИЗМЕНЕНИЕ 3: Все методы событий теперь просто меняют флаги и вызывают ОДИН метод для обновления вида
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!mainButton.interactable) return;
        isPointerDown = true;
        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!mainButton.interactable) return;
        isPointerDown = false;
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPointerDown = false; // Если курсор ушел, то и нажатие отменяется
        UpdateVisuals();
    }

    private void HandleSelection()
    {
        onSelectCallback?.Invoke(assignedQuest);
    }

    private void HandlePinning()
    {
        QuestManager.Instance.PinQuest(assignedQuest);
        // QuestLogUI обновит все элементы, включая этот
    }

    // <<< ИЗМЕНЕНИЕ 4: ЕДИНЫЙ метод, который решает, как должна выглядеть строка
    private void UpdateVisuals()
    {
        if (assignedQuest == null) return;

        titleText.text = assignedQuest.title;

        var buttonImage = mainButton.GetComponent<Image>();

        // Сначала разбираемся со статусом квеста
        if (assignedQuest.status == QuestStatus.Completed)
        {
            titleText.fontMaterial = disabledMaterial;
            checkmarkImage.gameObject.SetActive(true);
            pinButton.gameObject.SetActive(false);
            mainButton.interactable = false;
            if (buttonImage != null) buttonImage.sprite = buttonSprite;
            return; // Выходим, дальше проверять не нужно
        }

        // Если квест не завершен
        mainButton.interactable = true;
        checkmarkImage.gameObject.SetActive(false);
        pinButton.gameObject.SetActive(true);

        // Теперь определяем внешний вид на основе приоритетов:
        // 1. Выбрана (самый высокий приоритет)
        // 2. Нажата
        // 3. Наведена
        // 4. Обычное состояние (закреплена/не закреплена)
        if (isSelected)
        {
            if (buttonImage != null) buttonImage.sprite = buttonSelectedSprite;
            titleText.fontMaterial = selectedMaterial;
            pinIcon.sprite = pinnedSprite; // В выбранном состоянии булавка всегда "активна"
        }
        else if (isPointerDown)
        {
            if (buttonImage != null) buttonImage.sprite = buttonSelectedSprite; // Можно использовать тот же спрайт, что и для выбора
            titleText.fontMaterial = selectedMaterial;
            pinIcon.sprite = pinnedSprite;
        }
        else if (isPointerInside)
        {
            if (buttonImage != null) buttonImage.sprite = buttonOnSprite;
            titleText.fontMaterial = highlightedMaterial;
            pinIcon.sprite = pinHighlightedSprite; // Специальный спрайт для подсвеченной булавки
        }
        else // Обычное состояние
        {
            if (buttonImage != null) buttonImage.sprite = buttonSprite;
            titleText.fontMaterial = normalMaterial;
            pinIcon.sprite = assignedQuest.isPinned ? pinnedSprite : unpinnedSprite;
        }
    }
}