using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class QuestLogEntryUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Button pinButton; // Наша кнопка-булавка
    [SerializeField] private Image pinIcon; // Иконка на этой кнопке
    [SerializeField] private Button mainButton; // Основная кнопка, по которой кликаем

    [Header("Pin Sprites")]
    [SerializeField] private Sprite pinnedSprite; // Спрайт закрепленной булавки
    [SerializeField] private Sprite unpinnedSprite; // Спрайт обычной булавки
    [SerializeField] private Sprite pinHighlightedSprite;

    [Header("Button Sprites")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite buttonSelectedSprite;

    [Header("Text Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material disabledMaterial;

    [HideInInspector] public Quest assignedQuest;
    private Action<Quest> onSelectCallback;
    private bool isPointerInside;
    private bool isPointerDown;

    private void Awake()
    {
        if (titleText == null)
            titleText = GetComponentInChildren<TextMeshProUGUI>();

        if (titleText != null)
            titleText.raycastTarget = false;
    }

    public void Setup(Quest quest, Action<Quest> selectCallback)
    {
        assignedQuest = quest;
        onSelectCallback = selectCallback;

        titleText.text = quest.title;
        titleText.fontMaterial = normalMaterial;

        // Настраиваем видимость элементов в зависимости от статуса квеста
        bool isCompleted = quest.status == QuestStatus.Completed;
        checkmarkImage.gameObject.SetActive(isCompleted);
        pinButton.gameObject.SetActive(!isCompleted); // Булавку показываем только для активных

        if (isCompleted)
        {
            titleText.fontMaterial = disabledMaterial;
            mainButton.interactable = false;
        }
        else
        {
            titleText.fontMaterial = normalMaterial;
            mainButton.interactable = true;
            pinIcon.sprite = quest.isPinned ? pinnedSprite : unpinnedSprite;
        }

        // Привязываем слушателей событий
        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(HandleSelection);

        pinButton.onClick.RemoveAllListeners();
        pinButton.onClick.AddListener(HandlePinning);
    }

    // Клик по основной части строки - выбрать квест для показа деталей
    private void HandleSelection()
    {
        onSelectCallback?.Invoke(assignedQuest);
    }

    // Клик по булавке - закрепить/открепить
    private void HandlePinning()
    {
        bool newPinnedState = !assignedQuest.isPinned;
        QuestManager.Instance.PinQuest(assignedQuest);
        UpdatePinIcon(newPinnedState);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainButton.interactable && !isPointerDown)
        {
            titleText.fontMaterial = selectedMaterial;
            pinIcon.sprite = pinnedSprite;
            isPointerDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Не обрабатываем отпускание, если кнопка уже была выбрана
        if (!mainButton.interactable || isPointerDown)
            return;

        titleText.fontMaterial = isPointerInside ? highlightedMaterial : normalMaterial;
        UpdatePinIcon(assignedQuest.isPinned);
    }

    private void UpdatePinIcon(bool isPinned)
    {
        if (isPointerDown)
        {
            pinIcon.sprite = pinnedSprite;
        }
        else if (isPointerInside)
        {
            pinIcon.sprite = pinHighlightedSprite;
        }
        else
        {
            pinIcon.sprite = isPinned ? pinnedSprite : unpinnedSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        if (mainButton.interactable)
        {
            if (!isPointerDown)
            {
                titleText.fontMaterial = highlightedMaterial;
                UpdatePinIcon(assignedQuest.isPinned);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        if (mainButton.interactable)
        {
            if (!isPointerDown)
            {
                titleText.fontMaterial = normalMaterial;
                UpdatePinIcon(assignedQuest.isPinned);
            }
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (mainButton.interactable)
        {
            var buttonImage = mainButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = isSelected ? buttonSelectedSprite : buttonSprite;
            }

            titleText.fontMaterial = isSelected ? selectedMaterial : normalMaterial;
            pinIcon.sprite = isSelected ? pinnedSprite : 
                        (isPointerInside ? pinHighlightedSprite : 
                         assignedQuest.isPinned ? pinnedSprite : unpinnedSprite);
            isPointerDown = isSelected;
        }
    }
}